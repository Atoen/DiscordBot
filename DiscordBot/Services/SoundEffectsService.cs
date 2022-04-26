using Victoria.Filters;

namespace DiscordBot.Services;

public class SoundEffectsService
{
    private readonly LavaNode _lavaNode;

    public SoundEffectsService(LavaNode lavaNode) => _lavaNode = lavaNode;

    public async Task<string> ApplySoundEffectAsync(SocketCommandContext context, string effect)
    {
        var guild = context.Guild;
        var voiceState = context.User as IVoiceState;

        if (voiceState?.VoiceChannel == null)
        {
            return "You must be connected to a voice channel.";
        }

        if (!_lavaNode.HasPlayer(guild))
        {
            return "I'm not connected to voice channel.";
        }

        var player = _lavaNode.GetPlayer(guild);

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