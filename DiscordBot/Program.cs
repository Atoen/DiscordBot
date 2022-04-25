namespace DiscordBot;

public static class Program
{
    public static Task Main()
    {
        Console.Title = "DiscordBot";
        return new Startup().MainAsync();
    }
}
