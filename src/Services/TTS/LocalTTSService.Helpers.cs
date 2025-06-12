using System.Windows.Forms;

namespace XivVoices.Services;

public partial class LocalTTSService
{
  // TODO: this adds "player says" to emote messages. mayb just check if message starts with name and then dont add it instead of this emoticons dictionary.
  private async Task<string> ProcessPlayerChat(XivMessage message)
  {
    string sentence = message.Sentence.Trim();
    string playerName = message.OriginalSpeaker.Split(" ")[0];
    bool iAmSpeaking = await _framework.RunOnFrameworkThread(() => _clientState.LocalPlayer?.Name.TextValue == message.OriginalSpeaker);
    RegexOptions options = RegexOptions.IgnoreCase;
    Dictionary<string, string> emoticons = new()
    {
      { @"(^|\s)o/($|\s)", "waves and says " },
      { @"(^|\s)\\o($|\s)", "waves and says " },
      { @"(^|\s)(:\)|\^\^|\^[^\s]\^)($|\s)", "smiles and says " },
      { @"(^|\s)(:D|:>)($|\s)", "looks happy and says " },
      { @"(^|\s)(:O|:0)($|\s)", "looks surprised and says " },
      { @"(^|\s)(:\(|:<|:C|>([^\s]+)<)($|\s)", "looks sad and says " },
      { @"\bxD\b", "laughs and says " },
      { @"(^|\s)(:3)($|\s)", "gives a playful smile and says " },
      { @"(^|\s)(:P)($|\s)", "sticks a tongue out and says " },
      { @"\bT[^\s]T\b", "cries and says " },
      { @"(^|\s);\)($|\s)", "winks and says " },
    };

    if (iAmSpeaking)
    {
      playerName = "You";
      List<string> keys = [.. emoticons.Keys];
      foreach (string key in keys)
        emoticons[key] = Regex.Replace(emoticons[key], "s ", " ");
    }

    // Regex: remove links
    sentence = Regex.Replace(sentence, @"https?\S*", "", options);

    // Regex: remove coordinates
    sentence = Regex.Replace(sentence, @"(\ue0bb[^\(]*?)\([^\)]*\)", "$1", options);

    // Check if the player is waving
    if (sentence.Equals("o/"))
    {
      if (iAmSpeaking)
        return playerName + " wave.";
      else
        return playerName + " is waving.";
    }

    // Check other emotions
    bool saysAdded = false;
    foreach (KeyValuePair<string, string> emoticon in emoticons)
    {
      if (Regex.IsMatch(sentence, emoticon.Key, options))
      {
        saysAdded = true;
        sentence = Regex.Replace(sentence, emoticon.Key, " ", options).Trim();
        sentence = playerName + " " + emoticon.Value + sentence;
        break;
      }
    }

    if (!saysAdded && _configuration.LocalTTSPlayerSays)
    {
      string says = iAmSpeaking ? " say " : " says ";
      sentence = playerName + says + sentence;
    }

    // Replace "min" following numbers with "minutes", ensuring proper pluralization
    sentence = Regex.Replace(sentence, @"(\b\d+)\s*min\b", m =>
    {
      return int.Parse(m.Groups[1].Value) == 1 ? $"{m.Groups[1].Value} minute" : $"{m.Groups[1].Value} minutes";
    }, options);

    // Clean "and says" at the end of the sentence
    string pattern = @"\s*and says\s*$";
    if (iAmSpeaking)
      pattern = @"\s*and say\s*$";
    sentence = Regex.Replace(sentence, pattern, "", options);

    // Regex: replacements
    sentence = Regex.Replace(sentence, @"\bggty\b", "good game, thank you", options);
    sentence = Regex.Replace(sentence, @"\btyfp\b", "thank you for the party!", options);
    sentence = Regex.Replace(sentence, @"\bty4p\b", "thank you for the party!", options);
    sentence = Regex.Replace(sentence, @"\btyvm\b", "thank you very much", options);
    sentence = Regex.Replace(sentence, @"\btyft\b", "thank you for the train", options);
    sentence = Regex.Replace(sentence, @"\bty\b", "thank you", options);
    sentence = Regex.Replace(sentence, @"\brp\b", "role play", options);
    sentence = Regex.Replace(sentence, @"\bo7\b", "salute", options);
    sentence = Regex.Replace(sentence, @"\bafk\b", "away from keyboard", options);
    sentence = Regex.Replace(sentence, @"\bbrb\b", "be right back", options);
    sentence = Regex.Replace(sentence, @"\bprog\b", "progress", options);
    sentence = Regex.Replace(sentence, @"\bcomms\b", "commendations", options);
    sentence = Regex.Replace(sentence, @"\bcomm\b", "commendation", options);
    sentence = Regex.Replace(sentence, @"\blq\b", "low quality", options);
    sentence = Regex.Replace(sentence, @"\bhq\b", "high quality", options);
    sentence = Regex.Replace(sentence, @"\bfl\b", "friend list", options);
    sentence = Regex.Replace(sentence, @"\bfc\b", "free company", options);
    sentence = Regex.Replace(sentence, @"\bdot\b", "damage over time", options);
    sentence = Regex.Replace(sentence, @"\bcrit\b", "critical hit", options);
    sentence = Regex.Replace(sentence, @"\blol\b", "\"L-O-L\"", options);
    sentence = Regex.Replace(sentence, @"\blmao\b", "\"Lah-mao\"", options);
    sentence = Regex.Replace(sentence, @"\bgg\b", "good game", options);
    sentence = Regex.Replace(sentence, @"\bglhf\b", "good luck, have fun", options);
    sentence = Regex.Replace(sentence, @"\bgl\b", "good luck", options);
    sentence = Regex.Replace(sentence, @"\bsry\b", "sorry", options);
    sentence = Regex.Replace(sentence, @"\bsrry\b", "sorry", options);
    sentence = Regex.Replace(sentence, @"\bcs\b", "cutscene", options);
    sentence = Regex.Replace(sentence, @"\bttyl\b", "talk to you later", options);
    sentence = Regex.Replace(sentence, @"\boki\b", "okay", options);
    sentence = Regex.Replace(sentence, @"\bkk\b", "okay", options);
    sentence = Regex.Replace(sentence, @"\bffs\b", "for fuck's sake", options);
    sentence = Regex.Replace(sentence, @"\baight\b", "ight", options);
    sentence = Regex.Replace(sentence, @"\bggs\b", "good game", options);
    sentence = Regex.Replace(sentence, @"\bwp\b", "well played", options);
    sentence = Regex.Replace(sentence, @"\bgn\b", "good night", options);
    sentence = Regex.Replace(sentence, @"\bnn\b", "ight night", options);
    sentence = Regex.Replace(sentence, @"\bdd\b", "damage dealer", options);
    sentence = Regex.Replace(sentence, @"\bbis\b", "best in slot", options);
    sentence = Regex.Replace(sentence, @"(?<=\s|^):\)(?=\s|$)", "smile", options);
    sentence = Regex.Replace(sentence, @"(?<=\s|^):\((?=\s|$)", "sadge", options);
    sentence = Regex.Replace(sentence, @"\b<3\b", "heart", options);
    sentence = Regex.Replace(sentence, @"\bARR\b", "A Realm Reborn", options);
    sentence = Regex.Replace(sentence, @"\bHW\b", "Heavensward");
    sentence = Regex.Replace(sentence, @"\bSB\b", "Storm Blood");
    sentence = Regex.Replace(sentence, @"\bSHB\b", "Shadowbringers", options);
    sentence = Regex.Replace(sentence, @"\bEW\b", "End Walker");
    sentence = Regex.Replace(sentence, @"\bucob\b", "ultimate coils of bahamut", options);
    sentence = Regex.Replace(sentence, @"\bIT\b", "it");
    sentence = Regex.Replace(sentence, @"r says", "rr says");
    sentence = Regex.Replace(sentence, @"Eleanorr says", "el-uh-ner says");
    sentence = Regex.Replace(sentence, @"\bm1\b", "\"Melee one\"", options);
    sentence = Regex.Replace(sentence, @"\bm2\b", "\"Melee two\"", options);
    sentence = Regex.Replace(sentence, @"\bot\b", "\"Off-Tank\"", options);
    sentence = Regex.Replace(sentence, @"\bMt\b", "\"Main-Tank\"");
    sentence = Regex.Replace(sentence, @"\bMT\b", "\"Main-Tank\"");
    sentence = Regex.Replace(sentence, @"\bmt\b", "\"mistake\"");
    sentence = Regex.Replace(sentence, @"\br1\b", "\"Ranged One\"", options);
    sentence = Regex.Replace(sentence, @"\br2\b", "\"Ranged Two\"", options);
    sentence = Regex.Replace(sentence, @"\bh1\b", "\"Healer One\"", options);
    sentence = Regex.Replace(sentence, @"\bh2\b", "\"Healer Two\"", options);
    sentence = Regex.Replace(sentence, @"\brn\b", "\"right now\"", options);

    sentence = JobReplacement(sentence);
    return sentence;
  }

