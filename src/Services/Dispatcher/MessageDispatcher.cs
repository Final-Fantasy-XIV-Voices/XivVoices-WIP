using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace XivVoices.Services;

// TODO: a lot more debug logging everywhere, to hopefully figure out what went wrong just based on logs.

// TODO: for players/chat: store npcData if we found it once for a certain name? So gender would work as long as you've seen that player once.
// current XIVV seems to do that with XIV_Voices/playerData.json
// this would be local, not manifest, really.
// this might be worth doing in localttsservice? idk if here is the right place.

public partial class MessageDispatcher : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly PlaybackService PlaybackService;
  private readonly ReportService ReportService;
  private readonly SoundFilter SoundFilter;
  private readonly IFramework Framework;
  private readonly IClientState ClientState;
  private readonly InteropService InteropService;
  private readonly DataService DataService;

  private bool BlockAddonTalk = false;
  // private bool BlockAddonBattleTalk = false; // TODO: stuff to mess with once i have everything working again. See TextToTalk for its implementation of SoundFilter.

  public MessageDispatcher(Logger logger, Configuration configuration, PlaybackService playbackService, ReportService reportService, SoundFilter soundFilter, IFramework framework, IClientState clientState, InteropService interopService, DataService dataService)
  {
    Logger = logger;
    Configuration = configuration;
    PlaybackService = playbackService;
    ReportService = reportService;
    SoundFilter = soundFilter;
    Framework = framework;
    ClientState = clientState;
    InteropService = interopService;
    DataService = dataService;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    SoundFilter.OnCutsceneAudioDetected += SoundFilter_OnCutSceneAudioDetected;

    Logger.Debug("MessageDispatcher started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    SoundFilter.OnCutsceneAudioDetected -= SoundFilter_OnCutSceneAudioDetected;

    Logger.Debug("MessageDispatcher stopped");
    return Task.CompletedTask;
  }

  private void SoundFilter_OnCutSceneAudioDetected(object? sender, InterceptedSound sound)
  {
    if (DataService.Manifest == null) return;
    if (!ClientState.IsLoggedIn || !InteropService.IsInCutscene()) return;
    Logger.Debug($"SoundFilter: {sound.BlockAddonTalk} {sound.SoundPath}");
    BlockAddonTalk = sound.BlockAddonTalk;
  }

  public async Task TryDispatch(MessageSource source, string speaker, string sentence)
  {
    if (DataService.Manifest == null) return;

    // TODO: battletalk support for soundfilter, battletalk is rare enough we dont have to check for InDuty() or whatever and just always delay, really.
    if (source == MessageSource.AddonTalk && InteropService.IsInCutscene())
    {
      // SoundFilter is a lil slower than AddonTalk update so we wait a bit.
      // This is NOT that great but it works. 100 is an arbitrary number that seems to work for now.
      await Task.Delay(100);
      if (BlockAddonTalk)
      {
        Logger.Debug("AddonTalk message blocked by SoundFilter");
        BlockAddonTalk = false;
        return;
      }
    }

    // Ignored system messages and other such types without a speaker. // TODO: i want these localtts voiced.
    if (String.IsNullOrEmpty(speaker)) return;

    // If this sentence matches a sentence in Manifest.Retainers
    // and the speaker is not in Manifest.NpcsWithRetainerLines,
    // then replace the speaker with the retainer one.
    // This needs to be checked before CleanMessage.
    speaker = GetRetainerSpeaker(speaker, sentence);

    // If speaker is ignored, well... ignore it.
    if (DataService.Manifest.IgnoredSpeakers.Contains(speaker)) return;

    // Clean speaker and sentence only if this is a NPC message.
    if (source != MessageSource.ChatMessage)
    {
      (speaker, sentence) = await CleanMessage(speaker, sentence);

      // Skip if there's nothing meaningful to voice
      // E.g. if the sentence was "..." or "<sigh>"
      if (String.IsNullOrEmpty(sentence)) return;
    }

    // This can be null and a valid voice can still be found from Manifest.Nameless or Manifest.Voices
    NpcData? npcData = null;

    // Try to look up cached NpcData, we prefer this over getting accurate data from GameObjects, for some reason.
    // TODO: might need to be skipped if speaker is in "NpcWithVariedLooks". Needs investigating.
    if (source != MessageSource.ChatMessage && DataService.Manifest.NpcData.TryGetValue(speaker, out var _npcData)) npcData = _npcData;

    if (npcData == null)
    {
      Logger.Debug("Trying to get NpcData from GameObject");
      ICharacter? character = await InteropService.TryFindCharacterByName(speaker);
      Logger.Debug(character == null ? $"No Character with name {speaker} found" : $"Character with name {speaker} found");
      npcData = await GetNpcDataFromCharacter(character);
      Logger.Debug(npcData == null ? "Failed to get NpcData from Character" : "Grabbed NpcData from Character");
    }

    string? voicelinePath = null;
    string? voice = "";
    if (source != MessageSource.ChatMessage)
      (voicelinePath, voice) = await TryGetVoicelinePath(speaker, sentence, npcData);

    XivMessage message = new(
      Sha256(speaker, sentence),
      source,
      voice ?? "",
      speaker,
      sentence,
      npcData,
      voicelinePath
    );

    if (source != MessageSource.ChatMessage && message.VoicelinePath == null)
      ReportService.Report(message);

    Logger.Debug("Constructed message:");
    Logger.Debug(message);
    Logger.Debug(message.NpcData);

    await PlaybackService.Play(message);
  }
}
