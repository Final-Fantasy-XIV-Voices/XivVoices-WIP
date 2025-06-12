namespace XivVoices.Windows;

// TODO: force good font. the one that is no longer dalamud default for some reason. same on SetupWindow
// ^ MonoSansJpMedium

public enum ConfigWindowTab
{
  Overview,
  Debug,
  SelfTest,
  AudioLogs,
  Wine,
  Changelog
}

public partial class ConfigWindow(ILogger _logger, Configuration _configuration, IPlaybackService _playbackService, IAudioPostProcessor _audioPostProcessor, IDataService _dataService, ITextureProvider _textureProvider, IDalamudPluginInterface _pluginInterface) : Window("XivVoices###XivVoicesConfigWindow")
{
  public ConfigWindowTab SelectedTab { get; set; } = ConfigWindowTab.Overview;

  private int _debugModeClickCount = 0;
  private double _debugModeLastClickTime;
  private readonly double _debugModeMaxClickInteral = 0.5;

  public override void Draw()
  {
    Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;
    SizeCondition = ImGuiCond.Always;
    SizeConstraints = new WindowSizeConstraints
    {
      MinimumSize = new Vector2(440, 650),
      MaximumSize = new Vector2(600, 650),
    };
    RespectCloseHotkey = _dataService.DataDirectoryExists;

    Vector2 originPos = ImGui.GetCursorPos();
    ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMax().X + (8f * ImGuiHelpers.GlobalScale));
    ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetFrameHeight() - (26f * ImGuiHelpers.GlobalScale));
    DrawImageButton(ConfigWindowTab.Changelog, "Changelog", GetImGuiHandleForIconId(47));
    ImGui.SetCursorPos(originPos);

    using (ImRaii.IEndObject child = ImRaii.Child("Sidebar", new Vector2(50 * ImGuiHelpers.GlobalScale, 500 * ImGuiHelpers.GlobalScale), false))
    {
      if (!child.Success) return;

      DrawImageButton(ConfigWindowTab.Overview, "Overview", GetImGuiHandleForIconId(1));
      DrawImageButton(ConfigWindowTab.AudioLogs, "Audio Logs", GetImGuiHandleForIconId(45));
      if (Util.IsWine())
        DrawImageButton(ConfigWindowTab.Wine, "Wine Settings", GetImGuiHandleForIconId(24423));
      if (_configuration.DebugMode)
      {
        DrawImageButton(ConfigWindowTab.Debug, "Debug", GetImGuiHandleForIconId(46));
        DrawImageButton(ConfigWindowTab.SelfTest, "Self Test", GetImGuiHandleForIconId(25));
      }

      Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? discord = _textureProvider.GetFromFile(Path.Join(_pluginInterface.AssemblyLocation.Directory?.FullName!, "discord.png")).GetWrapOrDefault();
      if (discord == null) return;
      using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0)))
      {
        using (ImRaii.Disabled(!_dataService.DataDirectoryExists))
        {
          if (ImGui.ImageButton(discord.ImGuiHandle, new Vector2(42 * ImGuiHelpers.GlobalScale, 42 * ImGuiHelpers.GlobalScale)))
          {
            Util.OpenLink("https://discord.gg/jX2vxDRkyq");
          }
        }
      }

      if (ImGui.IsItemHovered())
        using (ImRaii.Tooltip())
          ImGui.TextUnformatted("Join Our Discord Community");

      if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
      {
        double currentTime = ImGui.GetTime();
        if (currentTime - _debugModeLastClickTime <= _debugModeMaxClickInteral)
          _debugModeClickCount++;
        else
          _debugModeClickCount = 1;

        _debugModeLastClickTime = currentTime;

        if (_debugModeClickCount >= 3)
        {
          _debugModeClickCount = 0;
          _configuration.DebugMode = !_configuration.DebugMode;
          _configuration.Save();
          if (SelectedTab == ConfigWindowTab.Debug)
            SelectedTab = ConfigWindowTab.Overview;
          _logger.Debug("Toggled Debug Mode");
        }
      }
    }

    ImGui.SameLine();
    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
    Vector2 lineStart = ImGui.GetCursorScreenPos() - new Vector2(0, 10);
    Vector2 lineEnd = new(lineStart.X, lineStart.Y + (630 * ImGuiHelpers.GlobalScale));
    uint lineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.15f, 0.15f, 1));
    drawList.AddLine(lineStart, lineEnd, lineColor, 1f);
    ImGui.SameLine(85 * ImGuiHelpers.GlobalScale);

    using ImRaii.IEndObject group = ImRaii.Group();
    switch (SelectedTab)
    {
      case ConfigWindowTab.Overview:
        DrawOverviewTab();
        break;
      case ConfigWindowTab.Debug:
        DrawDebugTab();
        break;
      case ConfigWindowTab.SelfTest:
        DrawSelfTestTab();
        break;
      case ConfigWindowTab.AudioLogs:
        DrawAudioLogsTab();
        break;
      case ConfigWindowTab.Wine:
        DrawWineTab();
        break;
      case ConfigWindowTab.Changelog:
        DrawChangelogTab();
        break;
    }
  }

  private void DrawImageButton(ConfigWindowTab tab, string tabName, IntPtr imageHandle)
  {
    ImGuiStylePtr style = ImGui.GetStyle();
    if (SelectedTab == tab)
    {
      ImDrawListPtr drawList = ImGui.GetWindowDrawList();
      Vector2 screenPos = ImGui.GetCursorScreenPos();
      Vector2 rectMin = screenPos + new Vector2(style.FramePadding.X - 1);
      Vector2 rectMax = screenPos + new Vector2((42 * ImGuiHelpers.GlobalScale) + style.FramePadding.X + 1);
      uint borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.7f, 1.0f, 1.0f));
      drawList.AddRect(rectMin, rectMax, borderColor, 5.0f, ImDrawFlags.None, 2.0f);
    }

    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0)))
    {
      Vector4 tintColor = (SelectedTab == tab) ? new Vector4(0.6f, 0.8f, 1.0f, 1.0f) : new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

      using (ImRaii.Disabled(!_dataService.DataDirectoryExists && tab != ConfigWindowTab.Overview))
      {
        if (ImGui.ImageButton(imageHandle, new Vector2(42 * ImGuiHelpers.GlobalScale), Vector2.Zero, Vector2.One, (int)style.FramePadding.X, Vector4.Zero, tintColor)) SelectedTab = tab;
      }

      if (ImGui.IsItemHovered())
        using (ImRaii.Tooltip())
          ImGui.TextUnformatted(tabName);
    }
  }

  private IntPtr GetImGuiHandleForIconId(uint iconId)
  {
    if (_textureProvider.TryGetFromGameIcon(new GameIconLookup(iconId), out ISharedImmediateTexture? icon))
      return icon.GetWrapOrEmpty().ImGuiHandle;
    return 0;
  }
}
