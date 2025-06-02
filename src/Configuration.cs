using Dalamud.Configuration;

namespace XivVoices;

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;
  public bool Debug { get; set; } = false;
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
