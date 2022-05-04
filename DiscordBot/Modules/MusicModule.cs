using DiscordBot.Services;

namespace DiscordBot.Modules;

[Name("Music")]
public class MusicModule : ModuleBase<SocketCommandContext>
{
    private readonly LavaNode _lavaNode;
    private readonly AudioService _audioService;
    
    public MusicModule(LavaNode lavaNode, AudioService audioService)
    {
        _lavaNode = lavaNode;
        _audioService = audioService;
    }

    #region Join
    [Command("join")]
    public async Task JoinAsync()
    {
        var guild = Context.Guild;
        var voiceState = Context.User as IVoiceState;
        var textChanel = Context.Channel as ITextChannel;
        
        if (_lavaNode.HasPlayer(guild))
        {
            var embed = await EmbedHandler.CreateBasicEmbed("Music, Join",
                "Already connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }
        
        if (voiceState?.VoiceChannel == null)
        {
            var embed =  await EmbedHandler.CreateBasicEmbed("Music, Join",
                "You must be connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }

        var errorEmbed = await _audioService.JoinAsync(voiceState.VoiceChannel, textChanel!);
        if (errorEmbed != null) await ReplyAsync(embed: errorEmbed);
    }
    #endregion

    #region Leave
    [Command("leave")]
    public async Task LeaveAsync()
    {
        var guild = Context.Guild;
        var voiceState = Context.User as IVoiceState;

        if (!_lavaNode.HasPlayer(guild))
        {
            var embed = await EmbedHandler.CreateBasicEmbed("Music, Leave",
                "I'm not in a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }
        
        if (voiceState?.VoiceChannel == null)
        {
            var embed =  await EmbedHandler.CreateBasicEmbed("Music, Leave",
                "You must be connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }

        var errorEmbed = await _audioService.LeaveAsync(voiceState.VoiceChannel);
        if (errorEmbed != null) await ReplyAsync(embed: errorEmbed);
    }
    #endregion

    #region Play
    [Command("play"), Alias("p", "grajcuj", "zapodawaj", "dźwięcz")]
    public async Task PlayAsync([Remainder] string query)
    {
        Embed embed;
        
        if (string.IsNullOrWhiteSpace(query))
        {
            embed = await EmbedHandler.CreateBasicEmbed("Music, Play",
                "No search terms provided.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }

        var guild = Context.Guild;
        var voiceState = Context.User as IVoiceState;

        if (voiceState?.VoiceChannel == null)
        {
            embed =  await EmbedHandler.CreateBasicEmbed("Music, Play",
                "You must be connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }

        if (!_lavaNode.HasPlayer(guild))
        {
            await JoinAsync();
        }

        var player = _lavaNode.GetPlayer(guild);

        embed = await _audioService.PlayAsync(player, query);
        await ReplyAsync(embed: embed);
    }
    #endregion

    #region List
    [Command("list")]
    public async Task ListAsync()
    {
        var guild = Context.Guild;
        Embed embed;
        
        if (!_lavaNode.TryGetPlayer(guild, out var player))
        {
            embed = await EmbedHandler.CreateBasicEmbed("Music, List",
                "I'm not connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }
        
        if (player.PlayerState is not (PlayerState.Playing or PlayerState.Paused))
        {
            embed = await EmbedHandler.CreateBasicEmbed("Music Playlist", 
                "No music is being played right now.", Color.Blue);
            await ReplyAsync(embed: embed);
            return;
        }
        
        if (player.Queue.Count < 1 && player.Track != null)
        {
            embed = await EmbedHandler.CreateBasicEmbed("Music Playlist", 
                $"Now playing: [{player.Track.Title}]({player.Track.Url}).", Color.Blue);
            await ReplyAsync(embed: embed);
            return;
        }

        var descriptionBuilder = new StringBuilder();
        var trackNum = 2;
        foreach (var track in player.Queue)
        {
            descriptionBuilder.Append($"#{trackNum}: [{track.Title}]({track.Url})\n");
            trackNum++;
        }
        
        embed = await EmbedHandler.CreateBasicEmbed("Music Playlist", 
            $"Now playing: [{player.Track?.Title}]({player.Track?.Url}) \n{descriptionBuilder}", Color.Blue);
        await ReplyAsync(embed: embed);
    }
    #endregion

    #region Skip
    [Command("skip")]
    public async Task SkipTrackAsync()
    {
        var guild = Context.Guild;
        var voiceState = Context.User as IVoiceState;
        Embed embed;
        
        if (!_lavaNode.TryGetPlayer(guild, out var player))
        {
            embed = await EmbedHandler.CreateBasicEmbed("Music, Skip",
                "I'm not connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }
        
        if (voiceState?.VoiceChannel == null)
        {
            embed =  await EmbedHandler.CreateBasicEmbed("Music, Skip",
                "You must be connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }

        embed = await _audioService.SkipAsync(player);
        await ReplyAsync(embed: embed);
    }
    #endregion

    #region Volume
    [Command("volume"), Alias("vol")]
    public async Task SetVolumeAsync(ushort volume = 100)
    {
        if (volume is > 150 or <= 0)
        {
            await ReplyAsync("Volume must be between 1 and 150.");
            return;
        }

        var guild = Context.Guild;
        var voiceState = Context.User as IVoiceState;

        if (!_lavaNode.TryGetPlayer(guild, out var player))
        {
            await ReplyAsync("I'm not connected to a voice channel.");
            return;
        }
        
        if (voiceState?.VoiceChannel == null)
        {
            await ReplyAsync("You must be connected to a voice channel");
            return;
        }
        
        await player.UpdateVolumeAsync(volume);
        await ReplyAsync($"Volume set to {volume}%.");
    }
    #endregion

    #region Loop
    [Command("loop")]
    public async Task LoopAsync(int loopTimes = -1)
    {
        var guild = Context.Guild;
        var voiceState = Context.User as IVoiceState;
        Embed embed;
        
        if (!_lavaNode.TryGetPlayer(guild, out var player))
        {
            embed = await EmbedHandler.CreateBasicEmbed("Music, Loop",
                "I'm not connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }
        
        if (voiceState?.VoiceChannel == null)
        {
            embed =  await EmbedHandler.CreateBasicEmbed("Music, Loop",
                "You must be connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }
        
        if (player.Track == null)
        {
            embed = await EmbedHandler.CreateBasicEmbed("Music, Loop", 
                "Nothing is being played right now.", Color.Blue);
            await ReplyAsync(embed: embed);
            return;
        }

        embed = await _audioService.LoopAsync(player, loopTimes);
        await ReplyAsync(embed: embed);
    }
    #endregion

    #region Effect

    [Command("effect"), Alias("sfx, soundeffect")]
    public async Task ApplyEffectAsync(string effect)
    {
        var guild = Context.Guild;
        var voiceState = Context.User as IVoiceState;

        if (!_lavaNode.TryGetPlayer(guild, out var player))
        {
            await ReplyAsync("I'm not connected to a voice channel.");
            return;
        }
        
        if (voiceState?.VoiceChannel == null)
        {
            await ReplyAsync("You must be connected to a voice channel");
            return;
        }

        var reply = _audioService.ApplySoundEffectAsync(player, effect);
    }
    #endregion
}
