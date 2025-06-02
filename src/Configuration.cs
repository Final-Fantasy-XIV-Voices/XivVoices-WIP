using Dalamud.Configuration;

namespace XivVoices;

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;

  // Opt-out if you really hate the "spam" or whatever.
  // It's easier if users have this enabled by default and can send logs after something goes wrong,
  // instead of having to replicate it after toggling this option.
  public bool Debug { get; set; } = true;

  public bool IsSetupComplete = false;
  public bool ReplaceVoicedARRCutscenes = true;

  [NonSerialized]
  private Logger? Logger;
  [NonSerialized]
  private IDalamudPluginInterface? PluginInterface;

  public void Initialize(Logger logger, IDalamudPluginInterface pluginInterface)
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
  public static void Migrate(Configuration configuration, Logger logger)
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
