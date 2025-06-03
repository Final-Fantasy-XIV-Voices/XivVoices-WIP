namespace XivVoices.Types;

public class XivMessage
{
  // Sha256(speaker, sentence)
  public string Id { get; }
  public MessageSource Source { get; }
  public string Voice { get; }
  public string Speaker { get; }
  public string Sentence { get; }
  public NpcData? NpcData { get; } = null;
  public string? VoicelinePath { get; } = null;

  public XivMessage(string id, MessageSource source, string voice, string speaker, string sentence, NpcData? npcData, string? voicelinePath)
  {
    Id = id;
    Voice = voice;
    Source = source;
    Speaker = speaker;
    Sentence = sentence;
    NpcData = npcData;
    VoicelinePath = voicelinePath;
  }
}

public enum MessageSource
{
  AddonTalk,
  AddonBattleTalk,
  ChatMessage,
}
