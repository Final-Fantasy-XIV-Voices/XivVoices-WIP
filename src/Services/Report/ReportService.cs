namespace XivVoices.Services;

public class ReportService : IHostedService
{
  private readonly Logger Logger;

  // private bool EchoglossianWarningThisSession = false;

  public ReportService(Logger logger)
  {
    Logger = logger;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    Logger.Debug("PlaybackService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.Debug("PlaybackService stopped");
    return Task.CompletedTask;
  }

  public void Report(XivMessage message)
  {
    Logger.Chat($"Reporting: {message.Speaker}: {message.Sentence}");
    // TODO:
    // Dont report if echoglossian is installed and enabled (do warn the user of this once a session i guess?)
    // get zone, coords, active quests
  }
}
