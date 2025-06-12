namespace XivVoices.Types;

public class XivReport
{
  // General Metadata
  public string Date { get; }

  // Message Information
  public XivMessage Message { get; }

  // Player Information
  // These are not added for manual reports, as the player
  // could be in another zone by that point.
  public string? Location { get; }
  public string? Coordinates { get; }
  public bool? IsInCutscene { get; }
  public bool? IsInDuty { get; }
  public List<string>? ActiveQuests { get; }

  // Manual report reason
  public string? Reason { get; }

  public XivReport(XivMessage message, string location, string coordinates, bool isInCutscene, bool isInDuty, List<string> activeQuests)
  {
    Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"); ;

    Message = message;
    Location = location;
    Coordinates = coordinates;
    IsInCutscene = isInCutscene;
    IsInDuty = isInDuty;
    ActiveQuests = activeQuests;
  }

  public XivReport(XivMessage message, string reason)
  {
    Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"); ;

    Message = message;
    Reason = reason;
  }
}
