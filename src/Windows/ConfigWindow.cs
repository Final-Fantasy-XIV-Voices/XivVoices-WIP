namespace XivVoices.Windows;

// TODO: force good font. the one that is no longer dalamud default for some reason. same on SetupWindow

// TODO: make this partial, one file for each tab

public class ConfigWindow : Window, IDisposable
{
  private readonly PlaybackService PlaybackService;

  public ConfigWindow(PlaybackService playbackService) : base("XivVoices###XivVoicesConfigWindow")
  {
    PlaybackService = playbackService;
  }

  public void Dispose() { }

  public override void Draw()
  {
    var tracks = PlaybackService.Debug_GetPlaying();
    ImGui.TextUnformatted($"Currently Playing: {tracks.Count()}");
    ImGui.TextUnformatted($"Mixer Sources: {PlaybackService.Debug_GetMixerSourceCount()}");

    ImGui.TextUnformatted("Audio History");
    foreach (var (message, isPlaying, currentTime, totalTime) in PlaybackService.GetPlaybackHistory())
    {
      ImGui.TextWrapped(message.Sentence);
      ImGui.TextWrapped($"Playing: {isPlaying} {currentTime}:{totalTime}");
      if (ImGui.Button(isPlaying ? "Stop" : "Play"))
      {
        if (isPlaying)
          PlaybackService.Stop(message.Id);
        else
          _ = PlaybackService.Play(message, true);
      }
    }
  }
}
