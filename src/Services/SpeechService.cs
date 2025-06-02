namespace XivVoices.Services;

public class SpeechService : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;

  public SpeechService(Logger logger, Configuration configuration)
  {
    Logger = logger;
    Configuration = configuration;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    // TODO: probably load localtts voices on startup

    Logger.Debug("SpeechService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.Debug("SpeechService stopped");
    return Task.CompletedTask;
  }

  public void Speak(string voiceline, IGameObject? gameObject)
  {
    // Logger.Toast($"Speak: {voiceline}");
    Logger.Chat($"Speak: {voiceline}");
  }

  public void SpeakTTS(string speaker, string sentence, NpcData? npcData, IGameObject? gameObject)
  {
    // Logger.Toast($"SpeakTTS: {speaker}:{sentence}");
    Logger.Chat($"SpeakTTS: {speaker}:{sentence}");
  }
}
