public class WorldGenerationProgress
{
    public float Progress { get; private set; }
    public string CurrentStep { get; private set; }

    public void Report(float progress, string step)
    {
        Progress = progress;
        CurrentStep = step;
    }
}