using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace XivVoices.Services;

public interface IPlaybackService : IHostedService
{
  event EventHandler<MessageSource>? PlaybackStarted;
  event EventHandler<MessageSource>? PlaybackCompleted;

  Task Play(XivMessage message, bool replay = false);

  void StopAll();
  void Stop(MessageSource source);
  void Stop(string id);

  IEnumerable<(XivMessage Message, bool IsPlaying, TimeSpan CurrentTime, TimeSpan TotalTime)> GetPlaybackHistory();

  IEnumerable<TrackableSound> Debug_GetPlaying();
  int Debug_GetMixerSourceCount();
}

public class PlaybackService(ILogger _logger, Configuration _configuration, IFramework _framework, IClientState _clientState, IGameInteropService _gameInteropService, ILocalTTSService _localTTSService, IAudioPostProcessor _audioPostProcessor, ILipSync _lipSync) : IPlaybackService
{
  private WaveOutEvent? _outputDevice;
  private MixingSampleProvider? _mixer;

  private readonly ConcurrentDictionary<string, TrackableSound> _playing = new();
  private readonly object _playbackHistoryLock = new();
  private readonly List<XivMessage> _playbackHistory = [];

  public event EventHandler<MessageSource>? PlaybackStarted;
  public event EventHandler<MessageSource>? PlaybackCompleted;

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _framework.Update += FrameworkOnUpdate;
    _clientState.TerritoryChanged += OnTerritoryChanged;

