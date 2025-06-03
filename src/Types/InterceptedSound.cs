namespace XivVoices.Types;

public class InterceptedSound : EventArgs
{
  public required string SoundPath { get; set; }
  public required bool BlockAddonTalk { get; set; }
}
