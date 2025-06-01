namespace XivVoices.Windows;

public class ConfigWindow : Window, IDisposable
{
  public ConfigWindow() : base("XivVoices###XivVoicesConfigWindow")
  {

  }

  public void Dispose() { }

  public override void Draw()
  {
    ImGui.TextUnformatted("w");
  }
}
