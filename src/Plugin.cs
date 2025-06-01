using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace XivVoices;

public sealed class Plugin : IDalamudPlugin
{
  private readonly IHost _host;

  public Plugin(
    IDalamudPluginInterface pluginInterface,
    IChatGui chatGui,
    IClientState clientState,
    ICommandManager commandManager,
    IFramework framework,
    IPluginLog pluginLog,
    IToastGui toastGui,
    IAddonLifecycle addonLifecycle,
    IObjectTable objectTable
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
        collection.AddSingleton(clientState);
        collection.AddSingleton(commandManager);
        collection.AddSingleton(framework);
        collection.AddSingleton(pluginLog);
        collection.AddSingleton(toastGui);
        collection.AddSingleton(addonLifecycle);
        collection.AddSingleton(objectTable);

        collection.AddSingleton<WindowService>();
        collection.AddSingleton<CommandService>();
        collection.AddSingleton<ConfigWindow>();
        collection.AddSingleton<SetupWindow>();

        collection.AddSingleton<Logger>();
        collection.AddSingleton<InteropService>();
        collection.AddSingleton<ReportService>();
        collection.AddSingleton<SpeechService>();
        collection.AddSingleton<DataService>();
        collection.AddSingleton<DataMapper>();
        collection.AddSingleton<AddonService>();

        collection.AddSingleton(InitializeConfiguration);
        collection.AddSingleton(new WindowSystem("XivVoices"));

        collection.AddHostedService(sp => sp.GetRequiredService<WindowService>());
        collection.AddHostedService(sp => sp.GetRequiredService<CommandService>());
        collection.AddHostedService(sp => sp.GetRequiredService<ReportService>());
        collection.AddHostedService(sp => sp.GetRequiredService<SpeechService>());
        collection.AddHostedService(sp => sp.GetRequiredService<DataService>());
        collection.AddHostedService(sp => sp.GetRequiredService<AddonService>());
      }).Build();

    _host.StartAsync();
  }

  private Configuration InitializeConfiguration(IServiceProvider s)
  {
    Logger logger = s.GetRequiredService<Logger>();
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
