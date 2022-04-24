﻿using System.Collections.Concurrent;
using System.Text;
using Discord;
using Discord.Audio;
using Discord.Commands;
using DiscordBot.Handlers;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace DiscordBot.Services;

public class AudioService
{
    private readonly LavaNode _lavaNode;
    
    public AudioService(LavaNode lavaNode) => _lavaNode = lavaNode;
    
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

        if (voiceState?.VoiceChannel is null)
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Join",
                "You must be connected to a voice channel.", Color.DarkRed);
        }

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
            Console.WriteLine(exception);
            return await EmbedHandler.CreateErrorEmbed("Music, Leave", exception.Message);
        }
    }

    /// <summary>
    /// Opuszcza obecny kanał głosowy
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
        
        var search = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, query)
            : await _lavaNode.SearchYouTubeAsync(query);

        if (search.Status == SearchStatus.NoMatches)
        {
            return await EmbedHandler.CreateBasicEmbed("Music, Play",
                "Couldn't find the requested video/song.", Color.DarkGreen);
        }

        var track = search.Tracks.FirstOrDefault();

        if (track == null)
        {
            return await EmbedHandler.CreateErrorEmbed("Music, Play", "Couldn't retrieve the track");
        }

        if (player.Track != null && player.PlayerState is PlayerState.Playing or PlayerState.Paused)
        {
            player.Queue.Enqueue(track);

            return await EmbedHandler.CreateBasicEmbed("Music", 
                $"{track?.Title} has been added to the queue at #{player.Queue.Count + 1}", Color.DarkGreen);
        }

        await player.PlayAsync(track);

        // Jeśli jest z poza youtube
        if (!track!.Url.Contains("watch?v="))
            return await EmbedHandler.CreateBasicEmbed("Now Playing",
                $"Now playing: [{track?.Title}]({track?.Url})", Color.DarkGreen);
        
        // Utworzenie link do miniaturki filmiku (w średniej jakości)
        var videoId = track?.Url.Split("watch?v=")[1].Split("&")[0];
        var thumbnailUrl = $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg";

        var embed = await Task.Run(() => new EmbedBuilder()
            .WithTitle("Now Playing")
            .WithDescription($"Now playing: [{track?.Title}]({track?.Url})")
            .WithImageUrl(thumbnailUrl)
            .WithColor(Color.DarkGreen)
            .Build());
            
        return embed;
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
            return await EmbedHandler.CreateBasicEmbed("Music, List", 
                "I'm not connected to a voice channel.\n Queue is empty.", Color.DarkRed);
        }

        var player = _lavaNode.GetPlayer(guild);
        var descriptionBuilder = new StringBuilder();

        if (player.PlayerState is not (PlayerState.Playing or PlayerState.Paused))
        {
            return await EmbedHandler.CreateBasicEmbed("Music, List", 
                "Player isn't playing anything right now.", Color.Blue);
        }

        if (player.Queue.Count < 1 && player.Track != null)
        {
            return await EmbedHandler.CreateBasicEmbed("Music, List", 
                $"Now playing: {player.Track.Title}", Color.Blue);
        }

        var trackNum = 2;
        foreach (var track in player.Queue)
        {
            descriptionBuilder.Append($"{trackNum}: [{track.Title}]({track.Url})\n");
            trackNum++;
        }
        
        return await EmbedHandler.CreateBasicEmbed("Music Playlist", 
            $"Now playing: [[{player.Track?.Title}]({player.Track?.Url}) \n{descriptionBuilder}", Color.Blue);
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
                    $"Nothing to skip.", Color.Blue);
            }
            
            return await EmbedHandler.CreateBasicEmbed("Music, Skip", 
                $"Skipped {currentTrack.Title}", Color.Blue);
        }

        await player.SkipAsync();
        
        return await EmbedHandler.CreateBasicEmbed("Music, Skip", 
            $"Skipped {currentTrack.Title}", Color.Blue);
    }

    /// <summary>
    /// Zmienia poziom głośności playera
    /// </summary>
    /// <param name="context"> Kontekst komendy z informacjami </param>
    /// <param name="volume"> nowy poziom głośności w procentach </param>
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
            return "I'm not connected to voice channel.";
        }

        var player = _lavaNode.GetPlayer(guild);
        await player.UpdateVolumeAsync(volume);
        return $"Volume set to {volume}.";
    }
    
    public static async Task TrackEnded(TrackEndedEventArgs args)
    {
        if (args.Reason is not (TrackEndReason.Finished or TrackEndReason.LoadFailed))
        {
            return;
        }

        if (!args.Player.Queue.TryDequeue(out var queueable))
        {
            return;
        }

        if (queueable is not { } track)
        {
            await args.Player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
            return;
        }

        await args.Player.PlayAsync(track);
        var embed = await EmbedHandler.CreateBasicEmbed("Now Playing", 
            $"Now playing: [{track?.Title}]({track?.Url})", Color.DarkGreen);
        
        await args.Player.TextChannel.SendMessageAsync(embed: embed);
    }

























}