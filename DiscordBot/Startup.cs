using DiscordBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

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
        if (_lavaNode is {IsConnected: false}) await _lavaNode.ConnectAsync();
    }

    internal async Task MainAsync()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        ConfigureServices(services);
        
        var provider = services.BuildServiceProvider();
        
        provider.GetRequiredService<CommandHandler>();
        _lavaNode = provider.GetRequiredService<LavaNode>();
        
        await provider.GetRequiredService<StartupService>().StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var localhost = false;
        
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
            .AddSingleton<AudioService>()
            .AddLavaNode(lava =>
            {
                lava.Hostname = _configuration[$"{server}:host"];
                lava.Port = ushort.Parse(_configuration[$"{server}:port"]);
                lava.Authorization = _configuration[$"{server}:password"];
                lava.IsSsl = bool.Parse(_configuration[$"{server}:ssl"]);
                lava.SelfDeaf = true;
            })
            .AddSingleton(_configuration);
    }
}
