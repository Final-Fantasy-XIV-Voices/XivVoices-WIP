using Dalamud.Interface.ImGuiFileDialog;

namespace XivVoices.Windows;

public partial class ConfigWindow
{
  private readonly FileDialogManager _fileDialogManager = new();
  private string? _selectedPath = null;
  private string? _errorMessage = null;

  private void DrawHorizontallyCenteredText(string text)
  {
    float textWidth = ImGui.CalcTextSize(text).X;
    float textX = (ImGui.GetContentRegionAvail().X - textWidth) * 0.5f;
    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + textX);
    ImGui.TextWrapped(text);
  }

  // TODO: replace the two setup buttons with a "Select Installation Directory" button? the "Install/Import" handlesboth anyway yes yes?
  // and then when datadirectory exists that button turns into "Check for Updates" / "Cancel Update"

  private void DrawOverviewTab()
  {
    _fileDialogManager.Draw();

    using (ImRaii.PushIndent(50 * ImGuiHelpers.GlobalScale))
    {
      var logo = _textureProvider.GetFromFile(Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, "logo.png")).GetWrapOrDefault();
      if (logo == null) return;
      ImGui.Image(logo.ImGuiHandle, new Vector2(200 * ImGuiHelpers.GlobalScale, 200 * ImGuiHelpers.GlobalScale));
    }

    DrawHorizontallyCenteredText("Welcome to XivVoices!");

    if (!_dataService.DataDirectoryExists)
    {
      DrawHorizontallyCenteredText("An existing installation was not found.");

      ImGui.Dummy(new(0, 10 * ImGuiHelpers.GlobalScale));

      using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.25f, 0.25f, 0.25f, 1.0f))) // Gray color
      {
        if (ImGui.Button("Create a new Installation", new Vector2(350 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale)))
        {
          _fileDialogManager.SaveFolderDialog("Select installation directory", "XivVoices", (ok, path) =>
          {
            if (!ok) return;
            path = path.Replace("\\", "/");
            if (Directory.EnumerateFileSystemEntries(path).Any())
            {
              _errorMessage = "The folder you selected is not empty.";
              _selectedPath = null;
              return;
            }

            _errorMessage = null;
            _selectedPath = path;
          });
        }

        if (ImGui.Button("Select an existing Installation", new Vector2(350 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale)))
        {
          _fileDialogManager.OpenFolderDialog("Select installation directory", (ok, path) =>
          {
            if (!ok) return;
            path = path.Replace("\\", "/");
            string legacyPath = Path.Join(path, "Data.json");
            if (File.Exists(legacyPath))
            {
              _errorMessage = "The installation you selected is incompatible.\nPlease create a new one.";
              _selectedPath = null;
              return;
            }

            string manifestPath = Path.Join(path, "manifest.json");
            if (!File.Exists(manifestPath))
            {
              _errorMessage = "The folder you selected is not a valid XivVoices installation.";
              _selectedPath = null;
              return;
            }

            _errorMessage = null;
            _selectedPath = path;
          });
        }
      }

      ImGui.Dummy(new(0, 10 * ImGuiHelpers.GlobalScale));

      if (_errorMessage != null)
      {
        using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.8f, 0.15f, 0.18f, 1.0f))) // Red color
        {
          DrawHorizontallyCenteredText(_errorMessage);
        }
      }

      if (_selectedPath != null)
      {
        DrawHorizontallyCenteredText($"Path: {_selectedPath}");

        using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.25f, 0.25f, 0.25f, 1.0f))) // Gray color
        {
          if (ImGui.Button("Install / Import", new Vector2(350 * ImGuiHelpers.GlobalScale, 40 * ImGuiHelpers.GlobalScale)))
          {
            _dataService.SetDataDirectory(_selectedPath);
            _selectedPath = null;
          }
        }
      }

      return;
    }

    if (_dataService.UpdateStatus == null)
    {
      if (ImGui.Button("Check for Updates"))
      {
        _dataService.Update();
      }
    }
    else
    {
      ImGui.TextWrapped(_dataService.UpdateStatus.ToString());
      if (ImGui.Button("Cancel Update"))
      {
        _dataService.CancelUpdate();
      }
    }

    // TODO: add current database status
    // TODO: add newest changelog and add "view all changelogs" button
    // TODO: add report status
  }
}
