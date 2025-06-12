using System.Net.Http;

namespace XivVoices.Services;

public interface IDataService : IHostedService
{
  UpdateStatus? UpdateStatus { get; }
  Manifest? Manifest { get; }
  bool DataDirectoryExists { get; }
  string? DataDirectory { get; }
  string? ToolsDirectory { get; }
  string? ReportsDirectory { get; }
  Task SetDataDirectory(string dataDirectory);
  Task Update();
  void CancelUpdate();
  string? TempFilePath(string fileName);
  NpcData? TryGetCachedPlayerNpcData(string speaker);
  void CachePlayerNpcData(string speaker, NpcData npcData);
}

// TODO: add a "alive" check to the serverurl and disable online functionality if server is ded. also display that in the configwindow and ImRaii.Disable buttons if server ded

public class DataService(ILogger _logger, Configuration _configuration) : IDataService
{
  private Dictionary<string, NpcData> _cachedPlayerNpcData = [];
  private readonly HttpClient _httpClient = new();
  private CancellationTokenSource? _cts;
  private readonly SemaphoreSlim _semaphore = new(25);

  public UpdateStatus? UpdateStatus { get; private set; } = null;
  public Manifest? Manifest { get; private set; } = null;

  public bool DataDirectoryExists { get; private set; } = false;
  public string? DataDirectory
  {
    get
    {
      if (!DataDirectoryExists) return null;
      return _configuration.DataDirectory;
    }
  }

  private bool _toolsDirectoryExists;
  public string? ToolsDirectory
  {
    get
    {
      string toolsDirectory = Path.Join(DataDirectory, "tools");
      if (_toolsDirectoryExists) return toolsDirectory;
      if (Directory.Exists(toolsDirectory))
      {
        _toolsDirectoryExists = true;
        return toolsDirectory;
      }
      return null;
    }
  }

  private bool _reportsDirectoryExists;
  public string? ReportsDirectory
  {
    get
    {
      string? dataDirectory = DataDirectory;
      if (dataDirectory == null) return null;
      string reportsDirectory = Path.Join(dataDirectory, "reports");
      if (_reportsDirectoryExists) return reportsDirectory;
      if (!Directory.Exists(reportsDirectory))
      {
        Directory.CreateDirectory(reportsDirectory);
        _reportsDirectoryExists = true;
      }
      return reportsDirectory;
    }
  }

  public string? TempFilePath(string fileName)
  {
    string? dataDirectory = DataDirectory;
    if (dataDirectory == null) return null;
    return Path.Join(dataDirectory, fileName);
  }

