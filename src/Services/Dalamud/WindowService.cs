namespace XivVoices.Services;

public interface IWindowService : IHostedService;

public class WindowService(ILogger _logger, IDataService _dataService, IDalamudPluginInterface _pluginInterface, WindowSystem _windowSystem, ConfigWindow _configWindow) : IWindowService
{
  public Task StartAsync(CancellationToken cancellationToken)
  {
    _windowSystem.AddWindow(_configWindow);

    _pluginInterface.UiBuilder.Draw += UiBuilderOnDraw;
    _pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

    if (_dataService.DataDirectory == null)
    {
      _configWindow.IsOpen = true;
      _configWindow.SelectedTab = ConfigWindowTab.Overview;
      _logger.Debug("DataDirectory does not exist, opening Setup");
    }

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
    _pluginInterface.UiBuilder.Draw -= UiBuilderOnDraw;

    _windowSystem.RemoveAllWindows();

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  private void ToggleConfigUi()
  {
    _configWindow.Toggle();
  }

  private void UiBuilderOnDraw()
  {
    _windowSystem.Draw();
  }
}
