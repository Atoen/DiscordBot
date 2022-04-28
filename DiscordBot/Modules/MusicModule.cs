using DiscordBot.Services;

namespace DiscordBot.Modules;
#if false
public sealed class MusicModule : ModuleBase<SocketCommandContext>
{
    private readonly AudioService _audioService;
    private readonly SoundEffectsService _sfxService;

    public MusicModule(AudioService audioService, SoundEffectsService soundEffectsService)
    {
        _audioService = audioService;
        _sfxService = soundEffectsService;
    }

    [Command("joinf")]
    [Summary("Joins the voice channel you are in.")]
    public async Task Join()
    {
        var embed = await _audioService.JoinAsync(Context);
        if (embed != null) await ReplyAsync(embed: embed);
    }

    [Command("leavef")]
    [Summary("Leaves the voice channel")]
    public async Task Leave()
    {
        var embed = await _audioService.LeaveAsync(Context);
        if (embed != null) await ReplyAsync(embed: embed);
    }

    [Command("playf")]
    [Alias("p", "grajcuj", "zapodawaj", "dźwięcz")]
    [Summary("Searches and plays the requested video.")]
    public async Task Play([Remainder] string query)
    {
        var embed = await _audioService.PlayAsync(Context, query);
        await ReplyAsync(embed: embed);
    }
    
    [Command("listf")]
    [Summary("Lists all the songs in the queue.")]
    public async Task List()
    {
        var embed = await _audioService.ListAsync(Context);
        await ReplyAsync(embed: embed);
    }

    [Command("skipf")]
    [Summary("Skips the currently playing song.")]
    public async Task Skip()
    {
        var embed = await _audioService.SkipTrackAsync(Context);
        await ReplyAsync(embed: embed);
    }

    [Command("volumef")]
    [Alias("vol")]
    [Summary("Sets the player volume.")]
    public async Task Volume(ushort volume = 100)
    {
        var message = await _audioService.SetVolumeAsync(Context, volume);
        await ReplyAsync(message);
    }

    [Command("loopf")]
    [Summary("Loops the currently playing song")]
    public async Task Loop(int loopTimes = -1)
    {
        var embed = await _audioService.LoopAsync(Context, loopTimes);
        await ReplyAsync(embed: embed);
    }

    [Command("effectf")]
    [Alias("sfx", "soundeffect")]
    [Summary("Applies sound effect on the player")]
    public async Task Effect([Remainder] string effect)
    {
        var message = await _sfxService.ApplySoundEffectAsync(Context, effect);
        await ReplyAsync(message);
    }
}
#endif