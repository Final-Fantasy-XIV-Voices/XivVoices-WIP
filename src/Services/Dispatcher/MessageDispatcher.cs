namespace XivVoices.Services;

// TODO: a lot more debug logging everywhere, to hopefully figure out what went wrong just based on logs.

public interface IMessageDispatcher : IHostedService
{
  Task TryDispatch(MessageSource source, string origSpeaker, string origSentence, uint? speakerBaseId = null);
}

public partial class MessageDispatcher(ILogger _logger, Configuration _configuration, IFramework _framework, IPlaybackService _playbackService, IReportService _reportService, ISoundFilter _soundFilter, IClientState _clientState, IGameInteropService _gameInteropService, IDataService _dataService) : IMessageDispatcher
{
  private bool BlockAddonTalkAndBattleTalk = false;

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _soundFilter.OnCutsceneAudioDetected += SoundFilter_OnCutSceneAudioDetected;

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _soundFilter.OnCutsceneAudioDetected -= SoundFilter_OnCutSceneAudioDetected;

    _logger.ServiceLifecycle();
    return Task.CompletedTask;
  }

  private void SoundFilter_OnCutSceneAudioDetected(object? sender, InterceptedSound sound)
  {
    if (_dataService.Manifest == null) return;
    if (!_clientState.IsLoggedIn || !(_gameInteropService.IsInCutscene() || _gameInteropService.IsInDuty())) return;
    _logger.Debug($"SoundFilter: {sound.BlockAddonTalkAndBattleTalk} {sound.SoundPath}");
    BlockAddonTalkAndBattleTalk = sound.BlockAddonTalkAndBattleTalk;
  }

  public async Task TryDispatch(MessageSource source, string origSpeaker, string origSentence, uint? speakerBaseId = null)
  {
    if (_dataService.Manifest == null) return;
    string speaker = origSpeaker;
    string sentence = origSentence;

    if ((source == MessageSource.AddonTalk && _gameInteropService.IsInCutscene()) || source == MessageSource.AddonBattleTalk)
    {
      // SoundFilter is a lil slower than AddonTalk update so we wait a bit.
      // This is NOT that great but it works. 100 is an arbitrary number that seems to work for now.
      await Task.Delay(100);
      if (BlockAddonTalkAndBattleTalk)
      {
        _logger.Debug($"{source} message blocked by SoundFilter");
        BlockAddonTalkAndBattleTalk = false;
        return;
      }
    }

    // If this sentence matches a sentence in Manifest.Retainers
    // and the speaker is not in Manifest.NpcsWithRetainerLines,
    // then replace the speaker with the retainer one.
    // This needs to be checked before CleanMessage.
    var retainerSpeaker = GetRetainerSpeaker(speaker, sentence);
    var isRetainer = false;
    if (retainerSpeaker != speaker)
    {
      isRetainer = true;
      speaker = retainerSpeaker;
    }

    // If speaker is ignored, well... ignore it.
    if (_dataService.Manifest.IgnoredSpeakers.Contains(speaker)) return;

    // Clean speaker and sentence only if this is a NPC message.
    if (source != MessageSource.ChatMessage)
    {
      (speaker, sentence) = await CleanMessage(speaker, sentence);

      // Skip if there's nothing meaningful to voice
      // E.g. if the sentence was "..." or "<sigh>"
      if (string.IsNullOrEmpty(sentence)) return;
    }

    // This one is a bit weird, we try to look up the NpcData directly from the game, that makes sense.
    // But even if we find it, for non-beastmen we prefer the cache? Ok.

    // This can be null and a valid voice can still be found from Manifest.Nameless or Manifest.Voices
    NpcData? npcData = await _gameInteropService.TryGetNpcData(speaker, speakerBaseId);

    // If NpcData was not found, try getting it from the cache.
    if (npcData == null)
      _dataService.Manifest.NpcData.TryGetValue(speaker, out npcData);

    // Cache player npcData to assign a gender to chatmessage tts when they're not near you.
    if (source == MessageSource.ChatMessage && npcData != null)
      _dataService.CachePlayerNpcData(origSpeaker, npcData);

    // Try to retrieve said cached npcData if they're not near you.
    if (source == MessageSource.ChatMessage && npcData == null)
      npcData = _dataService.TryGetCachedPlayerNpcData(origSpeaker);

    string? voicelinePath = null;
    string? voice = "";
    if (source != MessageSource.ChatMessage)
      (voicelinePath, voice) = await TryGetVoicelinePath(speaker, sentence, npcData);

    XivMessage message = new(
      Md5(speaker, sentence),
      source,
      voice ?? "",
      speaker,
      sentence,
      origSpeaker,
      origSentence,
      npcData,
      voicelinePath
    );

    _logger.Debug($"Constructed message: {message}");

    if (source != MessageSource.ChatMessage && message.VoicelinePath == null)
      _reportService.Report(message);

    bool allowed = true;
    bool isLocalTTS = message.VoicelinePath == null;
    bool isSystemMessage = speaker.StartsWith("Addon");
    switch (source)
    {
      case MessageSource.AddonTalk:
        allowed = _configuration.AddonTalkEnabled
          && (isSystemMessage
            ? _configuration.AddonTalkSystemEnabled
            : !isLocalTTS || _configuration.AddonTalkTTSEnabled);
        break;
      case MessageSource.AddonBattleTalk:
        allowed = _configuration.AddonBattleTalkEnabled
          && (isSystemMessage
            ? _configuration.AddonBattleTalkSystemEnabled
            : !isLocalTTS || _configuration.AddonBattleTalkTTSEnabled);
        break;
      case MessageSource.AddonMiniTalk:
        allowed = _configuration.AddonMiniTalkEnabled && (!isLocalTTS || _configuration.AddonMiniTalkTTSEnabled);
        break;
    }

    if (_configuration.Muted || !allowed || (isRetainer && !_configuration.RetainersEnabled) || (isLocalTTS && !_configuration.LocalTTSEnabled))
    {
      _logger.Debug("Not playing line due to user configuration");
      return;
    }

    await _playbackService.Play(message);
  }
}
