namespace XivVoices.Services;

public class WindowService : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly IDalamudPluginInterface PluginInterface;
  private readonly WindowSystem WindowSystem;
  private readonly ConfigWindow ConfigWindow;
  private readonly SetupWindow SetupWindow;

  public WindowService(Logger logger, Configuration configuration, IDalamudPluginInterface pluginInterface, WindowSystem windowSystem, ConfigWindow configWindow, SetupWindow setupWindow)
  {
    Logger = logger;
    Configuration = configuration;
    PluginInterface = pluginInterface;
    WindowSystem = windowSystem;
    ConfigWindow = configWindow;
    SetupWindow = setupWindow;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    WindowSystem.AddWindow(ConfigWindow);
    WindowSystem.AddWindow(SetupWindow);

    PluginInterface.UiBuilder.Draw += UiBuilderOnDraw;
    PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

    if (!Configuration.IsSetupComplete)
    {
      SetupWindow.IsOpen = true;
    }

    Logger.Debug("WindowService started");
    return Task.CompletedTask;
  }

  private void ToggleConfigUi()
  {
    if (!Configuration.IsSetupComplete) SetupWindow.Toggle();
    else ConfigWindow.Toggle();
  }

  private void UiBuilderOnDraw()
  {
    WindowSystem.Draw();
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
    PluginInterface.UiBuilder.Draw -= UiBuilderOnDraw;

    WindowSystem.RemoveAllWindows();

    Logger.Debug("WindowService stopped");
    return Task.CompletedTask;
  }
}
