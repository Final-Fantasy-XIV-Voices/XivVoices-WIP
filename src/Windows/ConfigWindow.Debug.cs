namespace XivVoices.Windows;

public partial class ConfigWindow
{
  private void DrawDebugTab()
  {
    var tracks = _playbackService.Debug_GetPlaying();
    ImGui.TextUnformatted($"Currently Playing: {tracks.Count()}");
    ImGui.TextUnformatted($"Mixer Sources: {_playbackService.Debug_GetMixerSourceCount()}");

    var debugLogging = _configuration.DebugLogging;
    if (ImGui.Checkbox("##debugLogging", ref debugLogging))
    {
      _configuration.DebugLogging = debugLogging;
      _configuration.Save();
    }
    ImGui.SameLine();
    ImGui.Text("Debug Logging");
  }
}
