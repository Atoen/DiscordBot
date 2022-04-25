namespace DiscordBot.Structs;

public class PlayerStateStruct
{
    public bool Looped
    {
        get => LoopTimes is > 0 or -1;
        set => LoopTimes = value ? -1 : 0;
    }
    
    public int LoopTimes { get; set; }
    public LavaTrack? LastTrack { get; set; }

    public void PerformLoop()
    {
        if (LoopTimes > 0 && LoopTimes != -1)
        {
            LoopTimes--;
        }
    }
}