    _outputDevice = new WaveOutEvent();
    _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2))
    {
      ReadFully = true
    };

    _outputDevice.Init(_mixer);
    _outputDevice.Play();

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _framework.Update -= FrameworkOnUpdate;
    _clientState.TerritoryChanged -= OnTerritoryChanged;

    foreach (TrackableSound track in _playing.Values)
    {
      _mixer?.RemoveMixerInput(track);
      track.Dispose();
    }

    _playing.Clear();
    _outputDevice?.Stop();
    _outputDevice?.Dispose();
    _outputDevice = null;

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  private void FrameworkOnUpdate(IFramework framework)
  {
    foreach (TrackableSound track in _playing.Values)
      UpdateTrack(track);
  }

  private void OnTerritoryChanged(ushort _)
  {
    StopAll();
  }

  private unsafe Task UpdateTrack(TrackableSound track)
  {
    return _framework.RunOnFrameworkThread(() =>
    {
      if (track.IsStopping) return;
      track.Volume = (track.Message.VoicelinePath == null ? _configuration.LocalTTSVolume : _configuration.Volume) / 100f;

      if (
        (track.Message.Source == MessageSource.AddonMiniTalk && _configuration.DirectionalAudioForAddonMiniTalk) ||
        (track.Message.Source == MessageSource.ChatMessage && _configuration.DirectionalAudioForChat)
      )
      {
        Camera* camera = CameraManager.Instance()->GetActiveCamera();
        if (camera == null) return;

        Character* speaker = (Character*)_gameInteropService.TryFindCharacter_NoThreadCheck(track.Message.Speaker, track.Message.NpcData?.BaseId ?? 0);
        if (speaker == null) return;

        if (_clientState.LocalPlayer == null) return;

        if (track.Message.OriginalSpeaker == _clientState.LocalPlayer.Name.ToString()) return;

        Vector3 playerPosition = _clientState.LocalPlayer.Position;
        Vector3 speakerPosition = new(speaker->Position.X, speaker->Position.Y, speaker->Position.Z);

        FFXIVClientStructs.FFXIV.Common.Math.Matrix4x4 cameraViewMatrix = camera->CameraBase.SceneCamera.ViewMatrix;
        Vector3 cameraForward = Vector3.Normalize(new Vector3(cameraViewMatrix.M13, cameraViewMatrix.M23, cameraViewMatrix.M33));
        Vector3 cameraUp = Vector3.Normalize(camera->CameraBase.SceneCamera.Vector_1);
        Vector3 cameraRight = Vector3.Normalize(Vector3.Cross(cameraUp, cameraForward));

        Vector3 relativePosition = speakerPosition - playerPosition;

        float distance = relativePosition.Length();

        float dotProduct = Vector3.Dot(relativePosition, cameraRight);
        float balance = Math.Clamp(dotProduct / 20, -1, 1);

        float volume = track.Volume;

        (float distanceStart, float distanceEnd, float volumeStart, float volumeEnd)[] volumeRanges =
        {
          (0f, 3f, volume*1f, volume*0.85f), // 0 to 3 units: 100% to 85%
          (3f, 5f, volume*0.85f, volume*0.3f), // 3 to 5 units: 85% to 30%
          (5f, 20f, volume*0.3f, volume*0.05f) // 5 to 20 units: 30% to 5%
        };

        foreach ((float distanceStart, float distanceEnd, float volumeStart, float volumeEnd) in volumeRanges)
        {
          if (distance >= distanceStart && distance <= distanceEnd)
          {
            float slope = (volumeEnd - volumeStart) / (distanceEnd - distanceStart);
            float yIntercept = volumeStart - (slope * distanceStart);
            float _volume = (slope * distance) + yIntercept;
            volume = Math.Clamp(_volume, Math.Min(volumeStart, volumeEnd), Math.Max(volumeStart, volumeEnd));
            break;
          }
        }

        if (volume == track.Volume)
          volume = volumeRanges[^1].volumeEnd;

        // Logger.Debug($"Updating track: volume::{volume} pan::{balance}");

        track.Volume = volume;
        track.Pan = balance;
      }
    });
  }

  public async Task Play(XivMessage message, bool replay = false)
  {
    if (_mixer == null || _outputDevice == null)
    {
      _logger.Error("Mixer or OutputDevice were not initialited.");
      return;
    }

    string? voicelinePath = message.VoicelinePath;
    bool isLocalTTS = voicelinePath == null;
    if (isLocalTTS) voicelinePath = await _localTTSService.WriteLocalTTSToDisk(message);
    if (voicelinePath == null) return; // LocalTTS generation failed

    WaveStream? sourceStream = await _audioPostProcessor.PostProcessToPCM(voicelinePath, isLocalTTS, message);
    if (isLocalTTS) File.Delete(voicelinePath);
    if (sourceStream == null) return; // AudioPostProcessor failed

    if (_playing.TryRemove(message.Id, out TrackableSound? oldTrack))
    {
      _mixer.RemoveMixerInput(oldTrack);
      oldTrack.Dispose();
    }

    // We only allow one AddonTalk line to play at a time
    if (message.Source == MessageSource.AddonTalk)
      Stop(MessageSource.AddonTalk);

    TrackableSound track = new(_logger, message, sourceStream);
    await UpdateTrack(track);
    track.OnPlaybackStopped += t =>
    {
      // Apparently no need to call .RemoveMixerInput, it seems to automagically remove itself
      // when playback is completed. Calling .RemoveMixerInput here does not cause any exceptions
      // but any future lines will be broken.
      t.Dispose();
      _playing.TryRemove(message.Id, out _);
      _logger.Debug($"Finished playing message: {message.Id}");

      PlaybackCompleted?.Invoke(this, message.Source);
    };

    PlaybackStarted?.Invoke(this, message.Source);
    _mixer.AddMixerInput(track);
    _playing[message.Id] = track;

    if (_configuration.LipSyncEnabled)
      _lipSync.TryLipSync(message, track.TotalTime.TotalSeconds);

    if (!replay)
    {
      lock (_playbackHistoryLock)
      {
        int existingIndex = _playbackHistory.FindIndex(m => m.Id == message.Id);
        if (existingIndex != -1)
          _playbackHistory.RemoveAt(existingIndex);

        _playbackHistory.Insert(0, message);
        if (_playbackHistory.Count > 100)
          _playbackHistory.RemoveAt(_playbackHistory.Count - 1);
      }
    }
  }

  public void StopAll()
  {
    _logger.Debug($"Stopping all playing audio");
    foreach (TrackableSound track in _playing.Values)
      _ = FadeOutAndStopAsync(track);
  }

  public void Stop(MessageSource source)
  {
    _logger.Debug($"Stopping playing audio from source {source}");
    foreach (TrackableSound track in _playing.Values)
      if (track.Message.Source == source)
        _ = FadeOutAndStopAsync(track);
  }

  public void Stop(string id)
  {
    if (_playing.TryRemove(id, out TrackableSound? track))
      _ = FadeOutAndStopAsync(track);
    else
      _logger.Debug($"Failed to find playing audio with id {id}");
  }

  public IEnumerable<(XivMessage Message, bool IsPlaying, TimeSpan CurrentTime, TimeSpan TotalTime)> GetPlaybackHistory()
  {
    lock (_playbackHistoryLock)
    {
      foreach (XivMessage message in _playbackHistory)
      {
        if (_playing.TryGetValue(message.Id, out var track))
          yield return (message, track.IsPlaying, track.CurrentTime, track.TotalTime);
        else
          yield return (message, false, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
      }
    }
  }

  public IEnumerable<TrackableSound> Debug_GetPlaying()
  {
    foreach (TrackableSound track in _playing.Values)
      yield return track;
  }

  public int Debug_GetMixerSourceCount()
  {
    return _mixer?.MixerInputs.Count() ?? -1;
  }

  private async Task FadeOutAndStopAsync(TrackableSound track, int fadeDurationMs = 150)
  {
    track.IsStopping = true;
    _lipSync.TryStopLipSync(track.Message);

    const int intervalMs = 25;
    int steps = fadeDurationMs / intervalMs;
    float initialVolume = track.Volume;

    for (int i = 0; i < steps; i++)
    {
      float newVolume = initialVolume * (1 - (float)(i + 1) / steps);
      track.Volume = newVolume;
      await Task.Delay(intervalMs);
    }

    track.Volume = 0;

    _mixer?.RemoveMixerInput(track);
    track.OnPlaybackStopped = null;
    track.Dispose();

    string key = _playing.FirstOrDefault(kvp => kvp.Value == track).Key;
    if (key != null)
      _playing.TryRemove(key, out _);

    _logger.Debug("Track faded out and stopped.");
  }
}

