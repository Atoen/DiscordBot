﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Victoria;

namespace DiscordBot.Services;

public class LoggingService
{
    public LoggingService(DiscordSocketClient discord, CommandService commands, LavaNode lavaNode)
    {
        discord.Log += OnLogAsync;
        commands.Log += OnLogAsync;
        lavaNode.OnLog += OnLogAsync;
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