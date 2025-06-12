using Dalamud.Game.Command;

namespace XivVoices.Services;

public interface ICommandService : IHostedService;

public class CommandService(ILogger _logger, Configuration _configuration, ICommandManager _commandManager, ConfigWindow _configWindow, IPlaybackService _playbackService) : ICommandService
{
  private const string XivVoicesCommand = "/xivvoices";
  private const string XivVoicesCommandAlias = "/xivv";

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _commandManager.AddHandler(XivVoicesCommand, new CommandInfo(OnCommand)
    {
      HelpMessage = $"See '{XivVoicesCommand} help' for more."
    });
    _commandManager.AddHandler(XivVoicesCommandAlias, new CommandInfo(OnCommand)
    {
      HelpMessage = $"Alias for {XivVoicesCommand}."
    });

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _commandManager.RemoveHandler(XivVoicesCommand);
    _commandManager.RemoveHandler(XivVoicesCommandAlias);

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  private void OnCommand(string command, string arguments)
  {
    _logger.Debug($"command::'{command}' arguments::'{arguments}'");

    string[] args = arguments.Split(" ", StringSplitOptions.RemoveEmptyEntries);
    if (args.Length == 0)
    {
      _configWindow.Toggle();
      return;
    }

    // TODO: add commands to open each config window tab (only show /xivv wine if Util.IsWine())
    switch (args[0])
    {
      case "help":
        _logger.Chat("Available commands:");
        _logger.Chat($"  {command} help - Display this help menu");
        _logger.Chat($"  {command} mute - Toggle the muted state");
        _logger.Chat($"  {command}");
        break;
      case "mute":
        bool mute = !_configuration.Muted;
        _configuration.Muted = mute;
        _configuration.Save();
        if (mute) _playbackService.StopAll();
        _logger.Chat(mute ? "Muted" : "Unmuted");
        break;
      default:
        _logger.Chat("Invalid command:");
        _logger.Chat($"  {command} {arguments}");
        goto case "help";
    }
  }
}
