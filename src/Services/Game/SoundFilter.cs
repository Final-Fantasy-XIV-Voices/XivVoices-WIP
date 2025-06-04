using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;

namespace XivVoices.Services;

// Cheers to 'SoundFilter' plugin.
// This intercepts all sounds that are loaded and played,
// allowing us to block XIVV's voices if a line is voiced,
// and to block ARR's in-game voices.
public class SoundFilter : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly IGameInteropProvider InteropProvider;

  private const int ResourceDataPointerOffset = 0xB0;
  private ConcurrentDictionary<IntPtr, string> Scds = new();

  private IntPtr NoSoundPtr;
  private IntPtr InfoPtr;

  public event EventHandler<InterceptedSound>? OnCutsceneAudioDetected;

  public SoundFilter(Logger logger, Configuration configuration, IGameInteropProvider interopProvider)
  {
    Logger = logger;
    Configuration = configuration;
    InteropProvider = interopProvider;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    var (noSoundPtr, infoPtr) = SetUpNoSound();
    NoSoundPtr = noSoundPtr;
    InfoPtr = infoPtr;

    InteropProvider.InitializeFromAttributes(this);
    GetResourceSyncHook.Enable();
    GetResourceAsyncHook.Enable();
    LoadSoundFileHook.Enable();
    PlaySpecificSoundHook.Enable();

    Logger.Debug("SoundFilter started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    GetResourceSyncHook?.Dispose();
    GetResourceAsyncHook?.Dispose();
    LoadSoundFileHook?.Dispose();
    PlaySpecificSoundHook?.Dispose();

    Marshal.FreeHGlobal(InfoPtr);
    Marshal.FreeHGlobal(NoSoundPtr);

    Logger.Debug("SoundFilter stopped");
    return Task.CompletedTask;
  }

  private byte[] GetNoSoundScd()
  {
    var assembly = Assembly.GetExecutingAssembly();
    string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("nosound.scd"));
    var noSound = assembly.GetManifestResourceStream(resourceName)!;
    using var memoryStream = new MemoryStream();
    noSound.CopyTo(memoryStream);
    return memoryStream.ToArray();
  }

  private (IntPtr noSoundPtr, IntPtr infoPtr) SetUpNoSound()
  {
    // get the data of an empty scd
    var noSound = GetNoSoundScd();

    // allocate unmanaged memory for this data and copy the data into the memory
    var noSoundPtr = Marshal.AllocHGlobal(noSound.Length);
    Marshal.Copy(noSound, 0, noSoundPtr, noSound.Length);

    // allocate some memory for feeding into the play sound function
    var infoPtr = Marshal.AllocHGlobal(256);
    // write a pointer to the empty scd
    Marshal.WriteIntPtr(infoPtr + 8, noSoundPtr);
    // specify where the game should offset from for the sound index
    Marshal.WriteInt32(infoPtr + 0x88, 0x54);
    // specify the number of sounds in the file
    Marshal.WriteInt16(infoPtr + 0x94, 0);

    return (noSoundPtr, infoPtr);
  }

  private unsafe delegate void* GetResourceSyncDelegate(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown);
  [Signature("E8 ?? ?? ?? ?? 48 8B D8 8B C7", DetourName = nameof(GetResourceSyncDetour))]
  private readonly Hook<GetResourceSyncDelegate> GetResourceSyncHook = null!;
  private unsafe void* GetResourceSyncDetour(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown)
  {
    var ret = GetResourceSyncHook.Original(pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown);
    GetResourceDetourInner(ret, pPath);
    return ret;
  }

  private unsafe delegate void* GetResourceAsyncDelegate(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown);
  [Signature("E8 ?? ?? ?? ?? 48 8B D8 EB 07 F0 FF 83", DetourName = nameof(GetResourceAsyncDetour))]
  private readonly Hook<GetResourceAsyncDelegate> GetResourceAsyncHook = null!;
  private unsafe void* GetResourceAsyncDetour(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown)
  {
    var ret = GetResourceAsyncHook.Original(pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, isUnknown);
    GetResourceDetourInner(ret, pPath);
    return ret;
  }

  private unsafe void GetResourceDetourInner(void* ret, char* pPath)
  {
    if (ret != null && EndsWithDotScd((byte*)pPath))
    {
      var scdData = Marshal.ReadIntPtr((IntPtr)ret + ResourceDataPointerOffset);
      // if we immediately have the scd data, cache it, otherwise add it to a waiting list to hopefully be picked up at sound play time
      if (scdData != IntPtr.Zero)
      {
        Scds[scdData] = ReadTerminatedString((byte*)pPath);
      }
    }
  }

  private unsafe delegate IntPtr LoadSoundFileDelegate(IntPtr resourceHandle, uint a2);
  [Signature("E8 ?? ?? ?? ?? 48 85 C0 75 12 B0 F6", DetourName = nameof(LoadSoundFileDetour))]
  private readonly Hook<LoadSoundFileDelegate> LoadSoundFileHook = null!;
  private unsafe IntPtr LoadSoundFileDetour(IntPtr resourceHandle, uint a2)
  {
    var ret = LoadSoundFileHook.Original(resourceHandle, a2);
    try
    {
      var handle = (ResourceHandle*)resourceHandle;
      var name = handle->FileName.ToString();
      if (name.EndsWith(".scd"))
      {
        var dataPtr = Marshal.ReadIntPtr(resourceHandle + ResourceDataPointerOffset);
        Scds[dataPtr] = name;
      }
    }
    catch (Exception ex)
    {
      Logger.Error(ex.ToString());
    }

    return ret;
  }

  private unsafe delegate void* PlaySpecificSoundDelegate(long a1, int idx);
  [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 33 F6 8B DA 48 8B F9 0F BA E2 0F", DetourName = nameof(PlaySpecificSoundDetour))]
  private readonly Hook<PlaySpecificSoundDelegate> PlaySpecificSoundHook = null!;
  private unsafe void* PlaySpecificSoundDetour(long a1, int idx)
  {
    try
    {
      var shouldFilter = PlaySpecificSoundDetourInner(a1, idx);
      if (shouldFilter)
      {
        a1 = InfoPtr;
        idx = 0;
      }
    }
    catch (Exception ex)
    {
      Logger.Error(ex.ToString());
    }

    return PlaySpecificSoundHook.Original(a1, idx);
  }

  private unsafe bool PlaySpecificSoundDetourInner(long a1, int idx)
  {
    if (a1 == 0) return false;

    var scdData = *(byte**)(a1 + 8);
    if (scdData == null) return false;

    if (!Scds.TryGetValue((IntPtr)scdData, out var path)) return false;

    path = path.ToLowerInvariant();
    var specificPath = $"{path}/{idx}";

    return ShouldFilter(specificPath);
  }

  private bool ShouldFilter(string path)
  {
    if ((path.Contains("vo_voiceman") || path.Contains("vo_man") || path.Contains("se_vfx_monster") || path.Contains("vo_line")) || path.Contains("cut/ffxiv/"))
    {
      if ((path.Contains("vo_man") || (path.Contains("cut/ffxiv/") && path.Contains("vo_voiceman"))) && Configuration.ReplaceVoicedARRCutscenes)
      {
        OnCutsceneAudioDetected?.Invoke(this, new InterceptedSound() { SoundPath = path, BlockAddonTalk = false });
        Logger.Debug("Blocking voiced ARR line in favor of XIVV");
        return true;
      }
      else
      {
        OnCutsceneAudioDetected?.Invoke(this, new InterceptedSound() { SoundPath = path, BlockAddonTalk = true });
      }
    }

    return false;
  }

  private unsafe bool EndsWithDotScd(byte* pPath)
  {
    if (pPath == null) return false;

    int len = 0;
    while (pPath[len] != 0) len++;

    if (len < 4) return false;

    return pPath[len - 4] == (byte)'.' &&
      pPath[len - 3] == (byte)'s' &&
      pPath[len - 2] == (byte)'c' &&
      pPath[len - 1] == (byte)'d';
  }

  private unsafe byte[] ReadTerminatedBytes(byte* ptr)
  {
    if (ptr == null)
    {
      return [];
    }

    var bytes = new List<byte>();
    while (*ptr != 0)
    {
      bytes.Add(*ptr);
      ptr += 1;
    }

    return [.. bytes];
  }

  private unsafe string ReadTerminatedString(byte* ptr)
  {
    return Encoding.UTF8.GetString(ReadTerminatedBytes(ptr));
  }
}
