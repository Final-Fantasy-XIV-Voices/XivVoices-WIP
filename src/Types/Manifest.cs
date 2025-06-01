namespace XivVoices.Types;

public class VoiceEntry
{
  public string Name { get; set; }
  public List<string> Speakers { get; set; }
}

public class ManifestJson
{
  public List<string> IgnoredSpeakers { get; set; }
  public List<VoiceEntry> Voices { get; set; }
  public Dictionary<string, string> Nameless { get; set; }
  public Dictionary<string, NpcData> NpcData { get; set; }
}

public class Manifest
{
  public List<string> IgnoredSpeakers { get; set; }
  public Dictionary<string, string> Voices { get; set; }
  public Dictionary<string, string> Nameless { get; set; }
  public Dictionary<string, NpcData> NpcData { get; set; }
}
