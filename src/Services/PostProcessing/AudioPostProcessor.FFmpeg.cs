using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Win32;

namespace XivVoices.Services;

public partial class AudioPostProcessor
{
  public int FFmpegWineProcessPort { get; } = 1469;
  public bool FFmpegWineProcessRunning { get; private set; } = false;
  public string FFmpegWineScriptPath { get; private set; } = "";
  public bool FFmpegWineDirty { get; private set; } = false;

  private readonly SemaphoreSlim _semaphore = new(1, 1);

  public async Task FFmpegStart()
  {
    FFmpegWineScriptPath = Path.Join(_pluginInterface.AssemblyLocation.Directory?.FullName!, "ffmpeg-wine.sh").Replace("\\", "/");
    FFmpegWineScriptPath = FFmpegWineScriptPath[2..]; // strip Z: or whatever drive maybe used.
    if (Util.IsWine())
    {
      FixWineRegistry();

      await RefreshFFmpegWineProcessState();
      if (_configuration.WineUseNativeFFmpeg)
      {
        StartFFmpegWineProcess();
        _ = Task.Run(async () =>
        {
          await Task.Delay(1000);
          await RefreshFFmpegWineProcessState();
          if (!FFmpegWineProcessRunning)
          {
            _logger.Chat("Failed to run ffmpeg natively. See '/xivv wine' for more information.");
          }
        });
      }
      else
      {
        await StopFFmpegWineProcess();
      }
    }
  }

  public async Task FFmpegStop()
  {
    await StopFFmpegWineProcess();
  }

  public async Task RefreshFFmpegWineProcessState()
  {
    FFmpegWineProcessRunning = await SendFFmpegWineCommand("");
  }

  public bool IsMac() =>
    _pluginInterface.ConfigDirectory.ToString().Replace("\\", "/").Contains("Mac"); // because of 'XIV on Mac'

