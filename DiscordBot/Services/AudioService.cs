using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using SharpLink;
using SharpLink.Enums;

namespace DiscordBot.Services;

public class AudioService
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();
    private readonly LavalinkManager _lavalink;
    
    public async Task JoinAudio(IGuild guild, IVoiceChannel target)
    {
        IAudioClient client;
        if (_connectedChannels.TryGetValue(guild.Id, out client))
        {
            return;
        }

        if (target.Guild.Id != guild.Id)
        {
            return;
        }

        var audioClient = await target.ConnectAsync(selfDeaf: true);
        _connectedChannels.TryAdd(guild.Id, audioClient);
    }
    
    public async Task LeaveAudio(IGuild guild)
    {
        IAudioClient client;
        if (_connectedChannels.TryGetValue(guild.Id, out client))
        {
            await _connectedChannels[guild.Id].StopAsync();
            _connectedChannels.TryRemove(guild.Id, out client);
        }
    }
    
}