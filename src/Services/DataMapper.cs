// TODO: GetVoiceFromNpcData (GetOtherVoiceNames in old xivv codebase), it will have to be that long because voices are randomly just differently named, great.
// TODO: all the stuff thats in interopservice rn. thanks.
public class DataMapper
{
  private readonly Logger Logger;

  public DataMapper(Logger logger)
  {
    Logger = logger;
  }

  private Dictionary<int, string> bodyMap = new Dictionary<int, string>()
  {
    {0, "Beastman"},
    {1, "Adult"},
    {3, "Elderly"},
    {4, "Child"},
  };

  private Dictionary<int, string> raceMap = new Dictionary<int, string>()
  {
    {1, "Hyur"},
    {2, "Elezen"},
    {3, "Lalafell"},
    {4, "Miqo'te"},
    {5, "Roegadyn"},
    {6, "Au Ra"},
    {7, "Hrothgar"},
    {8, "Viera"},
  };

  private Dictionary<int, string> tribeMap = new Dictionary<int, string>()
  {
    {1, "Midlander"},
    {2, "Highlander"},
    {3, "Wildwood"},
    {4, "Duskwight"},
    {5, "Plainsfolk"},
    {6, "Dunesfolk"},
    {7, "Seeker of the Sun"},
    {8, "Keeper of the Moon"},
    {9, "Sea Wolf"},
    {10, "Hellsguard"},
    {11, "Raen"},
    {12, "Xaela"},
    {13, "Helions"},
    {14, "The Lost"},
    {15, "Rava"},
    {16, "Veena"},
  };

  private Dictionary<int, string> eyesMap = new Dictionary<int, string>()
  {
    {0, "Option 1"},
    {1, "Option 2"},
    {2, "Option 3"},
    {3, "Option 4"},
    {4, "Option 5"},
    {5, "Option 6"},
    {128, "Option 1"},
    {129, "Option 2"},
    {130, "Option 3"},
    {131, "Option 4"},
    {132, "Option 5"},
    {133, "Option 6"},
  };

  private Dictionary<(int, ushort), string> skeletonRegionMap = new Dictionary<(int, ushort), string>()
  {
    {(21,0), "Golem"},
    {(21,478), "Golem"},
    {(60,0), "Dragon_Medium"}, // Medium size --> Sooh Non
    {(60,398), "Dragon_Medium"},
    {(63,0), "Dragon_Large"}, // Large size --> Ess Khas
    {(63,398), "Dragon_Large"},
    {(239,0), "Dragon_Small"}, // Small size --> Khash Thah
    {(239,398), "Dragon_Small"},

    {(278,0), "Node"},
    {(278,402), "Node"},

    {(405, 0), "Namazu"},
    {(494, 0), "Namazu"},
    {(494, 614), "Namazu"},
    {(494, 622), "Namazu"},

    {(706,0), "Ea"},
    {(706,960), "Ea"},

    {(11001, 0), "Amalj'aa"},
    {(11001, 146), "Amalj'aa"},
    {(11001, 401), "Vanu Vanu"},

    {(11002,0), "Ixal"},
    {(11002,154), "Ixal"},

    {(11003,0), "Kobold"},
    {(11003,180), "Kobold"},

    {(11004,0), "Goblin"},
    {(11004,478), "Goblin"},

    {(11005,0), "Sylph"},
    {(11005,152), "Sylph"},

    {(11006,0), "Moogle"},
    {(11006,400), "Moogle"},

    {(11007,0), "Sahagin"},
    {(11007,138), "Sahagin"},

    {(11008,0), "Mamool Ja"},
    {(11008,129), "Mamool Ja"},

    {(11009,0), "Matanga"},     // TODO: Find Areas for Them
    {(11009,1), "Giant"},       // TODO: Make a Giant Voice

    {(11012,0), "Qiqirn"},
    {(11013,0), "Qiqirn"},
    {(11013,139), "Qiqirn"},

    {(11016,0), "Skeleton"},    // TODO: Make a Dead Voice

    {(11020,0), "Vath"},
    {(11020,398), "Vath"},

    {(11028,0), "Kojin"},
    {(11028,613), "Kojin"},

    {(11029,0), "Ananta"},

    {(11030,0), "Lupin"},

    {(11037,0), "Nu Mou"},
    {(11037,816), "Nu Mou"},

    {(11038,0), "Pixie"},
    {(11038,816), "Pixie"},

    {(11051,0), "Omicron"},
    {(11051,960), "Omicron"},

    {(11052,0), "Loporrit"},
    {(11052,959), "Loporrit"}
  };

