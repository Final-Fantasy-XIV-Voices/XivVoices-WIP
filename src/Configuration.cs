using Dalamud.Configuration;

namespace XivVoices;

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;

  // Dialogue Settings
  public bool QueueChatMessages = true;
  public bool ChatSayEnabled = true;
  public bool ChatTellEnabled = true;
  public bool ChatShoutYellEnabled = true;
  public bool ChatPartyEnabled = true;
  public bool ChatAllianceEnabled = true;
  public bool ChatFreeCompanyEnabled = true;
  public bool ChatLinkshellEnabled = true;
  public bool ChatEmoteEnabled = true;

  public bool AddonTalkEnabled = true;
  public bool AddonBattleTalkEnabled = true;
  public bool AddonMiniTalkEnabled = true;

  public bool AddonTalkTTSEnabled = true;
  public bool AddonBattleTalkTTSEnabled = true;
  public bool AddonMiniTalkTTSEnabled = true;

  public bool AddonTalkSystemEnabled = true;
  public bool AddonBattleTalkSystemEnabled = true;

  public bool AutoAdvanceEnabled = true;
  public bool RetainersEnabled = true;
  public bool ReplaceVoicedARRCutscenes = true;

  // Audio Settings
  public bool Muted = false;
  public bool LipSyncEnabled = false;

  public int Speed = 100;
  public int Volume = 100;

  public bool DirectionalAudioForChat = false;
  public bool DirectionalAudioForAddonMiniTalk = true;

  public bool LocalTTSEnabled = true;
  public string LocalTTSDefaultVoice = "Male";
  public int LocalTTSVolume = 100;
  public int LocalTTSSpeed = 100;

  public bool LocalTTSPlayerSays = true;

  // Audio Logs
  public bool EnableAutomaticReports = true;
  public bool LogReportsToChat = true;

  // Wine Settings
  public bool WineUseNativeFFmpeg = true;

  // Debug Settings
  public string? DataDirectory = null;
  public bool DebugLogging { get; set; } = true;
  public bool DebugMode { get; set; } = false;
  public string ServerUrl = "http://127.0.0.1:6969";
  public string LocalTTSVoiceMale = "en-gb-northern_english_male-medium";
  public string LocalTTSVoiceFemale = "en-gb-jenny_dioco-medium";

  [NonSerialized]
  private ILogger? Logger;
  [NonSerialized]
  private IDalamudPluginInterface? PluginInterface;

  public void Initialize(ILogger logger, IDalamudPluginInterface pluginInterface)
  {
    Logger = logger;
    PluginInterface = pluginInterface;

    Logger.Configuration = this;
    ConfigurationMigrator.Migrate(this, Logger!);
  }

  public void Save() => PluginInterface!.SavePluginConfig(this);
}

public static class ConfigurationMigrator
{
  public static void Migrate(Configuration configuration, ILogger logger)
  {
    // if (configuration.Version == 0)
    // {
    // }
    // else
    {
      logger.Debug($"Configuration up-to-date: v{configuration.Version}");
    }
  }
}
