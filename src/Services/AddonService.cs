using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text;

namespace XivVoices.Services;

// TODO: addonbattletalk
// TODO: bubbles (there are multiple kinds i am kinda lost on them)
// TODO: chat
public class AddonService : IHostedService
{
  private readonly Logger Logger;
  private readonly IAddonLifecycle AddonLifecycle;
  private readonly DataService DataService;
  private readonly IChatGui ChatGui;

  public AddonService(Logger logger, IAddonLifecycle addonLifecycle, DataService dataService, IChatGui chatGui)
  {
    Logger = logger;
    AddonLifecycle = addonLifecycle;
    DataService = dataService;
    ChatGui = chatGui;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Talk", OnTalkAddonPostRefresh);
    ChatGui.ChatMessage += OnChatMessage;

    Logger.Debug("AddonService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    AddonLifecycle.UnregisterListener(OnTalkAddonPostRefresh);
    ChatGui.ChatMessage -= OnChatMessage;

    Logger.Debug("AddonService stopped");
    return Task.CompletedTask;
  }

  private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString sentence, ref bool isHandled)
  {
    var speaker = "";
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

        if (text != null)
        {
          speaker = text.Text;
          break;
        }
      }
    } catch { }

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
        DataService.ProcessMessage(speaker, sentence.ToString(), MessageSource.Chat);
        break;
    }
  }

  private static unsafe string ReadTextNode(AtkTextNode* textNode)
  {
    if (textNode == null) return "";
    var seString = textNode->NodeText.StringPtr.AsDalamudSeString();
    return seString.TextValue
      .Trim()
      .Replace("\n", "")
      .Replace("\r", "");
  }

  private unsafe void OnTalkAddonPostRefresh(AddonEvent type, AddonArgs args)
  {
    var addon = (AddonTalk*)args.Addon;
    if (addon == null) return;

    // TODO: if this line is voiced by the game, do not proceed here.

    var speaker = ReadTextNode(addon->AtkTextNode220);
    var sentence = ReadTextNode(addon->AtkTextNode228);

    Logger.Debug($"speaker::{speaker} sentence::{sentence}");

    DataService.ProcessMessage(speaker, sentence, MessageSource.AddonTalk);
  }
}
