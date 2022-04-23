using DiscordBot.Services;
using DiscordBot.Handlers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Victoria;

namespace DiscordBot;

public class Startup
{
    public IConfigurationRoot Configuration { get; }
    private readonly DiscordSocketClient _client;
    private LavaNode? _lavaNode;

    internal Startup()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json");
        
        Configuration = builder.Build();

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100
        });

        _client.Ready += ClientOnReady;
    }

    private async Task ClientOnReady()
    {
        Console.WriteLine("connecting lava");


        if (_lavaNode is {IsConnected: false}) await _lavaNode.ConnectAsync();
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
        services.AddSingleton(_client)
        .AddSingleton(new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Info,
            CaseSensitiveCommands = false
        }))
        .AddSingleton<CommandHandler>()
        .AddSingleton<StartupService>()
        .AddSingleton<LoggingService>()
        .AddSingleton<AudioService>()
        .AddLavaNode(_ =>
        {
            new LavaConfig
            {
                Hostname = "127.0.0.1",
                Port = 2333,
                Authorization = "youshallnotpass",
                LogSeverity = LogSeverity.Info
            };
        } )
        .AddSingleton(Configuration);
    }
}