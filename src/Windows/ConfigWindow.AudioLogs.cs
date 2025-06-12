namespace XivVoices.Windows;

public partial class ConfigWindow
{
  private void DrawAudioLogsTab()
  {
    ImGui.TextUnformatted("Audio History");
    foreach (var (message, isPlaying, currentTime, totalTime) in _playbackService.GetPlaybackHistory())
    {
      ImGui.TextWrapped(message.Sentence);
      ImGui.TextWrapped($"Playing: {isPlaying} {currentTime}:{totalTime}");
      if (ImGui.Button(isPlaying ? "Stop" : "Play"))
      {
        if (isPlaying)
          _playbackService.Stop(message.Id);
        else
          _ = _playbackService.Play(message, true);
      }
    }
  }
}
