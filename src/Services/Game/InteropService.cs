using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace XivVoices.Services;

public class InteropService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly IClientState ClientState;
  private readonly IObjectTable ObjectTable;
  private readonly IFramework Framework;
  private readonly ICondition Condition;
  private readonly IGameGui GameGui;

  public InteropService(Logger logger, Configuration configuration, IClientState clientState, IObjectTable objectTable, IFramework framework, ICondition condition, IGameGui gameGui)
  {
    Logger = logger;
    Configuration = configuration;
    ClientState = clientState;
    ObjectTable = objectTable;
    Framework = framework;
    Condition = condition;
    GameGui = gameGui;
  }

  public Task<ICharacter?> TryFindCharacterByName(string name)
  {
    return Framework.RunOnFrameworkThread(() =>
    {
      foreach (IGameObject gameObject in ObjectTable)
      {
        if (gameObject as ICharacter == null || gameObject.Name.TextValue == "") continue;
        if (gameObject.Name.TextValue == name)
        {
          return gameObject as ICharacter;
        }
      }

      return null;
    });
  }

  public bool IsInCutscene()
  {
    return Condition.Any(ConditionFlag.OccupiedInCutSceneEvent, ConditionFlag.WatchingCutscene, ConditionFlag.WatchingCutscene78);
  }

  public unsafe void AutoAdvance()
  {
    Framework.RunOnFrameworkThread(() =>
    {
      AddonTalk* addonTalk = (AddonTalk*)GameGui.GetAddonByName("Talk");
      if (addonTalk == null) return;
      var evt = stackalloc AtkEvent[1]
      {
        new()
        {
          Listener = (AtkEventListener*)addonTalk,
          Target = &AtkStage.Instance()->AtkEventTarget,
          State = new()
          {
            StateFlags = (AtkEventStateFlags)132
          }
        }
      };
      var data = stackalloc AtkEventData[1];
      for (var i = 0; i < sizeof(AtkEventData); i++)
      {
        ((byte*)data)[i] = 0;
      }
      addonTalk->ReceiveEvent(AtkEventType.MouseDown, 0, evt, data);
      addonTalk->ReceiveEvent(AtkEventType.MouseClick, 0, evt, data);
      addonTalk->ReceiveEvent(AtkEventType.MouseUp, 0, evt, data);
    });
  }
}
