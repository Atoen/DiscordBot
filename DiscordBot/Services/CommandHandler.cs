namespace DiscordBot.Services;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

public class CommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IConfigurationRoot _config;
    private readonly IServiceProvider _provider;
    
    public CommandHandler(DiscordSocketClient client, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
    {
        _client = client;
        _commands = commands;
        _config = config;
        _provider = provider;
        
        _client.MessageReceived += OnMessageReceivedAsync;
    }

    private async Task OnMessageReceivedAsync(SocketMessage arg)
    {
        if (arg is not SocketUserMessage message || message.Author.IsBot) return;

        var argPos = 0;

        if (message.HasStringPrefix(_config["prefix"], ref argPos))
        {
            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider);
            
            if (!result.IsSuccess) await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}