public class TrackableSound : ISampleProvider, IDisposable
{
  private readonly ILogger _logger;

  private readonly ISampleProvider _innerProvider;
  private readonly VolumeSampleProvider _volumeProvider;
  private readonly PanningSampleProvider _panningProvider;

  private bool _playbackEnded = false;
  private float _currentVolume = 1.0f;
  private float _currentPan = 0.0f;

  public XivMessage Message { get; }
  public WaveStream SourceStream { get; }
  public bool IsStopping { get; set; } = false;

  public Action<TrackableSound>? OnPlaybackStopped;

  public TimeSpan CurrentTime => SourceStream.CurrentTime;
  public TimeSpan TotalTime => SourceStream.TotalTime;

  public TrackableSound(ILogger logger, XivMessage message, WaveStream sourceStream)
  {
    _logger = logger;
    Message = message;
    SourceStream = sourceStream;

    ISampleProvider sourceSampleProvider = sourceStream.ToSampleProvider();
    if (sourceSampleProvider.WaveFormat.SampleRate != 48000)
    {
      _logger.Debug($"Resampling from {sourceSampleProvider.WaveFormat.SampleRate}hz to 48000hz");
      sourceSampleProvider = new WdlResamplingSampleProvider(sourceSampleProvider, 48000);
    }
    _innerProvider = sourceSampleProvider;

    _volumeProvider = new VolumeSampleProvider(_innerProvider) { Volume = 1.0f };
    _panningProvider = new PanningSampleProvider(_volumeProvider) { Pan = 0.0f };
  }

  public WaveFormat WaveFormat => _panningProvider.WaveFormat;

  public int Read(float[] buffer, int offset, int count)
  {
    int read = _panningProvider.Read(buffer, offset, count);
    if (!_playbackEnded && (read == 0 || SourceStream.Position >= SourceStream.Length))
    {
      _playbackEnded = true;
      OnPlaybackStopped?.Invoke(this);
    }

    return read;
  }

  public void Dispose() => SourceStream.Dispose();

  public bool IsPlaying => !IsStopping && !_playbackEnded && SourceStream.Position < SourceStream.Length;

  public float Volume
  {
    get => _currentVolume;
    set
    {
      _currentVolume = Math.Clamp(value, 0f, 1f);
      _volumeProvider.Volume = _currentVolume;
    }
  }

  public float Pan
  {
    get => _currentPan;
    set
    {
      _currentPan = Math.Clamp(value, -1f, 1f);
      _panningProvider.Pan = _currentPan;
    }
  }
}
