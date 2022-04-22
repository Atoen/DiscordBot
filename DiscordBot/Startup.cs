using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using SharpLink;

namespace DiscordBot;

public class Startup
{
    public IConfigurationRoot Configuration { get; }
    private readonly DiscordSocketClient _client;

    internal Startup()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json");
        
        Configuration = builder.Build();
        
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Verbose,
            MessageCacheSize = 100
        });
    }

    internal async Task MainAsync()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        ConfigureServices(services);
        
        var provider = services.BuildServiceProvider();
        
        // Uruchomienie usług
        provider.GetRequiredService<CommandHandler>();
        provider.GetRequiredService<LoggingService>();
        
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
        .AddSingleton(Configuration);
    }
}