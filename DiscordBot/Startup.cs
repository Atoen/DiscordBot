using DiscordBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DiscordBot;

public class Startup
{
    private readonly IConfigurationRoot _configuration;
    private readonly DiscordSocketClient _client;
    private LavaNode? _lavaNode;

    internal Startup()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json");
        
        _configuration = builder.Build();

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100
        });

        _client.Ready += ClientOnReady;
    }

    private async Task ClientOnReady()
    {
        if (_lavaNode is {IsConnected: false})
        {
            await _lavaNode.ConnectAsync();
            // _lavaNode.OnTrackEnded += AudioService.TrackEnded;
        }
    }

    internal async Task MainAsync()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        ConfigureServices(services);
        
        var provider = services.BuildServiceProvider();
        
        // Uruchomienie usług
        provider.GetRequiredService<CommandHandler>();
        provider.GetRequiredService<LoggingService>();

        _lavaNode = provider.GetRequiredService<LavaNode>();
        
        await provider.GetRequiredService<StartupService>().StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var localhost = true;
        
        var commandService = new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Info,
            CaseSensitiveCommands = false
        });

        var server = localhost ? "localHost" : "lavaServer";

        services.AddSingleton(_client)
            .AddSingleton(commandService)
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<NewAudioService>()
            .AddSingleton<SoundEffectsService>()
            .AddLavaNode(lava =>
            {
                lava.Hostname = _configuration[$"{server}:host"];
                lava.Port = ushort.Parse(_configuration[$"{server}:port"]);
                lava.Authorization = _configuration[$"{server}:password"];
                lava.IsSsl = false;
                lava.SelfDeaf = bool.Parse(_configuration[$"{server}:ssl"]);
            })
            .AddSingleton(_configuration);
    }
}
