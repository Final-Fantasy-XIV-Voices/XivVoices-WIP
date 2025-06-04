using NAudio.Wave;

namespace XivVoices.Services;

// TODO: split this localtts stuff into files nicer.

public partial class LocalTTSService : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly IFramework Framework;
  private readonly IClientState ClientState;
  private readonly DataService DataService;

  private IntPtr Context;
  private readonly object Lock = new object();
  private bool Disposed { get; set; }

  private LocalTTSVoice?[] LocalTTSVoices = new LocalTTSVoice[2];

  public LocalTTSService(Logger logger, Configuration configuration, IFramework framework, IClientState clientState, DataService dataService)
  {
    Logger = logger;
    Configuration = configuration;
    Framework = framework;
    ClientState = clientState;
    DataService = dataService;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    lock (Lock)
    {
      InitializeLocalTTSInterop();

      Context = LocalTTSStart();

      LocalTTSVoices[0] = new(Configuration.LocalTTSVoiceMale, Logger, Configuration, this);
      LocalTTSVoices[1] = new(Configuration.LocalTTSVoiceFemale, Logger, Configuration, this);
    }

    Logger.Debug("LocalTTSService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    lock (Lock)
    {
      Disposed = true;

      if (Context != IntPtr.Zero)
        LocalTTSFree(Context);

      LocalTTSVoices[0]?.Dispose();
      LocalTTSVoices[0] = null;
      LocalTTSVoices[1]?.Dispose();
      LocalTTSVoices[1] = null;

      DisposeLocalTTSInterop();
    }

    Logger.Debug("LocalTTSService stopped");
    return Task.CompletedTask;
  }

  // returns a path to the temporary .wav output
  // this path should be deleted once you're done with it.
  // can return null if it failed to generate.
  public async Task<string?> WriteLocalTTSToDisk(XivMessage message)
  {
    string sentence = Regex.Replace(message.Sentence, "[“”]", "\"");
    sentence = await ProcessPlayerChat(sentence, message.Speaker);
    // TODO: ProcessPlayerChat
    sentence = ApplyLexicon(sentence);

    // Remove anything that's not a letter, number, space, ',' or '.'
    sentence = new string(sentence.Where(c => char.IsLetterOrDigit(c) || c == ',' || c == '.' || c == ' ').ToArray());
    if (!sentence.Any(char.IsLetter))
    {
      Logger.Error($"Failed to clean local tts message: {message.Sentence} -> {sentence}");
      return null;
    }

    int speaker = Configuration.LocalTTSUngenderedVoice == "Male" ? 0 : 1;
    if (message.NpcData != null) speaker = (message.NpcData.Gender == "Male" ? 0 : 1);

    if (LocalTTSVoices[speaker] == null)
    {
      Logger.Debug($"LocalTTSVoice {speaker} was not loaded");
      return null;
    }

    var pcmData = await SpeakTTS(sentence, LocalTTSVoices[speaker]!);

    var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(22050, 1);
    var tempFilePath = Path.Join(Configuration.DataDirectory, $"localtts-{Guid.NewGuid()}.wav");
    using (var waveFileWriter = new WaveFileWriter(tempFilePath, waveFormat))
    {
      foreach (var sample in pcmData)
        waveFileWriter.WriteSample(sample);
    }

    return tempFilePath;
  }

  public async Task<float[]> SpeakTTS(string text, LocalTTSVoice voice)
  {
    Logger.Debug($"TTS for '{text}'");
    var units = SSMLPreprocessor.Preprocess(text);
    var samples = new List<float>();
    TTSResult result = null!;
    foreach (var unit in units)
    {
      result = await SpeakSamples(unit, voice);
      samples.AddRange(result.Samples);
    }

    Logger.Debug($"Done. Returned '{samples.Count}' samples .. result.SampleRate {result.SampleRate}");
    return samples.ToArray();
  }

  public async Task<TTSResult> SpeakSamples(SpeechUnit unit, LocalTTSVoice voice)
  {
    var tcs = new TaskCompletionSource<TTSResult>();

    float[] samples = null!;
    using var textPtr = new FixedString(unit.Text);
    var result = new LocalTTSResult
    {
      Channels = 0
    };

    await Task.Run(() =>
    {
      lock (Lock)
      {
        try
        {
          voice.AcquireReaderLock();
          if (Disposed || voice.Disposed)
          {
            samples = Array.Empty<float>();
            Logger.Error("Couldn't process TTS. TTSEngine or LocalTTSVoice has been disposed.");
            return;
          }
          ValidatePointer(voice.Pointer, "Voice pointer is null.");
          ValidatePointer(voice.ConfigPointer!.Address, "Config pointer is null.");
          ValidatePointer(voice.ModelPointer!.Address, "Model pointer is null.");
          ValidatePointer(Context, "Context pointer is null.");
          ValidatePointer(textPtr.Address, "Text pointer is null.");
          result = LocalTTSText2Audio(Context, textPtr.Address, voice.Pointer);
          samples = PtrToSamples(result.Samples, result.LengthSamples);
          voice.ReleaseReaderLock();
        }
        catch (Exception ex)
        {
          Logger.Error($"Error while processing TTS: {ex}");
          tcs.SetException(ex);
        }
      }
    });

    tcs.SetResult(new TTSResult
    {
      Channels = result.Channels,
      SampleRate = result.SampleRate,
      Samples = samples
    });

    LocalTTSFreeResult(result);
    textPtr.Dispose();
    return await tcs.Task;
  }

  private void ValidatePointer(IntPtr pointer, string errorMessage)
  {
    if (pointer == IntPtr.Zero)
      throw new InvalidOperationException(errorMessage);
  }

  private float[] PtrToSamples(IntPtr int16Buffer, uint samplesLength)
  {
    var floatSamples = new float[samplesLength];
    var int16Samples = new short[samplesLength];

    Marshal.Copy(int16Buffer, int16Samples, 0, (int)samplesLength);

    for (int i = 0; i < samplesLength; i++)
    {
      floatSamples[i] = int16Samples[i] / (float)short.MaxValue;
    }

    return floatSamples;
  }
}

public class FixedString : IDisposable
{
  public IntPtr Address { get; private set; }

  public FixedString(string text)
  {
    Address = Marshal.StringToHGlobalAnsi(text);
  }

  public void Dispose()
  {
    if (Address == IntPtr.Zero) return;
    Marshal.FreeHGlobal(Address);
    Address = IntPtr.Zero;
  }
}

public class TTSResult
{
  public required float[] Samples;
  public required uint Channels;
  public required uint SampleRate;
}
