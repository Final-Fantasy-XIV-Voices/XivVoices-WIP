using Dalamud.Game.Command;

namespace XivVoices.Services;

public class CommandService : IHostedService
{
  private string XivVoicesCommand = "/xivvoices";
  private string XivVoicesCommandAlias = "/xivv";

  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly ICommandManager CommandManager;
  private readonly ConfigWindow ConfigWindow;
  private readonly SetupWindow SetupWindow;

  public CommandService(Logger logger, Configuration configuration, ICommandManager commandManager, ConfigWindow configWindow, SetupWindow setupWindow)
  {
    Logger = logger;
    Configuration = configuration;
    CommandManager = commandManager;
    ConfigWindow = configWindow;
    SetupWindow = setupWindow;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    CommandManager.AddHandler(XivVoicesCommand, new CommandInfo(OnCommand)
    {
      HelpMessage = $"See '{XivVoicesCommand} help' for more."
    });
    CommandManager.AddHandler(XivVoicesCommandAlias, new CommandInfo(OnCommand)
    {
      HelpMessage = $"Alias for {XivVoicesCommand}."
    });

    Logger.Debug("EventService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    CommandManager.RemoveHandler(XivVoicesCommand);
    CommandManager.RemoveHandler(XivVoicesCommandAlias);

    Logger.Debug("EventService stopped");
    return Task.CompletedTask;
  }

  private void OnCommand(string command, string arguments)
  {
    Logger.Debug($"command::'{command}' arguments::'{arguments}'");

    string[] args = arguments.Split(" ", StringSplitOptions.RemoveEmptyEntries);
    if (args.Length == 0)
    {
      if (Configuration.IsSetupComplete) ConfigWindow.Toggle();
      else SetupWindow.Toggle();
      return;
    }

    switch (args[0])
    {
      case "help":
        Logger.Chat("Available commands:");
        Logger.Chat($"  {command} help");
        Logger.Chat($"  {command}");
        break;
      default:
        Logger.Chat("Invalid command:");
        Logger.Chat($"  {command} {arguments}");
        goto case "help";
    }
  }
}
