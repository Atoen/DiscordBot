namespace DiscordBot.Structs;

public class PlayerStateStruct
{
    private static readonly TimeSpan TimeOut = TimeSpan.FromMinutes(10);
    private readonly System.Timers.Timer _timer;
    public event EventHandler? TimedOut;
    
    public bool Looped
    {
        get => LoopTimes is > 0 or -1;
        set => LoopTimes = value ? -1 : 0;
    }
    public int LoopTimes { get; set; }
    public LavaTrack? LastTrack { get; set; }
    public IVoiceChannel VoiceChannel { get; }
    
    public PlayerStateStruct(IVoiceChannel voiceChannel)
    {
        VoiceChannel = voiceChannel;
        _timer = new System.Timers.Timer(TimeOut.TotalMilliseconds);
        
        _timer.Start();
        _timer.Elapsed += (sender, args) => TimedOut?.Invoke(sender, args);
    }

    // Restartowanie timera odłączania
    public void KeepConnected()
    {
        _timer.Enabled = false;
        _timer.Stop();
        _timer.Start();
        _timer.Enabled = true;
    }

    public void PerformLoop()
    {
        if (LoopTimes > 0)
        {
            LoopTimes--;
        }
    }
}