namespace XivVoices.Services;

public interface ILocalTTSVoice : IDisposable
{
  IntPtr Pointer { get; }
  void SetSpeakerId(int speakerId);
  void AcquireReaderLock();
  void ReleaseReaderLock();
}

public class LocalTTSVoice : ILocalTTSVoice
{
  private const int Timeout = 8000;
  private readonly ReaderWriterLock Lock = new();

  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly LocalTTSService LocalTTSService;

  public IntPtr Pointer { get; private set; }
  public FixedPointerToHeapAllocatedMem? ConfigPointer { get; private set; }
  public FixedPointerToHeapAllocatedMem? ModelPointer { get; private set; }
  public bool Disposed { get; private set; }

  public LocalTTSVoice(
    string voiceName,
    Logger logger,
    Configuration configuration,
    LocalTTSService localTTSService
  )
  {
    Logger = logger;
    Configuration = configuration;
    LocalTTSService = localTTSService;

    LoadVoice(voiceName);
  }

  private void LoadVoice(string voiceName)
  {
    var modelPath = Path.Combine(Configuration.ToolsDirectory, $"{voiceName}.bytes");
    var configPath = Path.Combine(Configuration.ToolsDirectory, $"{voiceName}.config.json");

    if (!File.Exists(modelPath))
      throw new FileNotFoundException($"Missing voice model: {modelPath}");

    if (!File.Exists(configPath))
      throw new FileNotFoundException($"Missing config: {configPath}");

    byte[] modelBytes = File.ReadAllBytes(modelPath);
    byte[] configBytes = File.ReadAllBytes(configPath);

    ConfigPointer = FixedPointerToHeapAllocatedMem.Create(configBytes, (uint)configBytes.Length);
    ModelPointer = FixedPointerToHeapAllocatedMem.Create(modelBytes, (uint)modelBytes.Length);

    Pointer = LocalTTSService.LocalTTSLoadVoice(
      ConfigPointer.Address, ConfigPointer.SizeInBytes,
      ModelPointer.Address, ModelPointer.SizeInBytes
    );

    LocalTTSService.LocalTTSSetSpeakerId(Pointer, 0);
    Logger.Debug($"Voice '{voiceName}' loaded successfully.");
  }

  public void SetSpeakerId(int speakerId) =>
    LocalTTSService.LocalTTSSetSpeakerId(Pointer, speakerId);

  public void AcquireReaderLock()
  {
    try { Lock.AcquireReaderLock(Timeout); }
    catch { throw; }
  }

  public void ReleaseReaderLock() =>
    Lock.ReleaseReaderLock();

  public void Dispose()
  {
    Lock.AcquireWriterLock(Timeout);
    if (Disposed) return;

    Disposed = true;

    ConfigPointer?.Free();
    ModelPointer?.Free();
    LocalTTSService.LocalTTSFreeVoice(Pointer);

    Lock.ReleaseWriterLock();
    Logger.Debug("Voice resources disposed.");
  }
}

public class FixedPointerToHeapAllocatedMem : IDisposable
{
  private GCHandle _handle;
  public IntPtr Address { get; private set; }

  public void Free()
  {
    _handle.Free();
    Address = IntPtr.Zero;
  }

  public static FixedPointerToHeapAllocatedMem Create<T>(T Object, uint SizeInBytes)
  {
    var pointer = new FixedPointerToHeapAllocatedMem
    {
      _handle = GCHandle.Alloc(Object, GCHandleType.Pinned),
      SizeInBytes = SizeInBytes
    };
    pointer.Address = pointer._handle.AddrOfPinnedObject();
    return pointer;
  }

  public void Dispose()
  {
    if (_handle.IsAllocated)
    {
      _handle.Free();
      Address = IntPtr.Zero;
    }
  }

  public uint SizeInBytes { get; private set; }
}
