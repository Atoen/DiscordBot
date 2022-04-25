using Microsoft.Extensions.Configuration;

namespace DiscordBot.Modules;

[Name("Helping")]
[Summary("Helpful commands")]
public class HelpModule : ModuleBase<SocketCommandContext>
{
    private readonly CommandService _service;
    private readonly IConfigurationRoot _config;
    
    public HelpModule(CommandService service, IConfigurationRoot config)
    {
        _service = service;
        _config = config;
    }
    
    [Command("help")]
    [Summary("Displays all commands")]
    public async Task HelpAsync()
    {
        var builder = new EmbedBuilder()
        {
            Color = Color.Teal,
            Description = "These are the commands you can use"
        };

        foreach (var module in _service.Modules)
        {
            string? description = null;
            foreach (var commandInfo in module.Commands)
            {
                var result = await commandInfo.CheckPreconditionsAsync(Context);
                if (result.IsSuccess)
                    description += $"{_config["prefix"]}{commandInfo.Aliases[0]}\n";
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                builder.AddField(x =>
                {
                    x.Name = module.Name;
                    x.Value = description;
                    x.IsInline = false;
                });
            }
        }

        await ReplyAsync("", false, builder.Build());
    }
    
    [Command("help")]
    [Summary("Displays specific command help")]
    public async Task HelpAsync(string command)
    {
        var result = _service.Search(Context, command);

        if (!result.IsSuccess)
        {
            await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
            return;
        }

        var builder = new EmbedBuilder()
        {
            Color = Color.Teal,
            Description = $"Here are some commands like **{command}**"
        };

        foreach (var match in result.Commands)
        {
            var commandInfo = match.Command;

            builder.AddField(fieldBuilder =>
            {
                fieldBuilder.Name = string.Join(", ", commandInfo.Aliases);
                fieldBuilder.Value = $"Parameters: {string.Join(", ", commandInfo.Parameters.Select(info => info.Name))}\n" +
                          $"Summary: {commandInfo.Summary}";
                fieldBuilder.IsInline = false;
            });
        }

        await ReplyAsync("", false, builder.Build());
    }
    
    [Command("Oro")]
    [Summary("Oro")]
    public async Task OroAsync()
    {
        await ReplyAsync("Oro deadass");
    }
}