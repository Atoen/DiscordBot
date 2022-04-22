namespace DiscordBot;

public static class Program
{
    public static Task Main()
    { 
        return new Startup().MainAsync();
    }
}
