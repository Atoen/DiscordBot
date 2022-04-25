namespace DiscordBot.Handlers;

public static class EmbedHandler
{
    public static async Task<Embed> CreateBasicEmbed(string title, string description, Color color)
    {
        var embed = await Task.FromResult(new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .Build());
        
        return embed;
    }
    
    public static async Task<Embed> CreateErrorEmbed(string source, string error)
    {
        var embed = await Task.FromResult(new EmbedBuilder()
            .WithTitle($"ERROR OCCURED FROM - {source}")
            .WithDescription($"**Error Details**: \n{error}")
            .WithColor(Color.DarkRed)
            .Build());
        
        return embed;
    }
}