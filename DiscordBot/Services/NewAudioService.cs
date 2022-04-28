namespace DiscordBot.Services;

public class NewAudioService
{
    private readonly LavaNode _lavaNode;
    private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens = new();
    private readonly ConcurrentDictionary<ulong, NewPlayerState> _playerStates = new();

    public NewAudioService(LavaNode lavaNode)
    {
        _lavaNode = lavaNode;
        _lavaNode.OnTrackStarted += OnTrackStarted;
        _lavaNode.OnTrackEnded += OnTrackEnded;
    }

    public void CreatePlayerState(ulong guildId)
    {
        _playerStates.TryAdd(guildId, new NewPlayerState());
    }

    public void RemovePlayerState(ulong guildId)
    {
        _playerStates.TryRemove(guildId, out var _);
    }

    public async Task<LavaTrack?> SearchSongAsync(string query)
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

    public async Task<Embed> LoopAsync(LavaPlayer player, int loopTimes)
    {
        if (!_playerStates.TryGetValue(player.VoiceChannel.GuildId, out var playerState))
        {
            return await EmbedHandler.CreateErrorEmbed("Music, Loop",
                "Couldn't retrieve the player state for looping");
        }

        playerState.LastTrack = player.Track;
        
        // Komenda wywołana z argumentem - loopowanie podaną ilość razy
        if (loopTimes != -1)
        {
            playerState.LoopTimes = loopTimes;
            
            return await EmbedHandler.CreateBasicEmbed("Music, Loop",
                $"Looping [{player.Track?.Title}]({player.Track?.Url}) {loopTimes} times.", Color.Blue);
        }
        
        // Wywołanie bez argumentu
        if (playerState.Looped)
        {
            playerState.Looped = false;
            
            return await EmbedHandler.CreateBasicEmbed("Music, Loop", 
                $"Unlooping [{player.Track?.Title}]({player.Track?.Url})", Color.Blue);
        }

        playerState.Looped = true;
        
        return await EmbedHandler.CreateBasicEmbed("Music, Loop",
            $"Looping [{player.Track?.Title}]({player.Track?.Url})", Color.Blue);
    }

    private async Task OnTrackStarted(TrackStartEventArgs arg)
    {
        if (!_disconnectTokens.TryGetValue(arg.Player.VoiceChannel.Id, out var cancellationToken))
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        cancellationToken.Cancel(true);
        await arg.Player.TextChannel.SendMessageAsync("Auto disconnect has been cancelled!");
    }

    private async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (args.Reason is not (TrackEndReason.Finished or TrackEndReason.LoadFailed))
        {
            return;
        }
        
        if (!_playerStates.TryGetValue(args.Player.VoiceChannel.GuildId, out var playerState))
        {
            await args.Player.TextChannel.SendMessageAsync("An error occured while switching tracks");
            return;
        }
        
        LavaTrack track;

        if (!playerState.Looped)
        {
            if (!args.Player.Queue.TryDequeue(out var queueable))
            {
                await args.Player.TextChannel.SendMessageAsync("Queue completed! Please add more tracks to rock n' roll!");
                _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(10));
                return;
            }

            track = queueable;

            var embed = await EmbedHandler.CreateBasicEmbed("Now Playing",
                $"Now playing: [{track.Title}]({track.Url})", Color.DarkGreen);

            await args.Player.TextChannel.SendMessageAsync(embed: embed);
        }

        else
        {
            track = playerState.LastTrack!;
        }

        playerState.PerformLoop();
        await args.Player.PlayAsync(track);
    }

    private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
    {
        if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var cancellationToken))
        {
            cancellationToken = new CancellationTokenSource();
            _disconnectTokens.TryAdd(player.VoiceChannel.Id, cancellationToken);
        }
        else if (cancellationToken.IsCancellationRequested)
        {
            _disconnectTokens.TryUpdate(player.VoiceChannel.Id,new CancellationTokenSource(), cancellationToken);
            cancellationToken = _disconnectTokens[player.VoiceChannel.Id];
        }

        await player.TextChannel.SendMessageAsync($"Auto disconnect initiated! Disconnecting in {timeSpan}...");
        var isCancelled = SpinWait.SpinUntil(() => cancellationToken.IsCancellationRequested, timeSpan);
        if (isCancelled)
        {
            return;
        }

        await _lavaNode.LeaveAsync(player.VoiceChannel);
        await player.TextChannel.SendMessageAsync("Invite me again sometime, sugar.");
        _playerStates.TryRemove(player.VoiceChannel.GuildId, out var _);
    }
}