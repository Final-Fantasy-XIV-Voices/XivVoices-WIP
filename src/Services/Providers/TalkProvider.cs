using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace XivVoices.Services;

public class TalkProvider : IHostedService
{
  private readonly Logger Logger;
  private readonly IAddonLifecycle AddonLifecycle;
  private readonly MessageDispatcher MessageDispatcher;
  private readonly PlaybackService PlaybackService;
  private readonly IGameGui GameGui;
  private readonly IFramework Framework;

  private bool AddonTalkLastVisible = false;

  public TalkProvider(Logger logger, IAddonLifecycle addonLifecycle, MessageDispatcher messageDispatcher, PlaybackService playbackService, IGameGui gameGui, IFramework framework)
  {
    Logger = logger;
    AddonLifecycle = addonLifecycle;
    MessageDispatcher = messageDispatcher;
    PlaybackService = playbackService;
    GameGui = gameGui;
    Framework = framework;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Talk", OnTalkAddonPostRefresh);
    Framework.Update += OnFrameworkUpdate;

    Logger.Debug("TalkProvider started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    AddonLifecycle.UnregisterListener(OnTalkAddonPostRefresh);
    Framework.Update -= OnFrameworkUpdate;

    Logger.Debug("TalkProvider stopped");
    return Task.CompletedTask;
  }

  private int UpdateCounter = 0;
  private const int UpdateInterval = 5;
  private unsafe void OnFrameworkUpdate(IFramework _)
  {
    UpdateCounter++;
    if (UpdateCounter >= UpdateInterval)
    {
      UpdateCounter = 0;

      // Stop playing AddonTalk voicelines if it was clicked away.
      AddonTalk* addonTalk = (AddonTalk*)GameGui.GetAddonByName("Talk");
      if (addonTalk != null)
      {
        bool visible = addonTalk->AtkUnitBase.IsVisible;
        if (AddonTalkLastVisible != visible)
        {
          AddonTalkLastVisible = visible;
          if (visible == false)
          {
            Logger.Debug("AddonTalk was clicked away.");
            PlaybackService.Stop(MessageSource.AddonTalk);
          }
        }
      }
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

  // TODO: pick up split-lines like the sidurgu one, as it stands i will have to code this for the existing generated lines
  private unsafe void OnTalkAddonPostRefresh(AddonEvent type, AddonArgs args)
  {
    var addon = (AddonTalk*)args.Addon;
    if (addon == null) return;

    var speaker = ReadTextNode(addon->AtkTextNode220);
    var sentence = ReadTextNode(addon->AtkTextNode228);

    Logger.Debug($"speaker::{speaker} sentence::{sentence}");

    AddonTalkLastVisible = true;
    _ = MessageDispatcher.TryDispatch(MessageSource.AddonTalk, speaker, sentence);
  }
}
