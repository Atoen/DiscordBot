using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;

namespace DiscordBot;

public class Startup
{
    // private readonly DiscordSocketClient _client;
    // private readonly CommandService _commands;
    // private readonly IServiceProvider _services;
    
    public IConfigurationRoot Configuration { get; }

    internal Startup()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json");
        
        Configuration = builder.Build();
        
        // _client = new DiscordSocketClient(new DiscordSocketConfig
        // {
        //     LogLevel = LogSeverity.Info,
        //     MessageCacheSize = 100
        // });
        //
        // _commands = new CommandService(new CommandServiceConfig
        // {
        //     LogLevel = LogSeverity.Info,
        //     CaseSensitiveCommands = false
        // });
        //
        // _client.Log += Log;
        // _commands.Log += Log;
        //
        // _services = ConfigureServices();
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

        // await InitCommands();
        // await _client.LoginAsync(TokenType.Bot, Configuration["token"]);
        // await _client.StartAsync();
        //
        // await Task.Delay(Timeout.Infinite);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // var map = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        //
        // map.AddSingleton<CommandHandler>();
        //
        // return map.BuildServiceProvider();

        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100
        }))
        .AddSingleton(new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Info,
            CaseSensitiveCommands = false
        }))
        .AddSingleton<CommandHandler>()
        .AddSingleton<StartupService>()
        .AddSingleton<LoggingService>()
        .AddSingleton(Configuration);
    }

    private static Task Log(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }

        Console.WriteLine(
            $"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
        Console.ResetColor();

        return Task.CompletedTask;
    }
}