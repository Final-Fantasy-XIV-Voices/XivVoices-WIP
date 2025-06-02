using System;

namespace XivVoices.Types;

public class InterceptedSound : EventArgs {
  public string SoundPath { get; set; }
  public bool BlockXIVVAudio { get; set; }
}
