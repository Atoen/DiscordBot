using Microsoft.Extensions.Logging;

namespace DiscordBot.Services;

public class AudioService
{
    private readonly LavaNode _lavaNode;
    private readonly ILogger _logger;
    private static readonly ConcurrentDictionary<SocketGuild, PlayerStateStruct> PlayerStatesDict = new();
    private static readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconectTokens = new();

    public AudioService(LavaNode lavaNode, LoggerFactory loggerFactory)
    {
        _lavaNode = lavaNode;
        _logger = loggerFactory.CreateLogger<LavaNode>();
        _lavaNode.OnLog += logMessage =>
        {
            _logger.Log((LogLevel) logMessage.Severity, logMessage.Exception, logMessage.Message);
            return Task.CompletedTask;
        };
    } 

    /// <summary>
    /// Dołącza do kanału głosowego użytkownika wywołującego komendę.
    /// </summary>
    /// <param name="context"> Kontekst komendy z informacjami </param>
    /// <returns>
    /// Embed z informacjami o możliwym błędzie
    /// null - jeśli udało się dołączyć do kanału
    /// </returns>
    public async Task<Embed?> JoinAsync(SocketCommandContext context)
    {
        var guild = context.Guild;
        var voiceState = context.User as IVoiceState;
        var textChanel = context.Channel as ITextChannel;
        
        if (_lavaNode.HasPlayer(guild))
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Join",
                "Already connected to a voice channel.", Color.DarkRed);
        }
        
        if (voiceState?.VoiceChannel == null)
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Join",
                "You must be connected to a voice channel.", Color.DarkRed);
        }

        var stateStruct = new PlayerStateStruct(voiceState.VoiceChannel);
        stateStruct.TimedOut += (_, _) => LeaveAfterTimeoutAsync(stateStruct.VoiceChannel);
        
        PlayerStatesDict.TryAdd(guild, stateStruct);

        try
        {
            await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChanel);
            
            // Poprawne dołączenie - brak embeda
            return null;
        }
        catch (Exception exception)
        {
            return await EmbedHandler.CreateErrorEmbed("Music, Join", exception.Message);
        }
    }

    /// <summary>
    /// Opuszcza obecny kanał głosowy
    /// </summary>
    /// <param name="context"> Kontekst komendy z informacjami </param>
    /// <returns>
    /// Embed z informacjami o możliwym błędzie
    /// null - jeśli udało się odłączyć od kanału
    /// </returns>
    public async Task<Embed?> LeaveAsync(SocketCommandContext context)
    {
        var guild = context.Guild;
        var voiceState = context.User as IVoiceState;

        PlayerStatesDict.TryRemove(guild, out _);

        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Leave",
                "I'm not in a voice channel.", Color.DarkRed);
        }

        var player = _lavaNode.GetPlayer(guild);
        
        if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
        {
            await player.StopAsync();
            player.Queue.Clear();
        }
        
        try
        {
            await _lavaNode.LeaveAsync(voiceState?.VoiceChannel);
            
            // Poprawne odłączenie - brak embeda
            return null;
        }
        catch (Exception exception)
        {
            return await EmbedHandler.CreateErrorEmbed("Music, Leave", exception.Message);
        }
    }

    private Task OnTrackStarted(TrackStartEventArgs args)
    {
        if (!_disconectTokens.TryGetValue(args.Player.VoiceChannel.Id, out var cancellationToken))
        {
            return Task.CompletedTask;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }
        
        cancellationToken.Cancel(true);
        return Task.CompletedTask;
    }

    private async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (args.Reason is not (TrackEndReason.Finished or TrackEndReason.LoadFailed))
        {
            return;
        }

        var player = args.Player;
        if (!player.Queue.TryDequeue(out var queueable))
        {
            await player.TextChannel.SendMessageAsync("Queue completed");
            _ = InitiateDisconnect(player, TimeSpan.FromSeconds(10));
            return;
        }

        // if (queueable is not { } track)
        // {
        //     await player.TextChannel.SendMessageAsync("Next item in queue is not a track");
        //     return;
        // }

        await player.PlayAsync(queueable);
        var embed = await EmbedHandler.CreateBasicEmbed("Now Playing",
            $"Now playing: [{queueable.Title}]({queueable.Url})", Color.DarkGreen);

        await args.Player.TextChannel.SendMessageAsync(embed: embed);
    }

    private async Task InitiateDisconnect(LavaPlayer player, TimeSpan timeSpan)
    {
        if (!_disconectTokens.TryGetValue(player.VoiceChannel.Id, out var cancellationToken))
        {
            cancellationToken = new CancellationTokenSource();
            _disconectTokens.TryAdd(player.VoiceChannel.Id, cancellationToken);
        }
        
        else if (cancellationToken.IsCancellationRequested)
        {
            _disconectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), cancellationToken);
            cancellationToken = _disconectTokens[player.VoiceChannel.Id];
        }

        await player.TextChannel.SendMessageAsync($"Auto disconnect initiated! Disconnecting in {timeSpan}...");
        var isCancelled = SpinWait.SpinUntil(() => cancellationToken.IsCancellationRequested, timeSpan);
        if (isCancelled)
        {
            return;
        }

        await _lavaNode.LeaveAsync(player.VoiceChannel);
        await player.TextChannel.SendMessageAsync("Invite me again sometime, sugar.");
    }
    
    
    /// <summary>
    /// Opuszcza obecny kanał jeśli minęła określona ilość czasu bez interakcji
    /// </summary>
    /// <param name="voiceChannel"> Kanał do opuszczenia </param>
    private async void LeaveAfterTimeoutAsync(IVoiceChannel voiceChannel)
    {
        if (voiceChannel.Guild is SocketGuild guild)
        {
            PlayerStatesDict.TryRemove(guild, out _);
        }

        await _lavaNode.LeaveAsync(voiceChannel);
    }

    /// <summary>
    /// Wyszukuje i odtwarza podany film/piosenkę
    /// </summary>
    /// <param name="context"> Kontekst komendy z informacjami </param>
    /// <param name="query"> Fraza do wyszukania </param>
    /// <returns>
    /// Embed z informacjami o powodzeniu operacji
    /// </returns>
    public async Task<Embed> PlayAsync(SocketCommandContext context, string query)
    {
        var guild = context.Guild;
        var voiceState = context.User as IVoiceState;

        if (voiceState?.VoiceChannel == null)
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Play",
                "You must be in a voice channel.", Color.DarkOrange);
        }

        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Play",
                "I'm not connected to a voice channel.", Color.DarkOrange);
        }

        var player = _lavaNode.GetPlayer(guild);
        
        var track = await SearchSongAsync(query);
        if (track == null)
        {
            return await EmbedHandler.CreateErrorEmbed("Music, Play", "Couldn't retrieve the track.");
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

                return embedBuilder.Build();
            }
        }

        await player.PlayAsync(track);
        
        // Zapisywanie utworu (potrzebne do loopowania)
        if (PlayerStatesDict.TryGetValue(guild, out var stateStruct))
        {
            stateStruct.LastTrack = track;
            stateStruct.KeepConnected();
        }

        // Jeśli jest z poza youtube
        if (videoId == string.Empty)
        {
            return await EmbedHandler.CreateBasicEmbed("Now Playing",
                $"Now playing: [{track.Title}]({track.Url}).", Color.DarkGreen);
        }

        embedBuilder.WithTitle("Now Playing")
            .WithDescription($"Now playing: [{track.Title}]({track.Url}).")
            .WithImageUrl(thumbnailUrl)
            .WithColor(Color.DarkGreen);

        return embedBuilder.Build();
    }
    
    /// <summary>
    /// Wyszukiwanie utworu z zapytania
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    private async Task<LavaTrack?> SearchSongAsync(string query)
    {
        // Szukanie utworu - jeśli zapytanie nie jest poprawnym linkiem to utwór jest wyszukiwany na youtubie
        var search = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, query)
            : await _lavaNode.SearchYouTubeAsync(query);

        if (search.Status == SearchStatus.NoMatches)
        {
            return null;
        }

        var track = search.Tracks.FirstOrDefault();

        return track;
    }
    
    /// <summary>
    /// Wyświetla obecny stan kolejki
    /// </summary>
    /// <param name="context"> Kontekst komendy z informacjami </param>
    /// <returns>
    /// Embed ze stanem kolejki
    /// </returns>
    public async Task<Embed> ListAsync(SocketCommandContext context)
    {
        var guild = context.Guild;
        
        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHandler.CreateBasicEmbed("Music Playlist", 
                "I'm not connected to a voice channel.\n Queue is empty.", Color.DarkRed);
        }

        var player = _lavaNode.GetPlayer(guild);
        var descriptionBuilder = new StringBuilder();

        if (player.PlayerState is not (PlayerState.Playing or PlayerState.Paused))
        {
            return await EmbedHandler.CreateBasicEmbed("Music Playlist", 
                "No music is being played right now.", Color.Blue);
        }

        if (player.Queue.Count < 1 && player.Track != null)
        {
            return await EmbedHandler.CreateBasicEmbed("Music Playlist", 
                $"Now playing: [{player.Track.Title}]({player.Track.Url}).", Color.Blue);
        }

        var trackNum = 2;
        foreach (var track in player.Queue)
        {
            descriptionBuilder.Append($"#{trackNum}: [{track.Title}]({track.Url})\n");
            trackNum++;
        }
        
        return await EmbedHandler.CreateBasicEmbed("Music Playlist", 
            $"Now playing: [{player.Track?.Title}]({player.Track?.Url}) \n{descriptionBuilder}", Color.Blue);
    }
    
    /// <summary>
    /// Pomija obecne odtwarzaną piosenkę
    /// </summary>
    /// <param name="context"> Kontekst komendy z informacjami </param>
    /// <returns>
    /// Embed z potwierdzeniem pominięcia
    /// </returns>
    public async Task<Embed> SkipTrackAsync(SocketCommandContext context)
    {
        var guild = context.Guild;
        var voiceState = context.User as IVoiceState;
        
        if (voiceState?.VoiceChannel == null)
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Skip", 
                "You must be connected to a voice channel.", Color.DarkRed);
        }
        
        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Skip", 
                "I'm not connected to a voice channel.\n Nothing to skip.", Color.DarkRed);
        }

        var player = _lavaNode.GetPlayer(guild);
        var currentTrack = player.Track;

        if (player.Queue.Count < 1)
        {
            await player.StopAsync();
            
            if (currentTrack == null)
            {
                return await EmbedHandler.CreateBasicEmbed("Music, Skip", 
                    "Nothing to skip.", Color.Blue);
            }
            
            return await EmbedHandler.CreateBasicEmbed("Music, Skip", 
                $"Skipped [{player.Track.Title}]({player.Track.Url}).", Color.Blue);
        }
        
        await player.SkipAsync();
        
        // Zapisywanie utworu (potrzebne do loopowania)
        if (PlayerStatesDict.TryGetValue(guild, out var stateStruct))
        {
            stateStruct.LastTrack = currentTrack;
            stateStruct.Looped = false;
        }

        return await EmbedHandler.CreateBasicEmbed("Music, Skip", 
            $"Skipped [{currentTrack.Title}]({currentTrack.Url}).", Color.Blue);
    }

    /// <summary>
    /// Zmienia poziom głośności playera
    /// </summary>
    /// <param name="context"> Kontekst komendy z informacjami </param>
    /// <param name="volume"> Nowy poziom głośności w procentach </param>
    /// <returns>
    /// string z potwierdzeniem zmiany
    /// </returns>
    public async Task<string> SetVolumeAsync(SocketCommandContext context, ushort volume)
    {
        if (volume is > 150 or <= 0)
        {
            return "Volume must be between 1 and 150.";
        }

        var guild = context.Guild;

        if (!_lavaNode.HasPlayer(guild))
        {
            return "I'm not connected to a voice channel.";
        }

        var player = _lavaNode.GetPlayer(guild);
        await player.UpdateVolumeAsync(volume);
        
        return $"Volume set to {volume}%.";
    }

    /// <summary>
    /// Obsługuje event TrackEnded 
    /// </summary>
    /// <param name="args"> Argumenty eventu </param>
    public static async Task TrackEnded(TrackEndedEventArgs args)
    {
        if (args.Reason is not (TrackEndReason.Finished or TrackEndReason.LoadFailed))
        {
            return;
        }
        
        var guild = args.Player.VoiceChannel.Guild as SocketGuild;

        if (!PlayerStatesDict.TryGetValue(guild!, out var stateStruct))
        {
            await args.Player.TextChannel.SendMessageAsync("An error occured while switching tracks");
            return;
        }
        
        stateStruct.PerformLoop();
        stateStruct.KeepConnected();

        LavaTrack track;

        if (!stateStruct.Looped)
        {
            if (!args.Player.Queue.TryDequeue(out var queueable)) return;

            track = queueable;

            var embed = await EmbedHandler.CreateBasicEmbed("Now Playing",
                $"Now playing: [{track.Title}]({track.Url})", Color.DarkGreen);

            await args.Player.TextChannel.SendMessageAsync(embed: embed);
        }

        else
        {
            track = stateStruct.LastTrack!;
        }

        await args.Player.PlayAsync(track);
    }

    /// <summary>
    /// Zapętla obecne odtwarzaną piosenkę
    /// </summary>
    /// <param name="context"> Kontekst komendy z informacjami </param>
    /// <param name="loopTimes"> Ilość powtórzeń piosenki </param>
    /// <returns>
    /// Embed z potwierdzeniem zapętlenia
    /// </returns>
    public async Task<Embed> LoopAsync(SocketCommandContext context, int loopTimes)
    {
        var guild = context.Guild;
        var voiceState = context.User as IVoiceState;
        
        if (voiceState?.VoiceChannel == null)
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Loop", 
                "You must be connected to a voice channel.", Color.DarkRed);
        }
        
        if (!_lavaNode.HasPlayer(guild))
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Loop", 
                "I'm not connected to a voice channel.\n Nothing to loop.", Color.DarkRed);
        }
        
        var player = _lavaNode.GetPlayer(guild);
        
        if (player.Track == null)
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Loop", 
                "Nothing is being played right now.", Color.Blue);
        }
        
        if (!PlayerStatesDict.TryGetValue(guild, out var stateStruct))
        {
            return await EmbedHandler.CreateErrorEmbed("Music, Skip",
                "Couldn't retrieve the player state for looping");
        }

        stateStruct.LastTrack = player.Track;

        // Komenda wywołana z argumentem - loopowanie podaną ilość razy
        if (loopTimes != -1)
        {
            stateStruct.LoopTimes = loopTimes;
            
            return await EmbedHandler.CreateBasicEmbed("Music, Loop",
                $"Looping [{player.Track?.Title}]({player.Track?.Url}) {loopTimes} times.", Color.Blue);
        }
        
        // Wywołanie bez argumentu
        if (stateStruct.Looped)
        {
            stateStruct.Looped = false;
            
            return await EmbedHandler.CreateBasicEmbed("Music, Loop", 
                $"Unlooping [{player.Track?.Title}]({player.Track?.Url})", Color.Blue);
        }

        stateStruct.Looped = true;
        
        return await EmbedHandler.CreateBasicEmbed("Music, Loop",
            $"Looping [{player.Track?.Title}]({player.Track?.Url})", Color.Blue);
    }
}
