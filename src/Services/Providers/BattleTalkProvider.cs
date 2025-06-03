namespace XivVoices.Services;

public class BattleTalkProvider : IHostedService
{
  private readonly Logger Logger;

  public BattleTalkProvider(Logger logger)
  {
    Logger = logger;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    Logger.Debug("BattleTalkProvider started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.Debug("BattleTalkProvider stopped");
    return Task.CompletedTask;
  }
}