  public async Task SetDataDirectory(string dataDirectory)
  {
    DataDirectoryExists = Directory.Exists(dataDirectory);
    if (!DataDirectoryExists) return;
    _configuration.DataDirectory = dataDirectory;
    _configuration.Save();
    await LoadManifest();
    if (Manifest != null)
      _ = Update();
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    DataDirectoryExists = Directory.Exists(_configuration.DataDirectory);

    _ = LoadManifest();
    LoadCachedPlayerNpcData();

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    CancelUpdate();
    SaveCachedPlayerNpcData();

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  private async Task LoadManifest()
  {
    string? dataDirectory = DataDirectory;
    if (dataDirectory == null)
    {
      _logger.Debug("Can't load manifest: DataDirectory doesn't exist.");
      return;
    }

    string manifestPath = Path.Join(DataDirectory, "manifest.json");
    bool manifestExists = File.Exists(manifestPath);
    bool shouldDownload = !manifestExists;
    if (manifestExists)
    {
      DateTime lastModified = File.GetLastWriteTime(manifestPath);
      if (DateTime.Now - lastModified > TimeSpan.FromHours(24))
      {
        _logger.Debug("Manifest file is older than 24 hours, redownloading.");
        shouldDownload = true;
      }
    }

    if (shouldDownload)
      await DownloadFile(manifestPath, "manifest.json");

    try
    {
      string jsonContent = File.ReadAllText(manifestPath);
      ManifestJson json = JsonSerializer.Deserialize<ManifestJson>(jsonContent) ?? throw new Exception("Failed to deserialize manifest.json");

      Manifest manifest = new()
      {
        Voicelines = json.Voicelines,
        IgnoredSpeakers = json.IgnoredSpeakers,
        Voices = [],
        Nameless = json.Nameless,
        NpcData = json.NpcData,
        Retainers = json.Retainers,
        Lexicon = json.Lexicon,
        NpcsWithVariedLooks = json.NpcsWithVariedLooks,
        NpcsWithRetainerLines = json.NpcsWithRetainerLines
      };

      foreach (VoiceEntry mapping in json.Voices)
      {
        foreach (string speaker in mapping.Speakers)
        {
          manifest.Voices[speaker] = mapping.Name;
        }
      }

      Manifest = manifest;
    }
    catch (Exception ex)
    {
      _logger.Error(ex);
      return;
    }

    if (shouldDownload)
    {
      _ = Update();
    }
  }

  public Task Update()
  {
    return Task.Run(UpdateInternal);
  }

  // TODO: download and update tools
  // TODO: check how many voices are downloaded at dataserver startup since its hella cheap now with that one getdirectory or whatever call.
  // that can then be displayed in the config window
  private async Task UpdateInternal()
  {
    if (Manifest == null)
    {
      _logger.Debug("Manifest not loaded, can't update.");
      return;
    }

    string? dataDirectory = DataDirectory;
    if (dataDirectory == null)
    {
      _logger.Debug("DataDirectory not found, can't update.");
      return;
    }

    if (_cts != null)
    {
      _cts.Cancel();
      _cts.Dispose();
    }

    _cts = new CancellationTokenSource();
    CancellationToken token = _cts.Token;

    UpdateStatus status = UpdateStatus = new()
    {
      TotalFiles = Manifest.Voicelines.Count,
      CompletedFiles = 0
    };

    List<(string filePath, string fileName)> missingFiles = [];
    string voicelineDirectory = Path.Join(dataDirectory, "voicelines");
    bool downloadAll = false;

    if (!Directory.Exists(voicelineDirectory))
    {
      Directory.CreateDirectory(voicelineDirectory);
      downloadAll = true;
    }

    Dictionary<string, long> fileSizeMap = new DirectoryInfo(voicelineDirectory).GetFiles("*", SearchOption.TopDirectoryOnly).ToDictionary(f => f.Name, f => f.Length);

    foreach (KeyValuePair<string, long> voiceline in Manifest.Voicelines)
    {
      if (token.IsCancellationRequested) break;
      string filePath = Path.Join(voicelineDirectory, voiceline.Key);
      if (downloadAll || !fileSizeMap.TryGetValue(voiceline.Key, out long size) || size != voiceline.Value)
        missingFiles.Add((filePath, voiceline.Key));
      else
        Interlocked.Increment(ref status.SkippedFiles);
    }

    _logger.Debug($"{missingFiles.Count} files need to be updated");
    if (missingFiles.Count == 0)
    {
      UpdateStatus = null;
      return;
    }

    List<Task> tasks = [];
    UpdateStatus.StartTime = DateTime.UtcNow;

    foreach ((string filePath, string fileName) in missingFiles)
    {
      try
      {
        await _semaphore.WaitAsync(token);
      }
      catch (OperationCanceledException)
      {
        _logger.Debug("Cancellation requested during semaphore wait, breaking loop.");
        break;
      }

      tasks.Add(Task.Run(async () =>
      {
        try
        {
          await DownloadFile(filePath, fileName, token);
          Interlocked.Increment(ref status.CompletedFiles);
        }
        finally
        {
          _semaphore.Release();
        }
      }, token));
    }

    try
    {
      await Task.WhenAll(tasks);
    }
    finally
    {
      UpdateStatus = null;
    }
  }

  public void CancelUpdate()
  {
    if (_cts != null)
    {
      _cts.Cancel();
      _cts.Dispose();
      _cts = null;
    }
  }

  private async Task DownloadFile(string filePath, string fileName, CancellationToken token = default)
  {
    try
    {
      string url = $"{_configuration.ServerUrl}/download?filename={fileName}";
      using HttpResponseMessage response = await _httpClient.GetAsync(url, token);
      response.EnsureSuccessStatusCode();
      byte[] fileBytes = await response.Content.ReadAsByteArrayAsync(token);
      await File.WriteAllBytesAsync(filePath, fileBytes, token);
    }
    catch (OperationCanceledException)
    {
      _logger.Debug($"DownloadFile was cancelled: {fileName}");
    }
    catch (Exception ex)
    {
      _logger.Error(ex);
    }
  }

  private void LoadCachedPlayerNpcData()
  {
    string filePath = Path.Join(DataDirectory, "playerNpcData.json");
    if (!File.Exists(filePath)) return;

    try
    {
      string jsonContent = File.ReadAllText(filePath);
      Dictionary<string, NpcData> json = JsonSerializer.Deserialize<Dictionary<string, NpcData>>(jsonContent) ?? throw new Exception("Failed to deserialize manifest.json");
      _cachedPlayerNpcData = json;
    }
    catch (Exception ex)
    {
      _logger.Error(ex);
    }
  }

  private readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };
  private void SaveCachedPlayerNpcData()
  {
    try
    {
      string filePath = Path.Join(DataDirectory, "playerNpcData.json");
      string json = JsonSerializer.Serialize(_cachedPlayerNpcData, _writeOptions);
      File.WriteAllText(filePath, json);
    }
    catch (Exception ex)
    {
      _logger.Error(ex);
    }
  }

  public NpcData? TryGetCachedPlayerNpcData(string speaker)
  {
    if (_cachedPlayerNpcData.TryGetValue(speaker, out NpcData? npcData))
      return npcData;
    return null;
  }

  public void CachePlayerNpcData(string speaker, NpcData npcData)
  {
    _cachedPlayerNpcData[speaker] = npcData;
    SaveCachedPlayerNpcData();
  }
}

public class UpdateStatus
{
  public int TotalFiles { get; set; }
  public int CompletedFiles;
  public int SkippedFiles;
  public DateTime StartTime { get; set; } = DateTime.UtcNow;

  public double ProgressPercent => TotalFiles == 0 ? 0 : (double)(SkippedFiles + CompletedFiles) / TotalFiles * 100;

  public TimeSpan ETA
  {
    get
    {
      if (CompletedFiles == 0) return TimeSpan.MaxValue;
      TimeSpan elapsed = DateTime.UtcNow - StartTime;
      double avgTimePerFile = elapsed.TotalSeconds / CompletedFiles;
      int remainingFiles = TotalFiles - SkippedFiles - CompletedFiles;
      return TimeSpan.FromSeconds(avgTimePerFile * remainingFiles);
    }
  }

  public override string ToString()
  {
    return $"{SkippedFiles + CompletedFiles}/{TotalFiles} files downloaded ({ProgressPercent:F1}%). ETA: {(ETA == TimeSpan.MaxValue ? "Calculating..." : ETA.ToString(@"hh\:mm\:ss"))}";
  }
}
