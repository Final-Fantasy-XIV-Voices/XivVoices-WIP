using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Win32;

namespace XivVoices.Services;

public partial class AudioPostProcessor
{
  private const int FFmpegWineProcessPort = 1469;
  private bool FFmpegWineProcessRunning = false;
  private string FFmpegWineScriptPath = "";

  public bool FFmpegWineDirty = false;

  private async Task FFmpegStart()
  {
    FFmpegWineScriptPath = Path.Join(PluginInterface.AssemblyLocation.Directory?.FullName!, "ffmpeg-wine.sh").Replace("\\", "/");
    FFmpegWineScriptPath = FFmpegWineScriptPath.Substring(2); // strip Z: or whatever drive maybe used.
    if (Dalamud.Utility.Util.IsWine())
    {
      FixWineRegistry();

      await RefreshFFmpegWineProcessState();
      if (Configuration.WineUseNativeFFmpeg)
        StartFFmpegWineProcess();
      else
        await StopFFmpegWineProcess();
    }
  }

  private async Task FFmpegStop()
  {
    await StopFFmpegWineProcess();
  }

  public async Task RefreshFFmpegWineProcessState()
  {
    FFmpegWineProcessRunning = await SendFFmpegWineCommand("");
  }

  private bool IsMac() =>
    PluginInterface.ConfigDirectory.ToString().Replace("\\", "/").Contains("Mac"); // because of 'XIV on Mac'

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
          Logger.Error($"Error in SetWineRegistry: key is null");
          return;
        }

        string? currentValue = key.GetValue(valueName) as string;
        if (currentValue == null)
        {
          Logger.Error($"Error in SetWineRegistry: PATHEXT value not found.");
          return;
        }

        string[] extensions = currentValue.Split(";", StringSplitOptions.RemoveEmptyEntries);

        if (!extensions.Contains("."))
        {
          string newValue = string.Join(";", extensions.Append("."));
          key.SetValue(valueName, newValue);
          Logger.Debug("SetWineRegistry: successfully updated registry");
          Logger.Chat("[XIVV] Warning: ffmpeg-wine might require wine to fully restart for registry changes to take effect.");
          FFmpegWineDirty = true;
        }
        else
        {
          Logger.Debug("SetWineRegistry: registry already updated");
        }
      }
    }
    catch (Exception ex)
    {
      Logger.Error($"Error in SetWineRegistry: {ex}");
    }
  }

  private string GetWineXIVVPath()
  {
    string configDirectory = PluginInterface.ConfigDirectory.ToString().Replace("\\", "/");
    bool isMac = IsMac();
    string baseDirectory = ""; // directory containing "wineprefix"
    // TODO: remove "WIP" below once internal name changes back
    if (isMac) baseDirectory = configDirectory.Replace("/pluginConfigs/XivVoicesWIP", ""); // XIVonMac
    else baseDirectory = configDirectory.Replace("/pluginConfigs/XivVoicesWIP", ""); // XIVLauncher
    string xivvDirectory = baseDirectory += "/wineprefix/drive_c/XivVoices"; // seems to always be this
    xivvDirectory = xivvDirectory.Substring(2); // strip Z: or whatever drive may be used
    return xivvDirectory;
  }

  public async Task ExecuteFFmpegCommand(string arguments, bool retry = true)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();
    if (Dalamud.Utility.Util.IsWine() && Configuration.WineUseNativeFFmpeg)
    {
      string _arguments = arguments.Replace("\\", "/").Replace(Configuration.DataDirectory, GetWineXIVVPath());
      Logger.Debug($"ExecuteFFmpegCommand: {_arguments}");
      bool success = await SendFFmpegWineCommand($"ffmpeg {_arguments}");
      if (!success)
      {
        if (retry)
        {
          StartFFmpegWineProcess();
          await Task.Delay(500);
          await ExecuteFFmpegCommand(arguments, false);
        }
        else
        {
          Logger.Chat("[XIVV] Failed to run ffmpeg natively. See '/xivv wine' for more information.");
          await ExecuteFFmpegCommandWindows(arguments);
          FFmpegWineProcessRunning = false;
        }
      }
    }
    else
    {
      await ExecuteFFmpegCommandWindows(arguments);
    }
    stopwatch.Stop();
    Logger.Debug($"ExecuteFFmpegCommand took {stopwatch.ElapsedMilliseconds} ms.");
  }

  private async Task ExecuteFFmpegCommandWindows(string arguments)
  {
    string ffmpegDirectoryPath = Path.Join("TODO: path to XivVoices/Tools. fuckies");
    Xabe.FFmpeg.FFmpeg.SetExecutablesPath(ffmpegDirectoryPath);
    Xabe.FFmpeg.IConversion conversion = Xabe.FFmpeg.FFmpeg.Conversions.New().AddParameter(arguments);
    await conversion.Start();
  }

  private async Task<bool> SendFFmpegWineCommand(string command)
  {
    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
    {
      try
      {
        using (TcpClient client = new TcpClient("127.0.0.1", FFmpegWineProcessPort))
        using (NetworkStream stream = client.GetStream())
        using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
        using (StreamReader reader = new StreamReader(stream))
        {
          await writer.WriteLineAsync($"{command}\n");
          var readTask = reader.ReadLineAsync();

          var completedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, cts.Token));
          if (completedTask == readTask)
          {
            await readTask;
            return true;
          }
          else
          {
            Logger.Error($"SendFFmpegWineCommand timed out after 5 seconds");
            return false;
          }
        }
      }
      catch (Exception ex)
      {
        if (String.IsNullOrEmpty(command)) return false; // Do not log errors for this.
        Logger.Debug($"SendFFmpegWineCommand error: {ex}");
        return false;
      }
    }
  }

  public void StartFFmpegWineProcess()
  {
    if (FFmpegWineProcessRunning) return;
    FFmpegWineProcessRunning = true;
    try
    {
      var ffmpegWineProcess = new Process();
      ffmpegWineProcess.StartInfo.FileName = "/usr/bin/env";
      ffmpegWineProcess.StartInfo.Arguments = $"bash \"{FFmpegWineScriptPath}\" {FFmpegWineProcessPort}";
      Logger.Debug($"ffmpegWineProcess.StartInfo.Arguments: {ffmpegWineProcess.StartInfo.Arguments}");
      ffmpegWineProcess.StartInfo.UseShellExecute = false;
      ffmpegWineProcess.Start();
      ffmpegWineProcess.Dispose();
      _ = Task.Run(async () =>
      {
        await Task.Delay(500);
        await RefreshFFmpegWineProcessState();
        if (!FFmpegWineProcessRunning)
        {
          Logger.Chat("Failed to run ffmpeg natively. See '/xivv wine' for more information.");
        }
      });
    }
    catch (Exception ex)
    {
      Logger.Error(ex);
      Logger.Chat("Failed to run ffmpeg natively. See '/xivv wine' for more information.");
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
