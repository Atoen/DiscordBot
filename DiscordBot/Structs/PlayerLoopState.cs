using DiscordBot.Services;
using Timer = System.Timers.Timer;

namespace DiscordBot.Structs;

public class PlayerLoopState
{
    public bool Looped
    {
        get => LoopTimes is > 0 or -1;
        set => LoopTimes = value ? -1 : 0;
    }
    public int LoopTimes { get; set; }
    public LavaTrack? LastTrack { get; set; }

    public event EventHandler? TimedOut;
    
    private static readonly TimeSpan IdleTime = TimeSpan.FromMinutes(10);
    private readonly Timer _timer;

    public PlayerLoopState()
    {
        _timer = new Timer(IdleTime.TotalMilliseconds);
        _timer.Elapsed += (_, args) => TimedOut?.Invoke(this, args);
    }

    public void StartIdleTimer()
    {
        _timer.Start();
    }

    public void StopIdleTimer()
    {
        _timer.Stop();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
    
    public void PerformLoop()
    {
        if (LoopTimes > 0)
        {
            LoopTimes--;
        }
    }
}