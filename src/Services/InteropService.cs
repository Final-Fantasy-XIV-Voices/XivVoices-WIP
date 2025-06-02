using Dalamud.Game.ClientState.Conditions;

namespace XivVoices.Services;

public class InteropService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly IClientState ClientState;
  private readonly IObjectTable ObjectTable;
  private readonly DataMapper DataMapper;
  private readonly IFramework Framework;
  private readonly ICondition Condition;

  public InteropService(Logger logger, Configuration configuration, IClientState clientState, IObjectTable objectTable, DataMapper dataMapper, IFramework framework, ICondition condition)
  {
    Logger = logger;
    Configuration = configuration;
    ClientState = clientState;
    ObjectTable = objectTable;
    DataMapper = dataMapper;
    Framework = framework;
    Condition = condition;
  }

  public Task<IGameObject?> GetGameObjectByName(string name)
  {
    return Framework.RunOnFrameworkThread(() => {
      foreach (IGameObject gameObject in ObjectTable)
      {
        if (gameObject as ICharacter == null || gameObject as ICharacter == ClientState.LocalPlayer || gameObject.Name.TextValue == "") continue;
        if (gameObject.Name.TextValue == name)
        {
          return gameObject;
        }
      }

      return null;
    });
  }

  public unsafe Task<NpcData?> GetNpcDataFromGameObject(IGameObject? gameObject)
  {
    return Framework.RunOnFrameworkThread(() => {
      if (gameObject == null) return null;

      ICharacter character = gameObject as ICharacter;
      string speaker = gameObject.Name.TextValue;

      bool gender = Convert.ToBoolean(character.Customize[(int)CustomizeIndex.Gender]);
      byte race = character.Customize[(int)CustomizeIndex.Race];
      byte tribe = character.Customize[(int)CustomizeIndex.Tribe];
      byte body = character.Customize[(int)CustomizeIndex.ModelType];
      byte eyes = character.Customize[(int)CustomizeIndex.EyeShape];

      NpcData npcData = new NpcData
      {
        Gender = DataMapper.GetGender(gender),
        Race = DataMapper.GetRace(race),
        Tribe = DataMapper.GetTribe(tribe),
        Body = DataMapper.GetBody(body),
        Eyes = DataMapper.GetEyes(eyes),
        Type = DataMapper.GetBody(body) == "Elderly" ? "Old" : "Default"
      };

      if (npcData.Body == "Beastman")
      {
        int skeletonId = ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)character.Address)->ModelContainer.ModelSkeletonId;
        npcData.Race = DataMapper.GetSkeleton(skeletonId, ClientState.TerritoryType);

        // I would like examples for why these workarounds are necessary,
        // but as it stands this is copied from old XIVV
        if (speaker.Contains("Moogle"))
          npcData.Race = "Moogle";
      }

      return npcData;
    });
  }

  public bool IsInCutscene()
  {
    return Condition.Any(ConditionFlag.OccupiedInCutSceneEvent, ConditionFlag.WatchingCutscene, ConditionFlag.WatchingCutscene78);
  }
}
