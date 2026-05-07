using System.Collections.Generic;

public static class WorldDataTransfer
{
    public static WorldData Data { get; set; }
    public static WorldData OriginalData { get; set; }
    public static List<WorldGenerator.PendingObjectPlacement> PendingPlacements { get; set; } = new();
}