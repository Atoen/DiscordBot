using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using DiscordBot.Services;
using SharpLink;
using SharpLink.Enums;

namespace DiscordBot.Modules;

public class AudioModule : ModuleBase<SocketCommandContext>
{
    private readonly AudioService _service;

    public AudioModule(AudioService audioService)
    {
        _service = audioService;
    }
    
    [Command("join", RunMode = RunMode.Async)]
    [Summary("Joins the voice channel you are in.")]
    public async Task JoinChannelAsync()
    {
        var voiceChannel = (Context.User as IVoiceState)?.VoiceChannel;

        if (voiceChannel != null)
        {
            await _service.JoinAudio(Context.Guild, voiceChannel);
        }

        else
        {
            await ReplyAsync("You need to be in a voice channel to use this command.");
        }
    }
    
    [Command("leave", RunMode = RunMode.Async)]
    [Summary("Leaves the voice channel you are in.")]
    public async Task LeaveChannelAsync()
    {
        await _service.LeaveAudio(Context.Guild);
    }
}

    