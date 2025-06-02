using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Concentus.Oggfile;
using Concentus.Structs;
using System.IO;

namespace XivVoices.Services;

public class SpeechService : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;

  private readonly object _playbackLock = new();
  private IWavePlayer? _currentAudioOutput;
  private WaveStream? _currentWaveStream;

  public SpeechService(Logger logger, Configuration configuration)
  {
    Logger = logger;
    Configuration = configuration;
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

  public void Speak(string voiceline, IGameObject? gameObject)
  {
    Logger.Debug($"Speak: {voiceline}");

    _currentAudioOutput?.Stop();
    _currentAudioOutput?.Dispose();
    _currentAudioOutput = null;

    _currentWaveStream?.Dispose();
    _currentWaveStream = null;

    try
    {
      _currentWaveStream = DecodeOggOpusToPCM(voiceline);

      var sampleProvider = _currentWaveStream.ToSampleProvider();
      var volumeProvider = new VolumeSampleProvider(sampleProvider)
      {
        Volume = 0.4f // 40% TODO: scale by ingame voice audio setting
      };

      _currentAudioOutput = new WasapiOut(); // TODO: Support other engines
      _currentAudioOutput.Init(volumeProvider);
      _currentAudioOutput.Play();
    }
    catch (Exception ex)
    {
      Logger.Error($"Failed to play voice line '{voiceline}': {ex.Message}");
    }
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
