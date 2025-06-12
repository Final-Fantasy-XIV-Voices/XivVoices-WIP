using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = XivVoices.Services.ILogger;

namespace XivVoices;

public sealed class Plugin : IDalamudPlugin
{
  private readonly IHost _host;

  public Plugin(
    IDalamudPluginInterface pluginInterface,
    IChatGui chatGui,
    IGameGui gameGui,
    IClientState clientState,
    ICommandManager commandManager,
    IFramework framework,
    IPluginLog pluginLog,
    IToastGui toastGui,
    IAddonLifecycle addonLifecycle,
    IObjectTable objectTable,
    IGameInteropProvider interopProvider,
    ICondition condition,
    IKeyState keyState,
    IDataManager dataManager,
    ITextureProvider textureProvider
  )
  {
    _host = new HostBuilder()
      .UseContentRoot(pluginInterface.ConfigDirectory.FullName)
      .ConfigureLogging(lb =>
      {
        lb.ClearProviders();
        lb.SetMinimumLevel(LogLevel.Trace);
      })
      .ConfigureServices(collection =>
      {
        collection.AddSingleton(pluginInterface);
        collection.AddSingleton(chatGui);
        collection.AddSingleton(gameGui);
        collection.AddSingleton(clientState);
        collection.AddSingleton(commandManager);
        collection.AddSingleton(framework);
        collection.AddSingleton(pluginLog);
        collection.AddSingleton(toastGui);
        collection.AddSingleton(addonLifecycle);
        collection.AddSingleton(objectTable);
        collection.AddSingleton(interopProvider);
        collection.AddSingleton(condition);
        collection.AddSingleton(keyState);
        collection.AddSingleton(dataManager);
        collection.AddSingleton(textureProvider);

        collection.AddSingleton<IWindowService, WindowService>();
        collection.AddSingleton<ICommandService, CommandService>();
        collection.AddSingleton<ConfigWindow>();
        collection.AddSingleton<ILogger, Logger>();

        collection.AddSingleton<IDataService, DataService>();
        collection.AddSingleton<ILocalTTSService, LocalTTSService>();
        collection.AddSingleton<IMessageDispatcher, MessageDispatcher>();
        collection.AddSingleton<IGameInteropService, GameInteropService>();
        collection.AddSingleton<ILipSync, LipSync>();
        collection.AddSingleton<ISoundFilter, SoundFilter>();
        collection.AddSingleton<IPlaybackService, PlaybackService>();
        collection.AddSingleton<IAudioPostProcessor, AudioPostProcessor>();
        collection.AddSingleton<IAddonBattleTalkProvider, AddonBattleTalkProvider>();
        collection.AddSingleton<IAddonMiniTalkProvider, AddonMiniTalkProvider>();
        collection.AddSingleton<IChatMessageProvider, ChatMessageProvider>();
        collection.AddSingleton<IAddonTalkProvider, AddonTalkProvider>();
        collection.AddSingleton<IReportService, ReportService>();

        collection.AddSingleton(InitializeConfiguration);
        collection.AddSingleton(new WindowSystem("XivVoices"));

        collection.AddHostedService(sp => sp.GetRequiredService<IDataService>());
        collection.AddHostedService(sp => sp.GetRequiredService<IWindowService>());
        collection.AddHostedService(sp => sp.GetRequiredService<ICommandService>());
        collection.AddHostedService(sp => sp.GetRequiredService<ISoundFilter>());
        collection.AddHostedService(sp => sp.GetRequiredService<IMessageDispatcher>());
        collection.AddHostedService(sp => sp.GetRequiredService<IPlaybackService>());
        collection.AddHostedService(sp => sp.GetRequiredService<IAudioPostProcessor>());
        collection.AddHostedService(sp => sp.GetRequiredService<IReportService>());
        collection.AddHostedService(sp => sp.GetRequiredService<IAddonBattleTalkProvider>());
        collection.AddHostedService(sp => sp.GetRequiredService<IAddonMiniTalkProvider>());
        collection.AddHostedService(sp => sp.GetRequiredService<IChatMessageProvider>());
        collection.AddHostedService(sp => sp.GetRequiredService<IAddonTalkProvider>());
      }).Build();

    _host.StartAsync();
  }

  private Configuration InitializeConfiguration(IServiceProvider s)
  {
    ILogger logger = s.GetRequiredService<ILogger>();
    IDalamudPluginInterface pluginInterface = s.GetRequiredService<IDalamudPluginInterface>();
    Configuration configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    configuration.Initialize(logger, pluginInterface);
    return configuration;
  }

  public void Dispose()
  {
    _host.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    _host.Dispose();
  }
}