  // https://gitlab.winehq.org/wine/wine/-/wikis/FAQ#how-do-i-launch-native-applications-from-a-windows-application
  private void FixWineRegistry()
  {
    string regPath = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment";
    string valueName = "PATHEXT";

    try
    {
      using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(regPath, writable: true))
      {
        if (key == null)
        {
          _logger.Error($"Error in SetWineRegistry: key is null");
          return;
        }

        string? currentValue = key.GetValue(valueName) as string;
        if (currentValue == null)
        {
          _logger.Error($"Error in SetWineRegistry: PATHEXT value not found.");
          return;
        }

        string[] extensions = currentValue.Split(";", StringSplitOptions.RemoveEmptyEntries);

        if (!extensions.Contains("."))
        {
          string newValue = string.Join(";", extensions.Append("."));
          key.SetValue(valueName, newValue);
          _logger.Debug("SetWineRegistry: successfully updated registry");
          _logger.Chat("Warning: ffmpeg-wine might require wine to fully restart for registry changes to take effect.");
          FFmpegWineDirty = true;
        }
        else
        {
          _logger.Debug("SetWineRegistry: registry already updated");
        }
      }
    }
    catch (Exception ex)
    {
      _logger.Error($"Error in SetWineRegistry: {ex}");
    }
  }

  private string GetWineXIVVPath()
  {
    string configDirectory = _pluginInterface.ConfigDirectory.ToString().Replace("\\", "/");
    bool isMac = IsMac();
    string baseDirectory = ""; // directory containing "wineprefix"
    if (isMac) baseDirectory = configDirectory.Replace($"/pluginConfigs/{_pluginInterface.InternalName}", ""); // XIVonMac
    else baseDirectory = configDirectory.Replace($"/pluginConfigs/{_pluginInterface.InternalName}", ""); // XIVLauncher
    string xivvDirectory = baseDirectory += "/wineprefix/drive_c/XivVoices"; // seems to always be this
    xivvDirectory = xivvDirectory[2..]; // strip Z: or whatever drive may be used
    return xivvDirectory;
  }

  public async Task ExecuteFFmpegCommand(string arguments, int attempts = 0)
  {
    if (attempts == 0) await _semaphore.WaitAsync();
    Stopwatch stopwatch = Stopwatch.StartNew();
    try
    {
      if (Util.IsWine() && _configuration.WineUseNativeFFmpeg)
      {
        string? dataDirectory = _dataService.DataDirectory;
        if (dataDirectory == null) return;
        string _arguments = arguments.Replace("\\", "/").Replace(dataDirectory, GetWineXIVVPath());
        _logger.Debug($"ExecuteFFmpegCommand: {_arguments}");
        bool success = await SendFFmpegWineCommand($"ffmpeg {_arguments}");
        if (!success)
        {
          if (attempts < 1)
          {
            _logger.Debug("Failed to run ffmpeg on wine, retrying.");
            FFmpegWineProcessRunning = false;
            StartFFmpegWineProcess();
            await Task.Delay(1000);
            await ExecuteFFmpegCommand(arguments, attempts + 1);
          }
          else
          {
            _logger.Chat("Failed to run ffmpeg natively. See '/xivv wine' for more information.");
            await ExecuteFFmpegCommandWindows(arguments);
            FFmpegWineProcessRunning = false;
          }
        }
      }
      else
      {
        await ExecuteFFmpegCommandWindows(arguments);
      }
    }
    finally
    {
      stopwatch.Stop();
      if (attempts == 0)
      {
        _semaphore.Release();
        _logger.Debug($"ExecuteFFmpegCommand took {stopwatch.ElapsedMilliseconds} ms.");
      }
    }
  }

  private async Task ExecuteFFmpegCommandWindows(string arguments)
  {
    Xabe.FFmpeg.FFmpeg.SetExecutablesPath(_dataService.ToolsDirectory);
    Xabe.FFmpeg.IConversion conversion = Xabe.FFmpeg.FFmpeg.Conversions.New().AddParameter(arguments);
    await conversion.Start();
  }

  private async Task<bool> SendFFmpegWineCommand(string command)
  {
    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
    try
    {
      using TcpClient client = new("127.0.0.1", FFmpegWineProcessPort);
      using NetworkStream stream = client.GetStream();
      using StreamWriter writer = new(stream) { AutoFlush = true };
      using StreamReader reader = new(stream);
      await writer.WriteLineAsync($"{command}\n");
      Task<string?> readTask = reader.ReadLineAsync();

      Task completedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, cts.Token));
      if (completedTask == readTask)
      {
        await readTask;
        return true;
      }
      else
      {
        _logger.Error($"SendFFmpegWineCommand timed out after 5 seconds");
        return false;
      }
    }
    catch
    {
      // eh, these are always just "connection failed" like whatever
      return false;
    }
  }

  public void StartFFmpegWineProcess()
  {
    if (FFmpegWineProcessRunning) return;
    FFmpegWineProcessRunning = true;
    try
    {
      Process ffmpegWineProcess = new();
      ffmpegWineProcess.StartInfo.FileName = "/usr/bin/env";
      ffmpegWineProcess.StartInfo.Arguments = $"bash \"{FFmpegWineScriptPath}\" {FFmpegWineProcessPort}";
      _logger.Debug($"ffmpegWineProcess.StartInfo.Arguments: {ffmpegWineProcess.StartInfo.Arguments}");
      ffmpegWineProcess.StartInfo.UseShellExecute = false;
      ffmpegWineProcess.Start();
      ffmpegWineProcess.Dispose();
    }
    catch (Exception ex)
    {
      _logger.Error(ex);
      _logger.Chat("Failed to run ffmpeg natively. See '/xivv wine' for more information.");
      FFmpegWineProcessRunning = false;
    }
  }

  public async Task StopFFmpegWineProcess()
  {
    if (!FFmpegWineProcessRunning) return;
    FFmpegWineProcessRunning = false;
    await SendFFmpegWineCommand("exit");
  }
}
