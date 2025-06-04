using System.Runtime.CompilerServices;

namespace XivVoices.Services;

public partial class LipSync {
  public enum CharacterMode : byte {
    None = 0,
    EmoteLoop = 3,
  }

  [StructLayout(LayoutKind.Explicit)]
  public unsafe struct ActorMemory
  {
    [FieldOffset(0x09B0)] public AnimationMemory Animation;
    [FieldOffset(0x22CC)] public byte CharacterMode;
  }

  [StructLayout(LayoutKind.Explicit)]
  public unsafe struct AnimationMemory
  {
    [FieldOffset(0x2D8)] public ushort LipsOverride;
  }

  private unsafe void TrySetLipsOverride(ICharacter? character, ushort newLipsOverride)
  {
    if (!IsCharacterValid(character)) return;
    ActorMemory* actorMemory = (ActorMemory*)character!.Address;
    if (actorMemory == null) return;
    AnimationMemory* animationMemory = (AnimationMemory*)Unsafe.AsPointer(ref actorMemory->Animation);
    if (animationMemory == null) return;
    animationMemory->LipsOverride = newLipsOverride;
  }

  private unsafe CharacterMode TryGetCharacterMode(ICharacter? character)
  {
    if (!IsCharacterValid(character)) return CharacterMode.None;
    ActorMemory* actorMemory = (ActorMemory*)character!.Address;
    if (actorMemory == null) return CharacterMode.None;
    return (CharacterMode)actorMemory->CharacterMode;
  }

  private unsafe void TrySetCharacterMode(ICharacter? character, CharacterMode characterMode)
  {
    if (!IsCharacterValid(character)) return;
    ActorMemory* actorMemory = (ActorMemory*)character!.Address;
    if (actorMemory == null) return;
    actorMemory->CharacterMode = (byte)characterMode;
  }

  private bool IsCharacterValid(ICharacter? character) =>
    character != null && character.Address != IntPtr.Zero;
}
