namespace XivVoices.Services;

public partial class LipSync
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly InteropService InteropService;
  private readonly IFramework Framework;

  // ActionTimeline exd sheet
  private const ushort SpeakNone = 0;
  private const ushort SpeakNormalLong = 631;
  private const ushort SpeakNormalMiddle = 630;
  private const ushort SpeakNormalShort = 629;

  private Dictionary<string, CancellationTokenSource> RunningTasks = new();

  public LipSync(Logger logger, Configuration configuration, InteropService interopService, IFramework framework)
  {
    Logger = logger;
    Configuration = configuration;
    InteropService = interopService;
    Framework = framework;
  }

  public async void TryLipSync(XivMessage message, double durationSeconds)
  {
    if (durationSeconds < 0.2f) return;

    ICharacter? character = await InteropService.TryFindCharacterByName(message.Speaker);
    if (!IsCharacterValid(character))
    {
      Logger.Debug($"No lipsync target found for speaker {message.Speaker}");
      return;
    }

    Dictionary<int, int> mouthMovement = new();
    int durationMs = (int)(durationSeconds * 1000);
    int durationRounded = (int)Math.Floor(durationSeconds);
    int remaining = durationRounded;
    mouthMovement[6] = remaining / 4;
    remaining = remaining % 4;
    mouthMovement[5] = remaining / 2;
    remaining = remaining % 2;
    mouthMovement[4] = remaining / 1;
    remaining = remaining % 1;

    Logger.Debug($"durationMs[{durationMs}] durationRounded[{durationRounded}] fours[{mouthMovement[6]}] twos[{mouthMovement[5]}] ones[{mouthMovement[4]}]");

    // Decide on the mode
    CharacterMode initialCharacterMode = TryGetCharacterMode(character);
    CharacterMode characterMode = CharacterMode.EmoteLoop;

    if (!RunningTasks.ContainsKey(message.Id))
    {
      CancellationTokenSource cts = new();
      RunningTasks.Add(message.Id, cts);
      var token = cts.Token;

      Task task = Task.Run(async () => {
        try
        {
          await Task.Delay(100, token);

          // 4-Second Lips Movement Animation
          if (!token.IsCancellationRequested && mouthMovement[6] > 0 && IsCharacterValid(character))
          {
            await Framework.RunOnFrameworkThread(() => {
              TrySetCharacterMode(character, characterMode);
              TrySetLipsOverride(character, SpeakNormalLong);
            });

            int adjustedDelay = CalculateAdjustedDelay(mouthMovement[6] * 4000, 6);
            Logger.Debug($"Task was started mouthMovement[6] durationMs[{mouthMovement[6] * 4}] delay [{adjustedDelay}]");

            await Task.Delay(adjustedDelay, token);

            if (!token.IsCancellationRequested && IsCharacterValid(character))
            {
              Logger.Debug("Task mouthMovement[6] has finished");
              await Framework.RunOnFrameworkThread(() => {
                TrySetCharacterMode(character, initialCharacterMode);
                TrySetLipsOverride(character, SpeakNone);
              });
            }
          }

          // 2-Second Lips Movement Animation
          if (!token.IsCancellationRequested && mouthMovement[5] > 0 && IsCharacterValid(character))
          {
            await Framework.RunOnFrameworkThread(() => {
              TrySetCharacterMode(character, characterMode);
              TrySetLipsOverride(character, SpeakNormalMiddle);
            });

            int adjustedDelay = CalculateAdjustedDelay(mouthMovement[5] * 2000, 5);
            Logger.Debug($"Task was started mouthMovement[5] durationMs[{mouthMovement[5] * 2}] delay [{adjustedDelay}]");

            await Task.Delay(adjustedDelay, token);

            if (!token.IsCancellationRequested && IsCharacterValid(character))
            {
              Logger.Debug("Task mouthMovement[5] has finished");
              await Framework.RunOnFrameworkThread(() => {
                TrySetCharacterMode(character, initialCharacterMode);
                TrySetLipsOverride(character, SpeakNone);
              });
            }
          }

          // 1-Second Lips Movement Animation
          if (!token.IsCancellationRequested && mouthMovement[4] > 0 && IsCharacterValid(character))
          {
            await Framework.RunOnFrameworkThread(() => {
              TrySetCharacterMode(character, characterMode);
              TrySetLipsOverride(character, SpeakNormalShort);
            });

            int adjustedDelay = CalculateAdjustedDelay(mouthMovement[4] * 1000, 5);
            Logger.Debug($"Task was started mouthMovement[4] durationMs[{mouthMovement[4] * 1}] delay [{adjustedDelay}]");

            await Task.Delay(adjustedDelay, token);

            if (!token.IsCancellationRequested && IsCharacterValid(character))
            {
              Logger.Debug("Task mouthMovement[4] has finished");
              await Framework.RunOnFrameworkThread(() => {
                TrySetCharacterMode(character, initialCharacterMode);
                TrySetLipsOverride(character, SpeakNone);
              });
            }
          }

          Logger.Debug("LipSync was completed");
        }
        catch (TaskCanceledException)
        {
          Logger.Debug("LipSync was canceled");
        }
        catch (Exception ex)
        {
          Logger.Error(ex);
        }
        finally
        {
          TrySetCharacterMode(character, initialCharacterMode);
          TrySetLipsOverride(character, SpeakNone);

          cts.Dispose();
          if (RunningTasks.ContainsKey(message.Id))
            RunningTasks.Remove(message.Id);
        }
      }, token);
    }
  }

  public void TryStopLipSync(string id)
  {
    if (RunningTasks.TryGetValue(id, out var cts))
    {
      try
      {
        Logger.Debug("StopLipSync cancelling cts");
        cts.Cancel();
      }
      catch (Exception ex)
      {
        Logger.Error(ex);
      }
    }
  }

  int CalculateAdjustedDelay(int durationMs, int lipSyncType)
  {
    int delay = 0;
    int animationLoop;
    if (lipSyncType == 4)
      animationLoop = 1000;
    else if (lipSyncType == 5)
      animationLoop = 2000;
    else
      animationLoop = 4000;
    int halfStep = animationLoop/2;

    if (durationMs <= (1* animationLoop) + halfStep)
    {
      return (1 * animationLoop) - 50;
    }
    else
    {
      for(int i = 2; delay < durationMs; i++)
      {
        if (durationMs > (i * animationLoop) - halfStep && durationMs  <= (i * animationLoop) + halfStep)
        {
          delay = (i * animationLoop) - 50;
          return delay;
        }
      }
    }

    return 404;
  }
}
