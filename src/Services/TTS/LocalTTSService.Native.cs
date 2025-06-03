namespace XivVoices.Services;

public partial class LocalTTSService
{
  private IntPtr _libraryHandle;

  private delegate IntPtr LocalTTSStartDelegate();
  private delegate LocalTTSResult LocalTTSText2AudioDelegate(IntPtr ctx, IntPtr text, IntPtr voice);
  private delegate IntPtr LocalTTSLoadVoiceDelegate(IntPtr configBuffer, uint configBufferSize, IntPtr modelBuffer, uint modelBufferSize);
  private delegate void LocalTTSSetSpeakerIdDelegate(IntPtr voice, long speakerId);
  private delegate void LocalTTSFreeVoiceDelegate(IntPtr voice);
  private delegate void LocalTTSFreeResultDelegate(LocalTTSResult result);
  private delegate void LocalTTSFreeDelegate(IntPtr ctx);

  private LocalTTSStartDelegate? _start;
  private LocalTTSText2AudioDelegate? _textToAudio;
  private LocalTTSLoadVoiceDelegate? _loadVoice;
  private LocalTTSSetSpeakerIdDelegate? _setSpeakerId;
  private LocalTTSFreeVoiceDelegate? _freeVoice;
  private LocalTTSFreeResultDelegate? _freeResult;
  private LocalTTSFreeDelegate? _free;

  [StructLayout(LayoutKind.Sequential)]
  public struct LocalTTSResult
  {
    public uint Channels;
    public uint SampleRate;
    public uint LengthSamples;
    public IntPtr Samples;
  }

  private void InitializeLocalTTSInterop()
  {
    string path = $"{Configuration.ToolsDirectory}/localtts.dll";
    _libraryHandle = NativeLibrary.Load(path);

    _start = GetFunction<LocalTTSStartDelegate>("localtts_start");
    _textToAudio = GetFunction<LocalTTSText2AudioDelegate>("localtts_text_2_audio");
    _loadVoice = GetFunction<LocalTTSLoadVoiceDelegate>("localtts_load_voice");
    _setSpeakerId = GetFunction<LocalTTSSetSpeakerIdDelegate>("localtts_set_speaker_id");
    _freeVoice = GetFunction<LocalTTSFreeVoiceDelegate>("localtts_free_voice");
    _freeResult = GetFunction<LocalTTSFreeResultDelegate>("localtts_free_result");
    _free = GetFunction<LocalTTSFreeDelegate>("localtts_free");

    Logger.Debug("LocalTTS library loaded");
  }

  public void DisposeLocalTTSInterop()
  {
    if (_libraryHandle != IntPtr.Zero)
      NativeLibrary.Free(_libraryHandle);

    Logger.Debug("LocalTTS library unloaded");
  }

  private T GetFunction<T>(string name) where T : Delegate
  {
    IntPtr ptr = NativeLibrary.GetExport(_libraryHandle, name);
    return Marshal.GetDelegateForFunctionPointer<T>(ptr);
  }

  public IntPtr LocalTTSStart() => _start!();
  public LocalTTSResult LocalTTSText2Audio(IntPtr ctx, IntPtr text, IntPtr voice) => _textToAudio!(ctx, text, voice);
  public IntPtr LocalTTSLoadVoice(IntPtr configBuffer, uint configSize, IntPtr modelBuffer, uint modelSize) => _loadVoice!(configBuffer, configSize, modelBuffer, modelSize);
  public void LocalTTSSetSpeakerId(IntPtr voice, long speakerId) => _setSpeakerId!(voice, speakerId);
  public void LocalTTSFreeVoice(IntPtr voice) => _freeVoice!(voice);
  public void LocalTTSFreeResult(LocalTTSResult result) => _freeResult!(result);
  public void LocalTTSFree(IntPtr ctx) => _free!(ctx);
}
