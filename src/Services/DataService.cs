using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace XivVoices.Services;

// TODO: a lot more debug logging everywhere, to hopefully figure out what went wrong just based on logs.

public class DataService : IHostedService
{
  private readonly Logger Logger;
  private readonly ReportService ReportService;
  private readonly SpeechService SpeechService;
  private readonly InteropService InteropService;
  private readonly DataMapper DataMapper;
  private readonly SoundFilter SoundFilter;
  private readonly IClientState ClientState;

  private Manifest Manifest;
  private bool BlockAddonTalk = false;

  private string DataDirectory = "/stuff/code/XivVoices-WIP/_data/voices"; // TODO: un-hardcode this
  private string ManifestJsonPath = "/stuff/code/XivVoices-WIP/_data/manifest.json"; // TODO: un-hardcode this

  public DataService(Logger logger, ReportService reportService, SpeechService speechService, InteropService interopService, DataMapper dataMapper, SoundFilter soundFilter, IClientState clientState)
  {
    Logger = logger;
    ReportService = reportService;
    SpeechService = speechService;
    InteropService = interopService;
    DataMapper = dataMapper;
    SoundFilter = soundFilter;
    ClientState = clientState;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    try
    {
      Manifest = LoadManifest();
    }
    catch (Exception ex)
    {
      Logger.Error($"Failed to load manifest: {ex.ToString()}");
      return Task.FromException(ex);
    }

    SoundFilter.OnCutsceneAudioDetected += SoundFilter_OnCutSceneAudioDetected;

    Logger.Debug("DataService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    SoundFilter.OnCutsceneAudioDetected -= SoundFilter_OnCutSceneAudioDetected;

    Logger.Debug("DataService stopped");
    return Task.CompletedTask;
  }

  private void SoundFilter_OnCutSceneAudioDetected(object sender, InterceptedSound sound)
  {
    if (!ClientState.IsLoggedIn || !InteropService.IsInCutscene()) return;
    Logger.Debug($"SoundFilter: {sound.BlockAddonTalk} {sound.SoundPath}");
    BlockAddonTalk = sound.BlockAddonTalk;
  }

  // This is stored locally, together with the voices.
  // TODO: This should be updated from the server on start-up, if the server is available.
  private Manifest LoadManifest()
  {
    string jsonContent = File.ReadAllText(ManifestJsonPath);
    var json = JsonSerializer.Deserialize<ManifestJson>(jsonContent);

    Manifest manifest = new Manifest
    {
      IgnoredSpeakers = json.IgnoredSpeakers,
      Voices = new Dictionary<string, string>(),
      Nameless = json.Nameless,
      NpcData = json.NpcData,
    };

    foreach (var mapping in json.Voices)
    {
      foreach (var speaker in mapping.Speakers)
      {
        manifest.Voices[speaker] = mapping.Name;
      }
    }

    return manifest;
  }

  // Entrypoint for all messages, including Chat.
  public async Task ProcessMessage(string speaker, string sentence, MessageSource source)
  {
    // TODO: BATTLETALK: see if i can also prevent xivv lines playing if a battletalk line is voiced!!
    // so that'd probably check IsInCombat or whatever instead of cutscene. and shouldnt have to worry
    // about clashing with addontalk as there is no way we have both battletalk and talk at once.

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

    // Ignored system messages and other such types without a speaker.
    if (String.IsNullOrEmpty(speaker)) return;

    // If speaker is ignored, well... ignore it.
    if (Manifest.IgnoredSpeakers.Contains(speaker)) return;

    // Clean speaker and sentence only if this is a NPC message.
    if (source != MessageSource.Chat)
    {
      (speaker, sentence) = await DataMapper.CleanMessage(speaker, sentence);
    }

    // Used for Lipsync and to get npcData if found.
    IGameObject? gameObject = await InteropService.GetGameObjectByName(speaker);

    // This can be null and a valid voice can still be found from Manifest.Nameless or Manifest.Voices
    NpcData? npcData = await InteropService.GetNpcDataFromGameObject(gameObject);

    // If no npcData was found from a GameObject, try looking up cached npcData, do not do this for chat messages.
    if (npcData == null && source != MessageSource.Chat && Manifest.NpcData.TryGetValue(speaker, out var _npcData)) npcData = _npcData;

    Logger.Debug(npcData);

    if (source == MessageSource.Chat)
    {
      // TODO: store npcData if we found it once for a certain name? So gender would work as long as you've seen that player once.
      // current XIVV seems to do that with XIV_Voices/playerData.json
      SpeechService.SpeakTTS(speaker, sentence, npcData, gameObject);
      return;
    }

    string? voiceline = await GetVoiceline(speaker, sentence, npcData);
    if (voiceline != null)
    {
      // Voiceline was found, play it.
      SpeechService.Speak(voiceline, gameObject);
    }
    else
    {
      // Line is missing, report it, and play local tts.
      SpeechService.SpeakTTS(speaker, sentence, npcData, gameObject);
      if (source != MessageSource.Chat) ReportService.Report(speaker, sentence, npcData, gameObject);
    }
  }

  // TODO: retainers

  // Try to get a voiceline filepath given a cleaned speaker and sentence and optionally NpcData.
  private Task<string?> GetVoiceline(string speaker, string sentence, NpcData? npcData)
  {
    return Task.Run(() => {
      string voice;
      if (speaker == "???" && Manifest.Nameless.TryGetValue(sentence, out var v1))
      {
        // If the speaker is "???", try getting it from Manifest.Nameless
        voice = v1;
        speaker = v1;
      }
      else if (Manifest.Voices.TryGetValue(speaker, out var v2))
      {
        // Else try to get the voice from Manifest.Voices based on the speaker
        // This is used for non-generic voies
        voice = v2;
      }
      else
      {
        // If no voice was found, get the generic voice from npcData, e.g. "Au_Ra_Raen_Female_01"
        if (npcData == null) return null; // If we have no NpcData, ggwp. We can't get a generic voice without npcData.
        voice = DataMapper.GetGenericVoice(npcData, speaker);
      }

      Logger.Debug($"voice::{voice} speaker::{speaker} sentence::{sentence}");
      string voiceline = Path.Combine(DataDirectory, Sha256(voice, speaker, sentence) + ".ogg");
      Logger.Debug($"voiceline::{voiceline}");

      return File.Exists(voiceline) ? voiceline : null;
    });
  }

  private string Sha256(params string[] inputs)
  {
    string combinedInput = string.Join(":", inputs);
    byte[] inputBytes = Encoding.UTF8.GetBytes(combinedInput);
    using (SHA256 sha256 = SHA256.Create())
    {
      byte[] hashBytes = sha256.ComputeHash(inputBytes);
      StringBuilder sb = new StringBuilder();
      foreach (byte b in hashBytes)
        sb.Append(b.ToString("x2"));
      return sb.ToString();
    }
  }
}
