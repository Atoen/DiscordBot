using DiscordBot.Services;

namespace DiscordBot.Modules;

public class NewMusicModule : ModuleBase<SocketCommandContext>
{
    private readonly LavaNode _lavaNode;
    private readonly NewAudioService _audioService;
    
    public NewMusicModule(LavaNode lavaNode, NewAudioService audioService)
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
        
        try
        {
            await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChanel);
            _audioService.CreatePlayerState(guild.Id);
        }
        catch (Exception exception)
        {
            var errorEmbed = await EmbedHandler.CreateErrorEmbed("Music, Join", exception.Message);
            await ReplyAsync(embed: errorEmbed);
        }
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
            var embed =  await EmbedHandler.CreateBasicEmbed("Music, Join",
                "You must be connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }
        
        try
        {
            await _lavaNode.LeaveAsync(voiceState?.VoiceChannel);
            _audioService.RemovePlayerState(guild.Id);
        }
        catch (Exception exception)
        {
            var errorEmbed = await EmbedHandler.CreateErrorEmbed("Music, Leave", exception.Message);
            await ReplyAsync(embed: errorEmbed);
        }
    }
    #endregion

    #region Play
    [Command("play"), Alias("p")]
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
        
        if (!_lavaNode.TryGetPlayer(guild, out var player))
        {
            embed = await EmbedHandler.CreateBasicEmbed("Music, Play",
                "I'm not connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }
        
        if (voiceState?.VoiceChannel == null)
        {
            embed =  await EmbedHandler.CreateBasicEmbed("Music, Play",
                "You must be connected to a voice channel.", Color.DarkRed);
            await ReplyAsync(embed: embed);
            return;
        }

        var track = await _audioService.SearchSongAsync(query);
        if (track == null)
        {
            embed = await EmbedHandler.CreateErrorEmbed("Music, Play", "Couldn't retrieve the track.");
            await ReplyAsync(embed: embed);
            return;
        }

        var embedBuilder = new EmbedBuilder();
        
        // Utworzenie link do miniaturki filmiku (w średniej jakości)
        var videoId = track.Url.Split("watch?v=")[1].Split("&")[0];
        var thumbnailUrl = $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg";
        
        // Jeśli player coś odtwarza to znaleziony utwór jest dodawany do kolejki
        if (player.Track != null && player.PlayerState is PlayerState.Playing or PlayerState.Paused)
        {
            player.Queue.Enqueue(track);

            embedBuilder.WithTitle("Music")
                .WithDescription($"[{track.Title}]({track.Url}) has been added to the queue at #{player.Queue.Count + 1}.")
                .WithColor(Color.DarkGreen);
                
            if (videoId != string.Empty)
            {
                embedBuilder.WithImageUrl(thumbnailUrl);
                await ReplyAsync(embed: embedBuilder.Build());
                return;
            }
        }

        await player.PlayAsync(track);
        
        // Jeśli jest z poza youtube
        if (videoId == string.Empty)
        {
            embed = await EmbedHandler.CreateBasicEmbed("Now Playing",
                $"Now playing: [{track.Title}]({track.Url}).", Color.DarkGreen);
            await ReplyAsync(embed: embed);
            return;
        }

        embedBuilder.WithTitle("Now Playing")
            .WithDescription($"Now playing: [{track.Title}]({track.Url}).")
            .WithImageUrl(thumbnailUrl)
            .WithColor(Color.DarkGreen);

        await ReplyAsync(embed: embedBuilder.Build());
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
    public async Task SkipTrack()
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

        var currentTrack = player.Track;
        
        if (player.Queue.Count < 1)
        {
            await player.StopAsync();
            
            if (currentTrack == null)
            {
                embed = await EmbedHandler.CreateBasicEmbed("Music, Skip", 
                    "Nothing to skip.", Color.Blue);
                await ReplyAsync(embed: embed);
                return;
            }
            
            embed = await EmbedHandler.CreateBasicEmbed("Music, Skip", 
                $"Skipped [{player.Track.Title}]({player.Track.Url}).", Color.Blue);
            await ReplyAsync(embed: embed);
            return;
        }

        await player.SkipAsync();
        
        embed = await EmbedHandler.CreateBasicEmbed("Music, Skip", 
            $"Skipped [{currentTrack?.Title}]({currentTrack?.Url}).", Color.Blue);
        await ReplyAsync(embed: embed);
    }
    #endregion

    #region Volume
    [Command("volume"), Alias("vol")]
    public async Task SetVolumeAsync(ushort volume)
    {
        if (volume is > 150 or <= 0)
        {
            await ReplyAsync("Volume must be between 1 and 150.");
            return;
        }

        var guild = Context.Guild;

        if (!_lavaNode.HasPlayer(guild))
        {
            await ReplyAsync("I'm not connected to a voice channel.");
            return;
        }

        var player = _lavaNode.GetPlayer(guild);
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
}