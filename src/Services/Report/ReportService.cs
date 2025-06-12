using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;

namespace XivVoices.Services;

public interface IReportService : IHostedService
{
  void Report(XivMessage message);
  void ReportWithReason(XivMessage message, string reason);
}

// TODO: dont report files locally twice, and preferably remember if you already reported them to the server as to also not report them then
// TODO: this should probably just try to upload immediately, and fall back to storing locally to be uploaded later.

public class ReportService(ILogger _logger, Configuration _configuration, IDataService _dataService, IClientState _clientState, IDalamudPluginInterface _pluginInterface, IFramework _framework, IDataManager _dataManager, IGameInteropService _gameInteropService) : IReportService
{
  private bool _languageWarningThisSession = false;
  private bool _invalidPluginsWarningsThisSession = false;

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  private readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };
  private void SaveReport(XivReport report)
  {
    string? reportsDirectory = _dataService.ReportsDirectory;
    if (reportsDirectory == null)
    {
      _logger.Chat("Failed to save report. No ReportDirectory was found.");
      return;
    }

    string fileName = $"report_{report.Date}_{Guid.NewGuid()}.json";
    string filePath = Path.Join(reportsDirectory, fileName);

    string json = JsonSerializer.Serialize(report, _writeOptions);
    File.WriteAllText(filePath, json);
  }

  private bool CanReport()
  {
    if (_clientState.ClientLanguage != Dalamud.Game.ClientLanguage.English)
    {
      if (!_languageWarningThisSession)
      {
        _logger.Chat("Unable to report. Your Client's Language is not set to English.");
        _languageWarningThisSession = true;
      }
      return false;
    }

    List<string> invalidPlugins = ["Echoglossian"];
    List<string> loadedPlugins = [];
    foreach (string invalidPlugin in invalidPlugins)
      if (_pluginInterface.InstalledPlugins.Any(pluginInfo => pluginInfo.InternalName == invalidPlugin && pluginInfo.IsLoaded))
        loadedPlugins.Add(invalidPlugin);

    if (loadedPlugins.Count > 0)
    {
      if (!_invalidPluginsWarningsThisSession)
      {
        _logger.Chat($"Unable to report. You have the following unsupported plugins installed: '{string.Join(", ", loadedPlugins)}'");
        _invalidPluginsWarningsThisSession = true;
      }
      return false;
    }

    return true;
  }

  public void Report(XivMessage message)
  {
    if (!_configuration.EnableAutomaticReports)
    {
      _logger.Debug("Not reporting message due to automatic reports being turned off.");
      return;
    }

    _framework.RunOnFrameworkThread(() =>
    {
      if (!CanReport() || !_clientState.IsLoggedIn || _clientState.LocalPlayer == null) return;

      if (_configuration.LogReportsToChat)
        _logger.Chat($"Reporting: {message.Speaker}: {message.Sentence}");

      TerritoryType territory = _dataManager.GetExcelSheet<TerritoryType>().GetRow(_clientState.TerritoryType);
      string location = $"{territory.PlaceNameRegion.Value.Name.ExtractText()}, {territory.PlaceName.Value.Name.ExtractText()}";
      Vector3 coordsVec3 = MapUtil.GetMapCoordinates(_clientState.LocalPlayer);
      string coordinates = $"X: {coordsVec3.X} Y: {coordsVec3.Y}";

      List<string> activeQuests = [];
      unsafe
      {
        foreach (QuestWork quest in QuestManager.Instance()->NormalQuests)
        {
          if (quest.QuestId is 0) continue;
          Quest questData = _dataManager.GetExcelSheet<Quest>().GetRow(quest.QuestId + 65536u);
          activeQuests.Add(questData.Name.ExtractText());
        }
      }

      XivReport report = new(
        message,
        location,
        coordinates,
        _gameInteropService.IsInCutscene(),
        _gameInteropService.IsInDuty(),
        activeQuests
      );

      SaveReport(report);
    });
  }

  public void ReportWithReason(XivMessage message, string reason)
  {
    if (!CanReport()) return;
    _logger.Chat($"Report submitted with reason: {reason}");

    SaveReport(new(message, reason));
  }
}
