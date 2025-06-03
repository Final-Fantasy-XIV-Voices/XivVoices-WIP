using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace XivVoices.Services;

// TODO: lipsync
public class PlaybackService : IHostedService
{
  private readonly Logger Logger;
  private readonly InteropService InteropService;
  private readonly LocalTTSService LocalTTSService;
  private readonly AudioPostProcessor AudioPostProcessor;

  private WaveOutEvent? OutputDevice;
  private MixingSampleProvider? Mixer;

  private readonly ConcurrentDictionary<string, TrackableSound> Playing = new();
  private readonly object PlaybackHistoryLock = new();
  private readonly List<XivMessage> PlaybackHistory = new();

  public PlaybackService(Logger logger, InteropService interopService, LocalTTSService localTTSService, AudioPostProcessor audioPostProcessor)
  {
    Logger = logger;
    InteropService = interopService;
    LocalTTSService = localTTSService;
    AudioPostProcessor = audioPostProcessor;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    OutputDevice = new WaveOutEvent();
    Mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1))
    {
      ReadFully = true
    };

    OutputDevice.Init(Mixer);
    OutputDevice.Play();

    Logger.Debug("PlaybackService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    foreach (var track in Playing.Values)
    {
      Mixer?.RemoveMixerInput(track);
      track.Dispose();
    }

    Playing.Clear();
    OutputDevice?.Stop();
    OutputDevice?.Dispose();
    OutputDevice = null;

    Logger.Debug("PlaybackService stopped");
    return Task.CompletedTask;
  }

  public async Task Play(XivMessage message, bool replay = false)
  {
    if (Mixer == null || OutputDevice == null)
    {
      Logger.Error("Mixer or OutputDevice were not initialited.");
      return;
    }

    string? voicelinePath = message.VoicelinePath;
    bool isLocalTTS = voicelinePath == null;
    if (isLocalTTS) voicelinePath = await LocalTTSService.WriteLocalTTSToDisk(message);
    if (voicelinePath == null) return; // LocalTTS generation failed

    WaveStream? sourceStream = await AudioPostProcessor.PostProcessToPCM(voicelinePath, isLocalTTS, message);
    if (isLocalTTS) File.Delete(voicelinePath);
    if (sourceStream == null) return; // AudioPostProcessor failed

    if (Playing.TryRemove(message.Id, out var oldTrack))
    {
      Mixer.RemoveMixerInput(oldTrack);
      oldTrack.Dispose();
    }

    // We only allow one AddonTalk line to play at a time
    if (message.Source == MessageSource.AddonTalk)
    {
      Stop(MessageSource.AddonTalk);
    }

    var track = new TrackableSound(message.Source, sourceStream, 0.5f); // TODO: read volume from config
    track.OnPlaybackStopped += t =>
    {
      // Apparently no need to call .RemoveMixerInput, it seems to automagically remove itself
      // when playback is completed. Calling .RemoveMixerInput here does not cause any exceptions
      // but any future lines will be broken.
      t.Dispose();
      Playing.TryRemove(message.Id, out _);
      Logger.Debug($"Finished playing message: {message.Id}");

      if (message.Source == MessageSource.AddonTalk)
      {
        Logger.Debug("AddonTalk message finished playing fully, auto-advancing.");
        InteropService.AutoAdvance();
      }
    };

    Mixer.AddMixerInput(track);
    Playing[message.Id] = track;

    if (!replay)
    {
      lock (PlaybackHistory)
      {
        var existingIndex = PlaybackHistory.FindIndex(m => m.Id == message.Id);
        if (existingIndex != -1)
          PlaybackHistory.RemoveAt(existingIndex);

        PlaybackHistory.Insert(0, message);
        if (PlaybackHistory.Count > 100)
          PlaybackHistory.RemoveAt(PlaybackHistory.Count - 1);
      }
    }
  }

  public void Stop(MessageSource source)
  {
    Logger.Debug($"Stopping playing audio from source {source}");
    foreach (var kvp in Playing.ToArray())
    {
      var id = kvp.Key;
      var track = kvp.Value;

      if (track.MessageSource == source)
      {
        _ = FadeOutAndStopAsync(track);
        //   Logger.Debug($"Stopped playing audio from source {source}");
        //   Mixer?.RemoveMixerInput(track);
        //   track.OnPlaybackStopped = null;
        //   track.Dispose();
        //   Playing.TryRemove(id, out _);
      }
    }
  }

  public void Stop(string id)
  {
    if (Playing.TryRemove(id, out var track))
    {
      _ = FadeOutAndStopAsync(track);
      // Logger.Debug($"Stopping playing audio with id {id}");
      // Mixer?.RemoveMixerInput(track);
      // track.OnPlaybackStopped = null;
      // track.Dispose();
    }
    else
    {
      Logger.Debug($"Failed to find playing audio with id {id}");
    }
  }

  public IEnumerable<(XivMessage Message, bool IsPlaying, TimeSpan CurrentTime, TimeSpan TotalTime)> GetPlaybackHistory()
  {
    lock (PlaybackHistoryLock)
    {
      foreach (var message in PlaybackHistory)
      {
        if (Playing.TryGetValue(message.Id, out var track))
          yield return (message, track.IsPlaying, track.CurrentTime, track.TotalTime);
        else
          yield return (message, false, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
      }
    }
  }

  public IEnumerable<TrackableSound> Debug_GetPlaying()
  {
    foreach (var track in Playing.Values)
      yield return track;
  }

  public int Debug_GetMixerSourceCount()
  {
    return Mixer?.MixerInputs.Count() ?? -1;
  }

  private async Task FadeOutAndStopAsync(TrackableSound track, int fadeDurationMs = 150)
  {
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

    Mixer?.RemoveMixerInput(track);
    track.OnPlaybackStopped = null;
    track.Dispose();

    var key = Playing.FirstOrDefault(kvp => kvp.Value == track).Key;
    if (key != null)
      Playing.TryRemove(key, out _);

    Logger.Debug("Track faded out and stopped.");
  }
}

public class TrackableSound : ISampleProvider, IDisposable
{
  public MessageSource MessageSource { get; }
  public WaveStream SourceStream { get; }

  private readonly ISampleProvider InnerProvider;
  private readonly VolumeSampleProvider VolumeProvider;

  private bool PlaybackEnded = false;
  private float CurrentVolume;

  public Action<TrackableSound>? OnPlaybackStopped;

  public TimeSpan CurrentTime => SourceStream.CurrentTime;
  public TimeSpan TotalTime => SourceStream.TotalTime;

  public TrackableSound(MessageSource messageSource, WaveStream sourceStream, float volume)
  {
    MessageSource = messageSource;
    SourceStream = sourceStream;
    InnerProvider = sourceStream.ToSampleProvider();
    CurrentVolume = volume;
    VolumeProvider = new VolumeSampleProvider(InnerProvider) { Volume = CurrentVolume };
  }

  public WaveFormat WaveFormat => InnerProvider.WaveFormat;

  public int Read(float[] buffer, int offset, int count)
  {
    int read = VolumeProvider.Read(buffer, offset, count);
    if (!PlaybackEnded && (read == 0 || SourceStream.Position >= SourceStream.Length))
    {
      PlaybackEnded = true;
      OnPlaybackStopped?.Invoke(this);
    }

    return read;
  }

  public void Dispose() => SourceStream.Dispose();

  public bool IsPlaying => !PlaybackEnded && SourceStream.Position < SourceStream.Length;

  public float Volume
  {
    get => CurrentVolume;
    set
    {
      CurrentVolume = value;
      VolumeProvider.Volume = CurrentVolume;
    }
  }
}
