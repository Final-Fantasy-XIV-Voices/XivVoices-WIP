using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace XivVoices.Services;

public class ChatMessageProvider : IHostedService
{
  private readonly Logger Logger;
  private readonly MessageDispatcher MessageDispatcher;
  private readonly IChatGui ChatGui;

  public ChatMessageProvider(Logger logger, MessageDispatcher messageDispatcher, IChatGui chatGui)
  {
    Logger = logger;
    MessageDispatcher = messageDispatcher;
    ChatGui = chatGui;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    ChatGui.ChatMessage += OnChatMessage;

    Logger.Debug("ChatMessageProvider started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    ChatGui.ChatMessage -= OnChatMessage;

    Logger.Debug("ChatMessageProvider stopped");
    return Task.CompletedTask;
  }

  private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString sentence, ref bool isHandled)
  {
    string speaker = "";
    try
    {
      foreach (var item in sender.Payloads)
      {
        var player = item as PlayerPayload;
        var text = item as TextPayload;
        if (player != null)
        {
          speaker = player.PlayerName;
          break;
        }

        if (text != null && text.Text != null)
        {
          speaker = text.Text;
          break;
        }
      }
    }
    catch { }

    // TODO: handle type == XivChatType.NPCDialogue
    // TODO: handle type == XivChatType.NPCDialogueAnnouncements
    // ^ the old plugin does this at least.

    switch (type)
    {
      case XivChatType.Say:
      case XivChatType.TellIncoming:
      // case XivChatType.TellOutgoing:
      case XivChatType.Shout:
      case XivChatType.Yell:
      case XivChatType.Party:
      case XivChatType.CrossParty:
      case XivChatType.Alliance:
      case XivChatType.FreeCompany:
      case XivChatType.CrossLinkShell1:
      case XivChatType.CrossLinkShell2:
      case XivChatType.CrossLinkShell3:
      case XivChatType.CrossLinkShell4:
      case XivChatType.CrossLinkShell5:
      case XivChatType.CrossLinkShell6:
      case XivChatType.CrossLinkShell7:
      case XivChatType.CrossLinkShell8:
      case XivChatType.Ls1:
      case XivChatType.Ls2:
      case XivChatType.Ls3:
      case XivChatType.Ls4:
      case XivChatType.Ls5:
      case XivChatType.Ls6:
      case XivChatType.Ls7:
      case XivChatType.Ls8:
        _ = MessageDispatcher.TryDispatch(MessageSource.ChatMessage, speaker, sentence.ToString());
        break;
    }
  }
}
