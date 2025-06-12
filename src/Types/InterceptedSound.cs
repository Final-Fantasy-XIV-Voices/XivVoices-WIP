namespace XivVoices.Types;

public class InterceptedSound : EventArgs
{
  public required string SoundPath { get; set; }
  public required bool BlockAddonTalkAndBattleTalk { get; set; }
}
