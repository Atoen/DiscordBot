using Discord;
using Discord.Commands;
using DiscordBot.Services;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using System.Linq;
using Victoria.Responses.Search;


namespace DiscordBot.Modules;

public sealed class MusicModule : ModuleBase<SocketCommandContext>
{
    private readonly LavaNode _lavaNode;

    public MusicModule(LavaNode lavaNode) =>
    _lavaNode = lavaNode;

    [Command("J")]
    public async Task JoinAsync()
    {
        if (_lavaNode.HasPlayer(Context.Guild))
        {
            await ReplyAsync("Already connected to voice channel");
            return;
        }

        var voiceState = Context.User as IVoiceState;
        if (voiceState?.VoiceChannel == null)
        {
            await ReplyAsync("You must be connected to a voice channel");
            return;
        }

        try
        {
            await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("play")]
    public async Task Play([Remainder] string query)
    {
        var voiceState = Context.User as IVoiceState;
        if (voiceState?.VoiceChannel == null)
        {
            await ReplyAsync("You must be connected to a voice channel");
            return;
        }
        
        var player = _lavaNode.GetPlayer(Context.Guild);

        var search = Uri.IsWellFormedUriString(query, UriKind.Absolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, query)
            : await _lavaNode.SearchYouTubeAsync(query);
        
        //If we couldn't find anything, tell the user.
        if (search.Status == SearchStatus.NoMatches)
        {
            await ReplyAsync("Nothing found");
            return;
        }
        
        var track = search.Tracks.FirstOrDefault();

        //If the Bot is already playing music, or if it is paused but still has music in the playlist, Add the requested track to the queue.
        if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
        {
            player.Queue.Enqueue(track);
            await ReplyAsync($"{track?.Title} has been added to queue.");
            return;
        }

        //Player was not playing anything, so lets play the requested track.
        await player.PlayAsync(track);
        await ReplyAsync($"Now Playing: {track?.Title}\nUrl: {track.Url}");
    }
}