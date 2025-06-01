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
    IPluginLog pluginLog
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

        collection.AddSingleton<WindowService>();
        collection.AddSingleton<CommandService>();
        collection.AddSingleton<ConfigWindow>();
        collection.AddSingleton<SetupWindow>();

        collection.AddSingleton<Logger>();

        collection.AddSingleton(InitializeConfiguration);
        collection.AddSingleton(new WindowSystem("XivVoices"));

        collection.AddHostedService<WindowService>();
        collection.AddHostedService<CommandService>();
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
