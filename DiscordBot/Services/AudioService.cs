using Victoria.Filters;

namespace DiscordBot.Services;

public class AudioService
{
    private readonly LavaNode _lavaNode;
    private readonly ConcurrentDictionary<ulong, PlayerLoopState> _playerStates = new();

    public AudioService(LavaNode lavaNode)
    {
        _lavaNode = lavaNode;

        _lavaNode.OnTrackStarted += OnTrackStarted;
        _lavaNode.OnTrackEnded += OnTrackEnded;

        _lavaNode.OnLog += LoggingService.Log;
    }

    public async Task<Embed?> JoinAsync(IVoiceChannel voiceChannel, ITextChannel textChannel)
    {
        try
        {
            await _lavaNode.JoinAsync(voiceChannel, textChannel);

            var playerState = new PlayerLoopState();
            playerState.TimedOut += async delegate { await LeaveAsync(voiceChannel); };
            playerState.StartIdleTimer();

            _playerStates.TryAdd(voiceChannel.GuildId, playerState);

            await LoggingService.LogMessage("LavaPlayer", $"Joined {voiceChannel} - {voiceChannel.Guild}");
            return null;
        }
        catch (Exception exception)
        {
            await LoggingService.LogError("LavaPlayer", $"Failed to join {voiceChannel}", exception);
            return await EmbedHandler.CreateErrorEmbed("Music, Join", exception.Message);
        }
    }

    public async Task<Embed?> LeaveAsync(IVoiceChannel voiceChannel)
    {
        try
        {
            await _lavaNode.LeaveAsync(voiceChannel);

            // Usuwanie player state z dictionary (usuwanie timera)
            if (_playerStates.TryRemove(voiceChannel.GuildId, out var state))
            {
                state.Dispose();
            }

            await LoggingService.LogMessage("LavaPlayer", $"Left {voiceChannel}");

            return null;
        }
        catch (Exception exception)
        {
            await LoggingService.LogError("LavaPlayer", $"Failed to leave {voiceChannel}", exception);
            return await EmbedHandler.CreateErrorEmbed("Music, Leave", exception.Message);
        }
    }

    public async Task<Embed> PlayAsync(LavaPlayer player, string query)
    {
        var track = await SearchSongAsync(query);
        if (track == null) return await EmbedHandler.CreateErrorEmbed("Music, Play", "Couldn't retrieve the track.");

        var embedBuilder = new EmbedBuilder();

        // Utworzenie linku do miniaturki filmiku (w średniej jakości)
        var videoId = track.Url.Split("watch?v=")[1].Split("&")[0];
        var thumbnailUrl = $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg";

        // Jeśli player coś odtwarza to znaleziony utwór jest dodawany do kolejki
        if (player.Track != null && player.PlayerState is PlayerState.Playing or PlayerState.Paused)
        {
            player.Queue.Enqueue(track);

            embedBuilder.WithTitle("Music")
                .WithDescription(
                    $"[{track.Title}]({track.Url}) has been added to the queue at #{player.Queue.Count + 1}.")
                .WithColor(Color.DarkGreen);

            if (videoId != string.Empty)
            {
                embedBuilder.WithImageUrl(thumbnailUrl);
                return embedBuilder.Build();
            }
        }

        await player.PlayAsync(track);

        // Jeśli jest z poza youtube
        if (videoId == string.Empty)
            return await EmbedHandler.CreateBasicEmbed("Now Playing",
                $"Now playing: [{track.Title}]({track.Url}).", Color.DarkGreen);

        embedBuilder.WithTitle("Now Playing")
            .WithDescription($"Now playing: [{track.Title}]({track.Url}).")
            .WithImageUrl(thumbnailUrl)
            .WithColor(Color.DarkGreen);

        return embedBuilder.Build();
    }

    public async Task<LavaTrack?> SearchSongAsync(string query)
    {
        // Szukanie utworu - jeśli zapytanie nie jest poprawnym linkiem to utwór jest wyszukiwany na youtubie
        var search = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, query)
            : await _lavaNode.SearchYouTubeAsync(query);

        if (search.Status == SearchStatus.NoMatches) return null;

        var track = search.Tracks.FirstOrDefault();
        return track;
    }

    public async Task<Embed> SkipAsync(LavaPlayer player)
    {
        var currentTrack = player.Track;
        if (currentTrack == null)
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Skip",
                "Nothing to skip.", Color.Blue);
        }

        await LoggingService.LogMusicMessage("LavaPlayer", $"Skipped {currentTrack.Title}");

        if (!_playerStates.TryGetValue(player.VoiceChannel.GuildId, out var playerState))
        {
            return await EmbedHandler.CreateErrorEmbed("Music, Skip", "Couldn't retrieve the player state");
        }
        

        if (player.Queue.Count < 1)
        {
            await player.StopAsync();
            playerState.StartIdleTimer();
        }
        else
        {
            await player.SkipAsync();
            playerState.Looped = false;
        }

        return await EmbedHandler.CreateBasicEmbed("Music, Skip",
            $"Skipped [{currentTrack.Title}]({currentTrack.Url}).", Color.Blue);
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
        await LoggingService.LogMusicMessage("LavaPlayer",
            $"Now playing: {arg.Track.Title} in {arg.Player.VoiceChannel}");

        if (!_playerStates.TryGetValue(arg.Player.VoiceChannel.GuildId, out var playerState)) return;

        playerState.StopIdleTimer();
    }

    private async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (args.Reason is not (TrackEndReason.Finished or TrackEndReason.LoadFailed)) return;

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
                playerState.StartIdleTimer();
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

    public async Task<string> ApplySoundEffectAsync(LavaPlayer player, string effect)
    {
        if (player.Track == null)
        {
            return "I'm not playing anything right now.";
        }

        IFilter filter;
        var returnMessage = "Applied effect.";

        switch (effect.ToLower())
        {
            case "rotation":
            case "rotate":
            case "8d":
                filter = new RotationFilter {Hertz = 0.2};
                break;

            case "karaoke":
                filter = new KarokeFilter();
                break;

            case "reset":
            case "default":
            case "off":
                filter = new ChannelMixFilter();
                returnMessage = "Disabled effects";
                break;

            case "oro":
            case "9d":
                filter = new DistortionFilter
                {
                    Offset = 2, Scale = 4, CosOffset = 5, CosScale = 10, SinOffset = 2, SinScale = 4, TanOffset = 2,
                    TanScale = 7
                };
                break;

            case "mono":
            case "1d":
                filter = new ChannelMixFilter
                    {LeftToLeft = 0.5, LeftToRight = 0.5, RightToLeft = 0.5, RightToRight = 0.5};
                break;

            default:
                return "Effect not found";
        }

        await player.ApplyFilterAsync(filter);

        return returnMessage;
    }
}