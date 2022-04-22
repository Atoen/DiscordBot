using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Services;

public class StartupService
{
    private readonly DiscordSocketClient _discord;
    private readonly CommandService _commands;
    private readonly IConfigurationRoot _config;
    private readonly IServiceProvider _provider;
    
    public StartupService(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
    {
        _discord = discord;
        _commands = commands;
        _config = config;
        _provider = provider;
    }
    
    public async Task StartAsync()
    {
        var token = _config["token"];

        await _discord.LoginAsync(TokenType.Bot, token);
        await _discord.StartAsync();
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
    }
}