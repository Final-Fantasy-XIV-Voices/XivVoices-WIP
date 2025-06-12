using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace XivVoices.Services;

public interface IAddonBattleTalkProvider : IHostedService;

// PostRefresh is too early here? Guess I'm polling this one too.
public class AddonBattleTalkProvider(ILogger _logger, IAddonLifecycle _addonLifecycle, IMessageDispatcher _messageDispatcher) : IAddonBattleTalkProvider
{
  private string LastSpeaker = "";
  private string LastSentence = "";

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _addonLifecycle.RegisterListener(AddonEvent.PostDraw, "_BattleTalk", OnBattleTalkAddonPostDraw);

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _addonLifecycle.UnregisterListener(OnBattleTalkAddonPostDraw);

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  private unsafe void OnBattleTalkAddonPostDraw(AddonEvent type, AddonArgs args)
  {
    var addon = (AddonBattleTalk*)args.Addon;
    if (addon == null) return;

    string speaker = ReadTextNode(addon->Speaker);
    if (string.IsNullOrEmpty(speaker)) speaker = "AddonTalk";
    string sentence = ReadTextNode(addon->Sentence);

    if (LastSpeaker != speaker || LastSentence != sentence)
    {
      LastSpeaker = speaker;
      LastSentence = sentence;
      _logger.Debug($"speaker::{speaker} sentence::{sentence}");
      _ = _messageDispatcher.TryDispatch(MessageSource.AddonBattleTalk, speaker, sentence);
    }
  }

  private static unsafe string ReadTextNode(AtkTextNode* textNode)
  {
    if (textNode == null) return "";
    var seString = textNode->NodeText.StringPtr.AsDalamudSeString();
    return seString.TextValue
      .Trim()
      .Replace("\n", "")
      .Replace("\r", "");
  }
}

[StructLayout(LayoutKind.Explicit, Size = 0x298)]
public unsafe struct AddonBattleTalk
{
  [FieldOffset(0x0)] public AtkUnitBase AtkUnitBase;
  [FieldOffset(0x238)] public AtkTextNode* Speaker;
  [FieldOffset(0x240)] public AtkTextNode* Sentence;
}
