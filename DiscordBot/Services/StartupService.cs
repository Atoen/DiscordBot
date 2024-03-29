﻿using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Services;

public class StartupService
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IConfigurationRoot _config;
    private readonly IServiceProvider _provider;
    
    public StartupService(DiscordSocketClient client, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
    {
        _client = client;
        _commands = commands;
        _config = config;
        _provider = provider;

        _client.Log += LoggingService.Log;
        _commands.Log += LoggingService.Log;
    }

    public async Task StartAsync()
    {
        var token = _config["token"];

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new Exception("No token provided in configuration.");
        }

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        
        await _client.SetGameAsync("Diuna", null, ActivityType.Streaming);
    }
}