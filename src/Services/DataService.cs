using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace XivVoices.Services;

public class DataService : IHostedService
{
  private readonly Logger Logger;
  private readonly ReportService ReportService;
  private readonly SpeechService SpeechService;
  private readonly InteropService InteropService;
  private readonly DataMapper DataMapper;

  private Manifest Manifest;

  private string DataDirectory = "/stuff/code/XivVoices-WIP/_data"; // TODO: un-hardcode this
  private string ManifestJsonPath = "/stuff/code/XivVoices-WIP/_data/manifest.json"; // TODO: un-hardcode this

  public DataService(Logger logger, ReportService reportService, SpeechService speechService, InteropService interopService, DataMapper dataMapper)
  {
    Logger = logger;
    ReportService = reportService;
    SpeechService = speechService;
    InteropService = interopService;
    DataMapper = dataMapper;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    try
    {
      Manifest = LoadManifest();
    }
    catch (Exception e)
    {
      Logger.Error($"Failed to load manifest: {e.Message}");
      return Task.FromException(e);
    }

    Logger.Debug("DataService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.Debug("DataService stopped");
    return Task.CompletedTask;
  }

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
  public void ProcessMessage(string speaker, string sentence)
  {
    // If speaker is ignored, well... ignore it.
    if (Manifest.IgnoredSpeakers.Contains(speaker)) return;

    // Clean speaker and sentence only if this is a NPC message.
    if (speaker != "Chat") CleanMessage(ref speaker, ref sentence);

    IGameObject? gameObject = null; // Used for Lipsync and to get guaranteed npcData.
    NpcData? npcData = null; // This can be null and a valid voice can still be found from Manifest.Nameless or Manifest.Voices
    if (InteropService.TryGetGameObjectByName(speaker, out gameObject))
    {
      npcData = InteropService.GetNpcDataFromGameObject(gameObject);
      Logger.Debug(npcData);
    }
    else
    {
      // Look up "cached" npcData from manifest based on the speaker.
      // This means we can have the same NpcData stored multiple times if
      // the NPC has multiple names.
      // Don't use this lookup for Chat messages.
      if (speaker != "Chat" && Manifest.NpcData.TryGetValue(speaker, out var _npcData))
      {
        npcData = _npcData;
      }
    }

    if (speaker == "Chat")
    {
      SpeechService.SpeakTTS(speaker, sentence, npcData, gameObject);
      return;
    }

    if (TryGetVoiceline(speaker, sentence, npcData, out var voiceline))
    {
      // Voiceline was found, play it.
      SpeechService.Speak(voiceline, gameObject);
    }
    else
    {
      // Line is missing, report it, and play local tts.
      ReportService.Report(speaker, sentence, npcData, gameObject);
      SpeechService.SpeakTTS(speaker, sentence, npcData, gameObject);
    }
  }

  private void CleanMessage(ref string speaker, ref string sentence)
  {
    // Remove '!' and '?' from speaker
    if (speaker != "???")
      speaker = speaker.Replace("!", "").Replace("?", "");

    // TODO: more.
  }

  // TODO: "NpcWithVariedLooks" ?
  // TODO: retainers ?

  // Try to get a voiceline filepath given a cleaned speaker and sentence and optionally NpcData.
  private bool TryGetVoiceline(string speaker, string sentence, NpcData? npcData, out string voiceline)
  {
    voiceline = null;

    string voice;
    if (speaker == "???" && Manifest.Nameless.TryGetValue(sentence, out var v1))
    {
      // If the speaker is "???", try getting it from Manifest.Nameless
      voice = v1;
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
      if (npcData == null) return false; // If we have no NpcData, ggwp. We can't get a generic voice without npcData.
      voice = DataMapper.GetGenericVoice(npcData, speaker);
    }

    Logger.Debug($"voice::{voice} speaker::{speaker} sentence::{sentence}");
    voiceline = Path.Combine(DataDirectory, Sha256(voice, speaker, sentence) + ".ogg");
    Logger.Debug($"voiceline::{voiceline}");
    return File.Exists(voiceline);
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
