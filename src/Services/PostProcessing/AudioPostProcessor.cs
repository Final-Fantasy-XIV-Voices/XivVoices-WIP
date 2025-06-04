using Concentus;
using Concentus.Oggfile;
using NAudio.Wave;

namespace XivVoices.Services;

public partial class AudioPostProcessor : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly IDalamudPluginInterface PluginInterface;

  public AudioPostProcessor(Logger logger, Configuration configuration, IDalamudPluginInterface pluginInterface)
  {
    Logger = logger;
    Configuration = configuration;
    PluginInterface = pluginInterface;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _ = FFmpegStart();

    Logger.Debug("AudioPostProcessor started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _ = FFmpegStop();

    Logger.Debug("AudioPostProcessor stopped");
    return Task.CompletedTask;
  }

  public async Task<WaveStream?> PostProcessToPCM(string voicelinePath, bool isLocalTTS, XivMessage message)
  {
    string filterArguments = GetFFmpegFilterArguments(message, isLocalTTS);
    Logger.Debug($"FFmpeg filter arguments: {filterArguments}");

    // LocalTTS needs to be resampled so this .wav support here is kinda unecessary, but I'll keep it just in case.
    if (String.IsNullOrEmpty(filterArguments) && !isLocalTTS)
      return voicelinePath.EndsWith(".ogg") ? DecodeOggOpusToPCM(voicelinePath) : DecodeWavIeeeToPCM(voicelinePath);

    var tempFilePath = Path.Join(Configuration.DataDirectory, $"ffmpeg-{Guid.NewGuid()}.ogg");

    string filterComplexFlag = String.IsNullOrEmpty(filterArguments) ? "" : $"-filter_complex \"{filterArguments}\"";
    string ffmpegArguments = $"-i \"{voicelinePath}\" {filterComplexFlag} -ar 48000 -c:a libopus \"{tempFilePath}\"";

    await ExecuteFFmpegCommand(ffmpegArguments);
    WaveStream waveStream = DecodeOggOpusToPCM(tempFilePath);

    File.Delete(tempFilePath);

    return waveStream;
  }

  public static WaveStream DecodeOggOpusToPCM(string filePath)
  {
    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    {
      IOpusDecoder decoder = OpusCodecFactory.CreateDecoder(48000, 1);
      OpusOggReadStream oggStream = new OpusOggReadStream(decoder, fileStream);

      List<float> pcmSamples = new List<float>();

      while (oggStream.HasNextPacket)
      {
        short[] packet = oggStream.DecodeNextPacket();
        if (packet != null)
        {
          foreach (var sample in packet)
          {
            pcmSamples.Add(sample / 32768f);
          }
        }
      }

      var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
      var stream = new MemoryStream();
      using (var writer = new BinaryWriter(stream, System.Text.Encoding.Default, leaveOpen: true))
      {
        foreach (var sample in pcmSamples)
        {
          writer.Write(sample);
        }
      }
      stream.Position = 0;
      return new RawSourceWaveStream(stream, waveFormat);
    }
  }

  public static WaveStream DecodeWavIeeeToPCM(string filePath)
  {
    byte[] wavBytes = File.ReadAllBytes(filePath);
    var memoryStream = new MemoryStream(wavBytes);
    var reader = new WaveFileReader(memoryStream);

    if (reader.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
      throw new InvalidOperationException("Expected IEEE float WAV format.");

    return reader;
  }
}
