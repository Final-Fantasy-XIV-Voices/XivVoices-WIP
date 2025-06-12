namespace XivVoices.Windows;

public partial class ConfigWindow
{
  private void DrawChangelogTab()
  {
    using (var child = ImRaii.Child("ChangelogScrollingRegion", new Vector2(355 * ImGuiHelpers.GlobalScale, 600 * ImGuiHelpers.GlobalScale), false, ImGuiWindowFlags.AlwaysVerticalScrollbar))
    {
      if (!child.Success) return;
      ImGui.Columns(2, "ChangelogColumns", false);
      ImGui.SetColumnWidth(0, 350 * ImGuiHelpers.GlobalScale);

      if (ImGui.CollapsingHeader("Version 1.0.0.0", ImGuiTreeNodeFlags.None))
      {
        ImGui.Bullet();
        ImGui.TextWrapped("Initial rewrite release.");
      }

      ImGui.Columns(1);
    }
  }
}
