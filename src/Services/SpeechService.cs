using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Concentus.Oggfile;
using Concentus.Structs;
using System.IO;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace XivVoices.Services;

// TODO: move stuff from here and addonservice that use gamegui into like, idk. GameGuiService. idfk.

public class SpeechService : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly IFramework Framework;
  private readonly IGameGui GameGui;

  private readonly object _playbackLock = new();
  private IWavePlayer? _currentAudioOutput;
  private WaveStream? _currentWaveStream;

  public SpeechService(Logger logger, Configuration configuration, IFramework framework, IGameGui gameGui)
  {
    Logger = logger;
    Configuration = configuration;
    Framework = framework;
    GameGui = gameGui;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    // TODO: probably load localtts voices on startup

    Logger.Debug("SpeechService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.Debug("SpeechService stopped");
    return Task.CompletedTask;
  }

  // TODO: we should allow multiple "sources" to speak at the same time, e.g. chat bubbles and dialogue, but not dialogue twice.
  public void Speak(string voiceline, IGameObject? gameObject)
  {
    Logger.Debug($"Speak: {voiceline}");

    StopPlaying();

    try
    {
      _currentWaveStream = DecodeOggOpusToPCM(voiceline);

      var sampleProvider = _currentWaveStream.ToSampleProvider();
      var volumeProvider = new VolumeSampleProvider(sampleProvider)
      {
        Volume = 0.4f // 40% TODO: scale by ingame voice audio setting
      };

      _currentAudioOutput = new WasapiOut(); // TODO: Support other engines

      _currentAudioOutput.PlaybackStopped += (sender, args) =>
      {
        Logger.Debug("Audio playback completed.");
        AutoAdvance(); // TODO: only if it was a addontalk message.
      };

      _currentAudioOutput.Init(volumeProvider);
      _currentAudioOutput.Play();
    }
    catch (Exception ex)
    {
      Logger.Error($"Failed to play voice line '{voiceline}': {ex.Message}");
    }
  }

  private unsafe void AutoAdvance()
  {
    Framework.RunOnFrameworkThread(() => {
      AddonTalk* addonTalk = (AddonTalk*)GameGui.GetAddonByName("Talk");
      if (addonTalk == null) return;
      var evt = stackalloc AtkEvent[1]
      {
        new()
        {
          Listener = (AtkEventListener*)addonTalk,
          Target = &AtkStage.Instance()->AtkEventTarget,
          State = new()
          {
            StateFlags = (AtkEventStateFlags)132
          }
        }
      };
      var data = stackalloc AtkEventData[1];
      for (var i =0 ; i < sizeof(AtkEventData); i++)
      {
        ((byte*)data)[i] = 0;
      }
      addonTalk->ReceiveEvent(AtkEventType.MouseDown, 0, evt, data);
      addonTalk->ReceiveEvent(AtkEventType.MouseClick, 0, evt, data);
      addonTalk->ReceiveEvent(AtkEventType.MouseUp, 0, evt, data);
    });
  }

  public void StopPlaying()
  {
    _currentAudioOutput?.Stop();
    _currentAudioOutput?.Dispose();
    _currentAudioOutput = null;

    _currentWaveStream?.Dispose();
    _currentWaveStream = null;
  }

  public void SpeakTTS(string speaker, string sentence, NpcData? npcData, IGameObject? gameObject)
  {
    // Logger.Toast($"SpeakTTS: {speaker}:{sentence}");
    Logger.Chat($"SpeakTTS: {speaker}:{sentence}");
  }

  public static WaveStream DecodeOggOpusToPCM(string filePath)
  {
    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    {
      // Initialize the decoder
      OpusDecoder decoder = new OpusDecoder(48000, 1); // Assuming a sample rate of 48000 Hz and mono audio
      OpusOggReadStream oggStream = new OpusOggReadStream(decoder, fileStream);

      // Buffer for storing the decoded samples
      List<float> pcmSamples = new List<float>();

      // Read and decode the entire file
      while (oggStream.HasNextPacket)
      {
        short[] packet = oggStream.DecodeNextPacket();
        if (packet != null)
        {
          foreach (var sample in packet)
          {
            pcmSamples.Add(sample / 32768f); // Convert to float and normalize
          }
        }
      }

      var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
      var stream = new MemoryStream();
      var writer = new BinaryWriter(stream);
      foreach (var sample in pcmSamples.ToArray())
      {
        writer.Write(sample);
      }
      stream.Position = 0;
      return new RawSourceWaveStream(stream, waveFormat);
    }
  }
}
