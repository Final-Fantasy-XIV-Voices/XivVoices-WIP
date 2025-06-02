namespace XivVoices.Services;

public class ReportService : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;

  public ReportService(Logger logger, Configuration configuration)
  {
    Logger = logger;
    Configuration = configuration;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    // TODO: idk honestly, might not need to be hosted

    Logger.Debug("ReportService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.Debug("ReportService stopped");
    return Task.CompletedTask;
  }

  public void Report(string speaker, string sentence, NpcData? npcData, IGameObject? gameObject)
  {
    Logger.Chat($"Reporting: {speaker}::{sentence}");
  }
}
