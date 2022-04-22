using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Services;

public class LoggingService
{
    private readonly DiscordSocketClient _discord;
    private readonly CommandService _commands;
    
    public LoggingService(DiscordSocketClient discord, CommandService commands)
    {
        _discord = discord;
        _commands = commands;

        _discord.Log += OnLogAsync;
        _commands.Log += OnLogAsync;
    }
    
    private static Task OnLogAsync(LogMessage message)
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