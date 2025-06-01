namespace XivVoices.Windows;

public class SetupWindow : Window, IDisposable
{
  public SetupWindow() : base("XivVoices###XivVoicesSetupWindow")
  {

  }

  public void Dispose() { }

  public override void Draw()
  {
    ImGui.TextUnformatted("w");
  }
}