  private string JobReplacement(string sentence)
  {
    Dictionary<string, string> jobReplacementsCaseSensitive = new()
    {
      { "WAR", "Warrior" },
      { "SAM", "Samurai" }
    };

    Dictionary<string, string> jobReplacementsCaseInsensitive = new()
    {
      { "CRP", "Carpenter" },
      { "BSM", "Blacksmith" },
      { "ARM", "Armorer" },
      { "GSM", "Goldsmith" },
      { "LTW", "Leatherworker" },
      { "WVR", "Weaver" },
      { "ALC", "Alchemist" },
      { "CUL", "Culinarian" },
      { "MIN", "Miner" },
      { "BTN", "Botanist" },
      { "FSH", "Fisher" },
      { "GLA", "Gladiator" },
      { "PGL", "Pugilist" },
      { "MRD", "Marauder" },
      { "LNC", "Lancer" },
      { "ROG", "Rogue" },
      { "CNJ", "Conjurer" },
      { "THM", "Thaumaturge" },
      { "ACN", "Arcanist" },
      { "PLD", "Paladin" },
      { "DRK", "Dark Knight" },
      { "GNB", "Gunbreaker" },
      { "RPR", "Reaper" },
      { "MNK", "Monk" },
      { "DRG", "Dragoon" },
      { "NIN", "Ninja" },
      { "WHM", "White Mage" },
      { "SCH", "Scholar" },
      { "AST", "Astrologian" },
      { "SGE", "Sage" },
      { "BRD", "Bard" },
      { "MCH", "Machinist" },
      { "DNC", "Dancer" },
      { "BLM", "Black Mage" },
      { "SMN", "Summoner" },
      { "RDM", "Red Mage" },
      { "BLU", "Blue Mage" },
      { "PCT", "Pictohmanser" },
      { "VPR", "Viper" }
    };

    // Apply case-insensitive replacements for most job abbreviations
    foreach (KeyValuePair<string, string> job in jobReplacementsCaseInsensitive)
      sentence = Regex.Replace(sentence, $@"\b{job.Key}\b", job.Value, RegexOptions.IgnoreCase);

    // Apply case-sensitive replacements for "WAR," "ARC," and "SAM"
    foreach (KeyValuePair<string, string> job in jobReplacementsCaseSensitive)
      sentence = Regex.Replace(sentence, $@"\b{job.Key}\b", job.Value);

    return sentence;
  }

  private string ApplyLexicon(string sentence)
  {
    if (_dataService.Manifest == null) return sentence;

    string cleanedSentence = sentence;
    foreach (KeyValuePair<string, string> entry in _dataService.Manifest.Lexicon)
    {
      string pattern = "\\b" + entry.Key + "\\b";
      cleanedSentence = Regex.Replace(cleanedSentence, pattern, entry.Value, RegexOptions.IgnoreCase);
    }
    return cleanedSentence;
  }
}
