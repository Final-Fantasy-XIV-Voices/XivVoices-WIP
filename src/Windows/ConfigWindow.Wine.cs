namespace XivVoices.Windows;

public partial class ConfigWindow
{
  private void DrawWineTab()
  {
    if (!Dalamud.Utility.Util.IsWine())
    {
      ImGui.TextUnformatted("You are not using wine.");
      return;
    }

    ImGui.Dummy(new Vector2(0, 10 * ImGuiHelpers.GlobalScale));
    ImGui.TextWrapped("FFmpeg Settings");
    ImGui.Dummy(new Vector2(0, 10 * ImGuiHelpers.GlobalScale));

    var wineUseNativeFFmpeg = _configuration.WineUseNativeFFmpeg;
    if (ImGui.Checkbox("##wineUseNativeFFmpeg", ref wineUseNativeFFmpeg))
    {
      _configuration.WineUseNativeFFmpeg = wineUseNativeFFmpeg;
      if (wineUseNativeFFmpeg) _audioPostProcessor.FFmpegStart();
      else _audioPostProcessor.FFmpegStop();
      _configuration.Save();
    }
    ImGui.SameLine();
    ImGui.Text("Use native FFmpeg");
    ImGui.Indent(16 * ImGuiHelpers.GlobalScale);
    ImGui.Bullet();
    ImGui.TextWrapped("Increases processing speed and prevents lag spikes on voices with effects (e.g. Dragons) and when using a playback speed other than 100%");
    ImGui.Unindent(16 * ImGuiHelpers.GlobalScale);

    ImGui.Dummy(new Vector2(0, 20 * ImGuiHelpers.GlobalScale));
    if (wineUseNativeFFmpeg)
    {
      using (var child = ImRaii.Child("##wineFFmpegState", new Vector2(345 * ImGuiHelpers.GlobalScale, 60 * ImGuiHelpers.GlobalScale), true, ImGuiWindowFlags.NoScrollbar))
      {
        if (!child.Success) return;
        ImGui.TextWrapped($"FFmpeg daemon state: {(_audioPostProcessor.FFmpegWineProcessRunning ? "Running" : "Stopped")}");
        if (ImGui.Button("Start"))
        {
          _audioPostProcessor.FFmpegStart();
        }
        ImGui.SameLine();
        if (ImGui.Button("Stop"))
        {
          _audioPostProcessor.FFmpegStop();
        }
        ImGui.SameLine();
        if (ImGui.Button("Refresh"))
        {
          _audioPostProcessor.RefreshFFmpegWineProcessState();
        }
        ImGui.SameLine();
        if (ImGui.Button("Copy Start Command"))
        {
          ImGui.SetClipboardText($"/usr/bin/env bash -c '/usr/bin/env nohup /usr/bin/env bash \"{_audioPostProcessor.FFmpegWineScriptPath}\" {_audioPostProcessor.FFmpegWineProcessPort} >/dev/null 2>&1' &");
        }

        if (ImGui.IsItemHovered())
          using (ImRaii.Tooltip())
            ImGui.TextUnformatted("Copies the command to start the ffmpeg-wine daemon manually. Run this in your native linux/macos console.");
      }

      if (!_audioPostProcessor.FFmpegWineProcessRunning)
      {
        if (_audioPostProcessor.FFmpegWineDirty)
        {
          ImGui.TextWrapped("Warning: ffmpeg-wine might require wine to fully restart for registry changes to take effect.");
          ImGui.Dummy(new Vector2(0, 20 * ImGuiHelpers.GlobalScale));
        }
        using (var child = ImRaii.Child("##wineFFmpegTroubleshooting", new Vector2(345 * ImGuiHelpers.GlobalScale, 285 * ImGuiHelpers.GlobalScale), true, ImGuiWindowFlags.NoScrollbar))
        {
          if (!child.Success) return;
          ImGui.TextWrapped("If the FFmpeg daemon fails to start, check the following:");
          ImGui.Indent(4 * ImGuiHelpers.GlobalScale);
          ImGui.Bullet();
          ImGui.TextWrapped("'/usr/bin/env' exists");
          ImGui.Bullet();
          ImGui.TextWrapped("bash is installed system-wide as 'bash'");
          ImGui.Bullet();
          if (_audioPostProcessor.IsMac())
            ImGui.TextWrapped("netstat is installed system-wide as 'netstat'");
          else
            ImGui.TextWrapped("ss is installed system-wide as 'ss'");
          ImGui.Bullet();
          ImGui.TextWrapped("ffmpeg is installed system-wide as 'ffmpeg'");
          ImGui.Bullet();
          ImGui.TextWrapped("pgrep is installed system-wide as 'pgrep'");
          ImGui.Bullet();
          ImGui.TextWrapped("grep is installed system-wide as 'grep'");
          ImGui.Bullet();
          ImGui.TextWrapped("ncat is installed system-wide as 'ncat'");
          ImGui.Indent(16 * ImGuiHelpers.GlobalScale);
          ImGui.Bullet();
          ImGui.TextWrapped("Not 'netcat' nor 'nc'. ncat is usually part of the 'nmap' package");
          ImGui.Unindent(16 * ImGuiHelpers.GlobalScale);
          ImGui.Bullet();
          ImGui.TextWrapped("wc is installed system-wide as 'wc'");
          ImGui.Bullet();
          ImGui.TextWrapped($"port {_audioPostProcessor.FFmpegWineProcessPort} is not in use");
          ImGui.Unindent(4 * ImGuiHelpers.GlobalScale);
        }
      }
    }
  }
}
