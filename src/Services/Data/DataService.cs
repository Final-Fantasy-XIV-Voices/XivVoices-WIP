// TODO: handles the local data, updating, validating, loading manifest, etc.
namespace XivVoices.Services;

public class DataService : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;

  public Manifest? Manifest;

  public DataService(Logger logger, Configuration configuration)
  {
    Logger = logger;
    Configuration = configuration;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    LoadManifest();

    Logger.Debug("DataService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.Debug("DataService stopped");
    return Task.CompletedTask;
  }

  private void LoadManifest()
  {
    if (Manifest != null) return;
    try
    {
      string jsonContent = File.ReadAllText(Path.Join(Configuration.DataDirectory, "manifest.json"));
      var json = JsonSerializer.Deserialize<ManifestJson>(jsonContent);
      if (json == null) throw new Exception("Failed to deserialize manifest.json");

      Manifest manifest = new Manifest
      {
        IgnoredSpeakers = json.IgnoredSpeakers,
        Voices = new Dictionary<string, string>(),
        Nameless = json.Nameless,
        NpcData = json.NpcData,
        Retainers = json.Retainers,
        Lexicon = json.Lexicon,
        NpcsWithRetainerLines = json.NpcsWithRetainerLines
      };

      foreach (var mapping in json.Voices)
      {
        foreach (var speaker in mapping.Speakers)
        {
          manifest.Voices[speaker] = mapping.Name;
        }
      }

      Manifest = manifest;
    }
    catch (Exception ex)
    {
      Logger.Error(ex);
    }
  }
}
