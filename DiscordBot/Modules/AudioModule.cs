using Discord;
using Discord.Commands;
using Victoria;
using Victoria.Enums;
using DiscordBot.Handlers;
using DiscordBot.Services;
using Victoria.Responses.Search;

namespace DiscordBot.Modules;

public sealed class AudioModule : ModuleBase<SocketCommandContext>
{
    private readonly AudioService _audioService;

    public AudioModule(AudioService audioService) => _audioService = audioService;

    [Command("join")]
    [Summary("Joins the voice channel you are in.")]
    public async Task Join()
    {
        var embed = await _audioService.JoinAsync(Context);
        if (embed != null) await ReplyAsync(embed: embed);
    }

    [Command("leave")]
    [Summary("Leaves the voice channel")]
    public async Task Leave()
    {
        var embed = await _audioService.LeaveAsync(Context);
        if (embed != null) await ReplyAsync(embed: embed);
    }

    [Command("play")]
    [Alias("p", "grajcuj", "zapodawaj", "dźwięcz")]
    [Summary("Searches and plays the requested video.")]
    public async Task Play([Remainder] string query)
    {
        var embed = await _audioService.PlayAsync(Context, query);
        await ReplyAsync(embed: embed);
    }
    
    [Command("list")]
    [Summary("Lists all the songs in the queue.")]
    public async Task List()
    {
        var embed = await _audioService.ListAsync(Context);
        await ReplyAsync(embed: embed);
    }

    [Command("skip")]
    [Summary("Skips the currently playing song")]
    public async Task Skip()
    {
        var embed = await _audioService.SkipTrackAsync(Context);
        await ReplyAsync(embed: embed);
    }

    [Command("Volume")]
    [Alias("vol")]
    [Summary("Sets the player volume")]
    public async Task Volume(ushort volume = 100)
    {
        var message = await _audioService.SetVolumeAsync(Context, volume);
        await ReplyAsync(message);
    }
}