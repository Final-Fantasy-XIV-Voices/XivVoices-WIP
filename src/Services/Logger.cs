using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace XivVoices.Services;

public class Logger
{
  public Configuration Configuration { get; set; } = new Configuration();

  private readonly IPluginLog PluginLog;
  private readonly IToastGui ToastGui;
  private readonly IChatGui ChatGui;

  public Logger(IPluginLog pluginLog, IToastGui toastGui, IChatGui chatGui)
  {
    PluginLog = pluginLog;
    ToastGui = toastGui;
    ChatGui = chatGui;
  }

  public void Toast(string pre, string italic = "", string post = "")
  {
    ToastGui.ShowNormal(
      new SeStringBuilder()
        .AddText(pre)
        .AddItalics(italic)
        .AddText(post)
        .Build(),
      new ToastOptions
      {
        Position = ToastPosition.Bottom,
        Speed = ToastSpeed.Fast,
      }
    );
  }

  public void Chat(string pre, string italic = "", string post = "")
  {
    XivChatEntry chatMessage = new XivChatEntry
    {
      Type = XivChatType.Debug,
      Message = new SeStringBuilder()
        .AddUiForeground("[XivVoices] ", 35)
        .AddText(pre)
        .AddItalics(italic)
        .AddText(post)
        .Build(),
    };
    ChatGui.Print(chatMessage);
    Debug($"Printed chatMessage::'{chatMessage.Message}'");
  }

  private string FormatCallsite(string callerPath = "", string callerName = "", int lineNumber = -1) =>
    $"[{Path.GetFileName(callerPath)}:{callerName}:{lineNumber}]";

  public void Error(string text, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = -1) =>
    PluginLog.Error($"{FormatCallsite(callerPath, callerName, lineNumber)} {text}");

  public void Debug(string text, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = -1)
  {
    if (!Configuration.Debug) return;
    PluginLog.Debug($"{FormatCallsite(callerPath, callerName, lineNumber)} {text}");
  }

  public void Debug<T>(T obj, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerName = "", [CallerLineNumber] int lineNumber = -1)
  {
    if (obj == null)
    {
      Debug((string)"null", callerPath, callerName, lineNumber);
      return;
    }

    Type type = typeof(T);
    StringBuilder sb = new StringBuilder();
    sb.AppendLine($"Type: {type.Name}");

    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    foreach (var prop in properties)
    {
      object value = prop.GetValue(obj);
      sb.AppendLine($"  {prop.Name}: {value}");
    }

    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
    foreach (var field in fields)
    {
      object value = field.GetValue(obj);
      sb.AppendLine($"  {field.Name}: {value}");
    }

    if (properties.Length == 0 && fields.Length == 0)
      sb.AppendLine("  No public properties or fields found.");

    Debug(sb.ToString(), callerPath, callerName, lineNumber);
  }
}