  public string GetGender(bool id) => id ? "Female" : "Male";
  public string GetBody(int id) => bodyMap.TryGetValue(id, out var name) ? name : "Adult";
  public string GetRace(int id) => raceMap.TryGetValue(id, out var name) ? name : "Unknown:" + id.ToString();
  public string GetTribe(int id) => tribeMap.TryGetValue(id, out var name) ? name : "Unknown:" + id.ToString();
  public string GetEyes(int id) => eyesMap.TryGetValue(id, out var name) ? name : "Unknown:" + id.ToString();
  public string GetSkeleton(int id, ushort region)
  {
    if (skeletonRegionMap.TryGetValue((id, region), out var name))
      return name;
    else if (skeletonRegionMap.TryGetValue((id, 0), out var defaultName))
      return defaultName;
    return "Unknown combination: ID " + id.ToString() + ", Region " + region.ToString();
  }

  /*
  This is 'GetOtherVoiceNames' from old xivv. Can't quite clean this up because voices are
  sometimes "_05_06" where two eyeshapes share a voice, or other such cases.
  This is what I would do otherwise:

  var validRaces = new string[] { "Au Ra", "Elezen", "Hrothgar", "Hyur", "Lalafell", "Miqo'te", "Roegadyn", "Viera" };
  var validTribes = new string[] { "Raen", "Xaela", "Duskwight", "Wildwood", "Helions", "The Lost", "Highlander", "Midlander", "Dunesfolk", "Plainsfolk", "Keeper of the Moon", "Seeker of the Sun", "Hellsguard", "Sea Wolf", "Rava", "Veena" };

  if (npcData.Body == "Adult")
  {
    if (validRaces.Contains(npcData.Race) && validTribes.Contains(npcData.Tribe))
      return $"{npcData.Race.Replace(" ", "_").Replace("'", "")}_{npcData.Tribe.Replace(" ", "_")_{npcData.Gender}_{npcData.Eyes.Replace("Option ", "0")}}"
  }

  ...
  */
  public string GetGenericVoice(NpcData npcData, string speaker)
  {
    if (npcData.Body == "Adult")
    {
      if (npcData.Race == "Au Ra")
      {
        if (npcData.Tribe == "Raen" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Au_Ra_Raen_Female_01";
        if (npcData.Tribe == "Raen" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Au_Ra_Raen_Female_02";
        if (npcData.Tribe == "Raen" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Au_Ra_Raen_Female_03";
        if (npcData.Tribe == "Raen" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Au_Ra_Raen_Female_04";
        if (npcData.Tribe == "Raen" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Au_Ra_Raen_Female_05";

        if (npcData.Tribe == "Raen" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Au_Ra_Raen_Male_01";
        if (npcData.Tribe == "Raen" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Au_Ra_Raen_Male_02";
        if (npcData.Tribe == "Raen" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Au_Ra_Raen_Male_03";
        if (npcData.Tribe == "Raen" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Au_Ra_Raen_Male_04";
        if (npcData.Tribe == "Raen" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Au_Ra_Raen_Male_05";
        if (npcData.Tribe == "Raen" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Au_Ra_Raen_Male_06";

        if (npcData.Tribe == "Xaela" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Au_Ra_Xaela_Female_01";
        if (npcData.Tribe == "Xaela" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Au_Ra_Xaela_Female_02";
        if (npcData.Tribe == "Xaela" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Au_Ra_Xaela_Female_03";
        if (npcData.Tribe == "Xaela" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Au_Ra_Xaela_Female_04";
        if (npcData.Tribe == "Xaela" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Au_Ra_Xaela_Female_05";

        if (npcData.Tribe == "Xaela" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Au_Ra_Xaela_Male_01";
        if (npcData.Tribe == "Xaela" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Au_Ra_Xaela_Male_02";
        if (npcData.Tribe == "Xaela" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Au_Ra_Xaela_Male_03";
        if (npcData.Tribe == "Xaela" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Au_Ra_Xaela_Male_04";
        if (npcData.Tribe == "Xaela" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Au_Ra_Xaela_Male_05";
        if (npcData.Tribe == "Xaela" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Au_Ra_Xaela_Male_06";
      }

      if (npcData.Race == "Elezen")
      {
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Elezen_Duskwight_Female_01";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Elezen_Duskwight_Female_02";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Elezen_Duskwight_Female_03";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Elezen_Duskwight_Female_04";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Elezen_Duskwight_Female_05_06";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Female" && npcData.Eyes == "Option 6")
          return "Elezen_Duskwight_Female_05_06";

        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Elezen_Duskwight_Male_01";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Elezen_Duskwight_Male_02";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Elezen_Duskwight_Male_03";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Elezen_Duskwight_Male_04";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Elezen_Duskwight_Male_05";
        if (npcData.Tribe == "Duskwight" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Elezen_Duskwight_Male_06";

        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Elezen_Wildwood_Female_01";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Elezen_Wildwood_Female_02";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Elezen_Wildwood_Female_03";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Elezen_Wildwood_Female_04";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Elezen_Wildwood_Female_05";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Female" && npcData.Eyes == "Option 6")
          return "Elezen_Wildwood_Female_06";

        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Elezen_Wildwood_Male_01";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Elezen_Wildwood_Male_02";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Elezen_Wildwood_Male_03";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Elezen_Wildwood_Male_04";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Elezen_Wildwood_Male_05";
        if (npcData.Tribe == "Wildwood" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Elezen_Wildwood_Male_06";
      }

      if (npcData.Race == "Hrothgar")
      {
        if (npcData.Tribe == "Helions" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Hrothgar_Helion_01_05";
        if (npcData.Tribe == "Helions" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Hrothgar_Helion_02";
        if (npcData.Tribe == "Helions" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Hrothgar_Helion_03";
        if (npcData.Tribe == "Helions" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Hrothgar_Helion_04";
        if (npcData.Tribe == "Helions" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Hrothgar_Helion_01_05";

        if (npcData.Tribe == "The Lost" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Hrothgar_The_Lost_01";
        if (npcData.Tribe == "The Lost" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Hrothgar_The_Lost_02";
        if (npcData.Tribe == "The Lost" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Hrothgar_The_Lost_03";
        if (npcData.Tribe == "The Lost" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Hrothgar_The_Lost_04_05";
        if (npcData.Tribe == "The Lost" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Hrothgar_The_Lost_04_05";
      }

      if (npcData.Race == "Hyur")
      {
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Hyur_Highlander_Female_01";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Hyur_Highlander_Female_02";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Hyur_Highlander_Female_03";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Hyur_Highlander_Female_04";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Hyur_Highlander_Female_05";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 6")
          return "Hyur_Highlander_Female_06";

        if (npcData.Tribe == "Highlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Hyur_Highlander_Male_01";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Hyur_Highlander_Male_02";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Hyur_Highlander_Male_03";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Hyur_Highlander_Male_04";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Hyur_Highlander_Male_05";
        if (npcData.Tribe == "Highlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Hyur_Highlander_Male_06";

        if (npcData.Tribe == "Midlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Hyur_Midlander_Female_01";
        if (npcData.Tribe == "Midlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Hyur_Midlander_Female_02";
        if (npcData.Tribe == "Midlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Hyur_Midlander_Female_03";
        if (npcData.Tribe == "Midlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Hyur_Midlander_Female_04";
        if (npcData.Tribe == "Midlander" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Hyur_Midlander_Female_05";

        if (npcData.Tribe == "Midlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Hyur_Midlander_Male_01";
        if (npcData.Tribe == "Midlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Hyur_Midlander_Male_02";
        if (npcData.Tribe == "Midlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Hyur_Midlander_Male_03";
        if (npcData.Tribe == "Midlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Hyur_Midlander_Male_04";
        if (npcData.Tribe == "Midlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Hyur_Midlander_Male_05";
        if (npcData.Tribe == "Midlander" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Hyur_Midlander_Male_06";
      }

      if (npcData.Race == "Lalafell")
      {
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Lalafell_Dunesfolk_Female_01";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Lalafell_Dunesfolk_Female_02";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Lalafell_Dunesfolk_Female_03";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Lalafell_Dunesfolk_Female_04";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Lalafell_Dunesfolk_Female_05";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 6")
          return "Lalafell_Dunesfolk_Female_06";

        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Lalafell_Dunesfolk_Male_01";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Lalafell_Dunesfolk_Male_02";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Lalafell_Dunesfolk_Male_03";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Lalafell_Dunesfolk_Male_04";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Lalafell_Dunesfolk_Male_05";
        if (npcData.Tribe == "Dunesfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Lalafell_Dunesfolk_Male_06";

        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Lalafell_Plainsfolk_Female_01";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Lalafell_Plainsfolk_Female_02";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Lalafell_Plainsfolk_Female_03";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Lalafell_Plainsfolk_Female_04";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Lalafell_Plainsfolk_Female_05";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Female" && npcData.Eyes == "Option 6")
          return "Lalafell_Plainsfolk_Female_06";

        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Lalafell_Plainsfolk_Male_01";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Lalafell_Plainsfolk_Male_02";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Lalafell_Plainsfolk_Male_03";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Lalafell_Plainsfolk_Male_04";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Lalafell_Plainsfolk_Male_05";
        if (npcData.Tribe == "Plainsfolk" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Lalafell_Plainsfolk_Male_06";
      }

      if (npcData.Race == "Miqo'te")
      {
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Miqote_Keeper_of_the_Moon_Female_01";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Miqote_Keeper_of_the_Moon_Female_02";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Miqote_Keeper_of_the_Moon_Female_03";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Miqote_Keeper_of_the_Moon_Female_04";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Miqote_Keeper_of_the_Moon_Female_05";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Female" && npcData.Eyes == "Option 6")
          return "Miqote_Keeper_of_the_Moon_Female_06";

        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Miqote_Keeper_of_the_Moon_Male_01";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Miqote_Keeper_of_the_Moon_Male_02_06";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Miqote_Keeper_of_the_Moon_Male_03";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Miqote_Keeper_of_the_Moon_Male_04";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Miqote_Keeper_of_the_Moon_Male_05";
        if (npcData.Tribe == "Keeper of the Moon" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Miqote_Keeper_of_the_Moon_Male_02_06";

        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Miqote_Seeker_of_the_Sun_Female_01";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Miqote_Seeker_of_the_Sun_Female_02";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Miqote_Seeker_of_the_Sun_Female_03";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Miqote_Seeker_of_the_Sun_Female_04";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Miqote_Seeker_of_the_Sun_Female_05";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Female" && npcData.Eyes == "Option 6")
          return "Miqote_Seeker_of_the_Sun_Female_06";

        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Miqote_Seeker_of_the_Sun_Male_01";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Miqote_Seeker_of_the_Sun_Male_02";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Miqote_Seeker_of_the_Sun_Male_03";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Miqote_Seeker_of_the_Sun_Male_04";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Miqote_Seeker_of_the_Sun_Male_05";
        if (npcData.Tribe == "Seeker of the Sun" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
          return "Miqote_Seeker_of_the_Sun_Male_06";

        //if (npcData.Tribe == "Fat Cat")
        //    return "Miqote_Fat";
      }

      if (npcData.Race == "Roegadyn")
      {
        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Roegadyn_Hellsguard_Female_01";
        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Roegadyn_Hellsguard_Female_02";
        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Roegadyn_Hellsguard_Female_03";
        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Roegadyn_Hellsguard_Female_04";
        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Roegadyn_Hellsguard_Female_05";

        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Roegadyn_Hellsguard_Male_01";
        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Roegadyn_Hellsguard_Male_02";
        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Roegadyn_Hellsguard_Male_03";
        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Roegadyn_Hellsguard_Male_04";
        if (npcData.Tribe == "Hellsguard" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Roegadyn_Hellsguard_Male_05";

        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Roegadyn_Sea_Wolves_Female_01";
        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Roegadyn_Sea_Wolves_Female_02";
        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Roegadyn_Sea_Wolves_Female_03";
        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Roegadyn_Sea_Wolves_Female_04";
        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Roegadyn_Sea_Wolves_Female_05";

        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Roegadyn_Sea_Wolves_Male_01";
        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Roegadyn_Sea_Wolves_Male_02";
        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Roegadyn_Sea_Wolves_Male_03";
        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Roegadyn_Sea_Wolves_Male_04";
        if (npcData.Tribe == "Sea Wolf" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
          return "Roegadyn_Sea_Wolves_Male_05";
      }

      if (npcData.Race == "Viera")
      {
        if (npcData.Tribe == "Rava" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Viera_Rava_Female_01_05";
        if (npcData.Tribe == "Rava" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Viera_Rava_Female_02";
        if (npcData.Tribe == "Rava" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Viera_Rava_Female_03";
        if (npcData.Tribe == "Rava" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Viera_Rava_Female_04";
        if (npcData.Tribe == "Rava" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Viera_Rava_Female_01_05";

        if (npcData.Tribe == "Rava" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
          return "Viera_Rava_Male_01";
        if (npcData.Tribe == "Rava" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Viera_Rava_Male_03";
        if (npcData.Tribe == "Rava" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
          return "Viera_Rava_Male_04";

        if (npcData.Tribe == "Veena" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
          return "Viera_Veena_Female_01_05";
        if (npcData.Tribe == "Veena" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
          return "Viera_Veena_Female_02";
        if (npcData.Tribe == "Veena" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
          return "Viera_Veena_Female_03";
        if (npcData.Tribe == "Veena" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
          return "Viera_Veena_Female_04";
        if (npcData.Tribe == "Veena" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
          return "Viera_Veena_Female_01_05";

        if (npcData.Tribe == "Veena" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
          return "Viera_Veena_Male_02";
        if (npcData.Tribe == "Veena" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
          return "Viera_Veena_Male_03";
      }
    }

    if (npcData.Body == "Elderly")
    {
      if (npcData.Race == "Hyur" && npcData.Gender == "Male")
        return "Elderly_Male_Hyur";

      if (npcData.Gender == "Male")
        return "Elderly_Male";

      if (npcData.Gender == "Female")
        return "Elderly_Female";
    }

    if (npcData.Body == "Child")
    {
      if (npcData.Race == "Hyur" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
        return "Child_Hyur_Female_1";
      if (npcData.Race == "Hyur" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
        return "Child_Hyur_Female_2";
      if (npcData.Race == "Hyur" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
        return "Child_Hyur_Female_3_5";
      if (npcData.Race == "Hyur" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
        return "Child_Hyur_Female_4";
      if (npcData.Race == "Hyur" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
        return "Child_Hyur_Female_3_5";

      if (npcData.Race == "Hyur" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
        return "Child_Hyur_Male_1";
      if (npcData.Race == "Hyur" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
        return "Child_Hyur_Male_2";
      if (npcData.Race == "Hyur" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
        return "Child_Hyur_Male_3_6";
      if (npcData.Race == "Hyur" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
        return "Child_Hyur_Male_4";
      if (npcData.Race == "Hyur" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
        return "Child_Hyur_Male_5";
      if (npcData.Race == "Hyur" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
        return "Child_Hyur_Male_3_6";

      if (npcData.Race == "Elezen" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
        return "Child_Elezen_Female_1_3";
      if (npcData.Race == "Elezen" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
        return "Child_Elezen_Female_2";
      if (npcData.Race == "Elezen" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
        return "Child_Elezen_Female_1_3";
      if (npcData.Race == "Elezen" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
        return "Child_Elezen_Female_4";
      if (npcData.Race == "Elezen" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
        return "Child_Elezen_Female_5_6";
      if (npcData.Race == "Elezen" && npcData.Gender == "Female" && npcData.Eyes == "Option 6")
        return "Child_Elezen_Female_5_6";

      if (npcData.Race == "Elezen" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
        return "Child_Elezen_Male_1";
      if (npcData.Race == "Elezen" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
        return "Child_Elezen_Male_2";
      if (npcData.Race == "Elezen" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
        return "Child_Elezen_Male_3";
      if (npcData.Race == "Elezen" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
        return "Child_Elezen_Male_4";
      if (npcData.Race == "Elezen" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
        return "Child_Elezen_Male_5_6";
      if (npcData.Race == "Elezen" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
        return "Child_Elezen_Male_5_6";

      if (npcData.Race == "Au Ra" && npcData.Gender == "Female" && npcData.Eyes == "Option 1")
        return "Child_Aura_Female_1_5";
      if (npcData.Race == "Au Ra" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
        return "Child_Aura_Female_2";
      if (npcData.Race == "Au Ra" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
        return "Child_Aura_Female_4";
      if (npcData.Race == "Au Ra" && npcData.Gender == "Female" && npcData.Eyes == "Option 5")
        return "Child_Aura_Female_1_5";

      if (npcData.Race == "Au Ra" && npcData.Gender == "Male" && npcData.Eyes == "Option 1")
        return "Child_Aura_Male_1";
      if (npcData.Race == "Au Ra" && npcData.Gender == "Male" && npcData.Eyes == "Option 2")
        return "Child_Aura_Male_2";
      if (npcData.Race == "Au Ra" && npcData.Gender == "Male" && npcData.Eyes == "Option 3")
        return "Child_Aura_Male_3";
      if (npcData.Race == "Au Ra" && npcData.Gender == "Male" && npcData.Eyes == "Option 4")
        return "Child_Aura_Male_4";
      if (npcData.Race == "Au Ra" && npcData.Gender == "Male" && npcData.Eyes == "Option 5")
        return "Child_Aura_Male_5_6";
      if (npcData.Race == "Au Ra" && npcData.Gender == "Male" && npcData.Eyes == "Option 6")
        return "Child_Aura_Male_5_6";

      if (npcData.Race == "Miqo'te" && npcData.Gender == "Female" && npcData.Eyes == "Option 2")
        return "Child_Miqote_Female_2";
      if (npcData.Race == "Miqo'te" && npcData.Gender == "Female" && npcData.Eyes == "Option 3")
        return "Child_Miqote_Female_3_4";
      if (npcData.Race == "Miqo'te" && npcData.Gender == "Female" && npcData.Eyes == "Option 4")
        return "Child_Miqote_Female_3_4";
    }

    // ARR Beast Tribes
    if (npcData.Race == "Amalj'aa")
      return "Amaljaa";

    if (npcData.Race == "Sylph")
      return "Sylph";

    if (npcData.Race == "Kobold")
      return "Kobold";

    if (npcData.Race == "Sahagin")
      return "Sahagin";

    if (npcData.Race == "Ixal")
      return "Ixal";

    if (npcData.Race == "Qiqirn")
      return "Qiqirn";

    // HW Beast Tribes
    if (npcData.Race.StartsWith("Dragon"))
      return npcData.Race;

    if (npcData.Race == "Goblin")
    {
      if (npcData.Gender == "Female")
        return "Goblin_Female";
      else
        return "Goblin_Male";
    }

    if (npcData.Race == "Vanu Vanu")
    {
      if (npcData.Gender == "Female")
        return "Vanu_Female";
      else
        return "Vanu_Male";
    }

    if (npcData.Race == "Vath")
      return "Vath";

    if (npcData.Race == "Moogle")
      return "Moogle";

    if (npcData.Race == "Node")
      return "Node";

    // SB Beast Tribes
    if (npcData.Race == "Kojin")
      return "Kojin";

    if (npcData.Race == "Ananta")
      return "Ananta";

    if (npcData.Race == "Namazu")
      return "Namazu";

    if (npcData.Race == "Lupin")
    {
      if (speaker == "Hakuro" || speaker == "Hakuro Gunji" || speaker == "Hakuro Whitefang")
        return "Ranjit";

      int hashValue = speaker.GetHashCode();
      int result = Math.Abs(hashValue) % 10 + 1;

      switch (result)
      {
        case 1: return "Hrothgar_Helion_03";
        case 2: return "Hrothgar_Helion_04";
        case 3: return "Hrothgar_The_Lost_02";
        case 4: return "Hrothgar_The_Lost_03";
        case 5: return "Lalafell_Dunesfolk_Male_06";
        case 6: return "Roegadyn_Hellsguard_Male_04";
        case 7: return "Others_Widargelt";
        case 8: return "Hyur_Highlander_Male_04";
        case 9: return "Hrothgar_Helion_02";
        case 10: return "Hyur_Highlander_Male_05";
      }
      return "Lupin";
    }

    // Shb Beast Tribes
    if (npcData.Race == "Pixie")
      return "Pixie";

    // EW Beast Tribes
    if (npcData.Race == "Matanga")
    {
      if (npcData.Gender == "Female")
        return "Matanga_Female";
      else
        return "Matanga_Male";
    }

    if (npcData.Race == "Loporrit")
      return "Loporrit";

    if (npcData.Race == "Omicron")
      return "Omicron";

    if (npcData.Race == "Ea")
      return "Ea";

    // Bosses
    if (npcData.Race.StartsWith("Boss"))
      return npcData.Race;

    Logger.Debug("Cannot find a voice for " + speaker);
    return "Unknown";
  }
}
