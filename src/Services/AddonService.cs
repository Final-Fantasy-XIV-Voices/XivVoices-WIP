using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace XivVoices.Services;

// TODO: addonbattletalk
// TODO: bubbles (there are multiple kinds i am kinda lost on them)
// TODO: chat
public class AddonService : IHostedService
{
  private readonly Logger Logger;
  private readonly IAddonLifecycle AddonLifecycle;
  private readonly DataService DataService;

  public AddonService(Logger logger, IAddonLifecycle addonLifecycle, DataService dataService)
  {
    Logger = logger;
    AddonLifecycle = addonLifecycle;
    DataService = dataService;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Talk", OnTalkAddonPostRefresh);

    Logger.Debug("AddonService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    AddonLifecycle.UnregisterListener(OnTalkAddonPostRefresh);

    Logger.Debug("AddonService stopped");
    return Task.CompletedTask;
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

    var speaker = ReadTextNode(addon->AtkTextNode220);
    var sentence = ReadTextNode(addon->AtkTextNode228);

    Logger.Debug($"speaker::{speaker} sentence::{sentence}");

    DataService.ProcessMessage(speaker, sentence);
  }
}
