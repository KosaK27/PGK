using System.Collections.Generic;

public static class WorldRLE
{
    public static List<RLEEntry> EncodeBlocks(WorldData world)
    {
        var result = new List<RLEEntry>();
        var raw = world.GetRawData();
        if (raw.Length == 0) return result;
        int current = (int)raw[0];
        int count = 1;
        for (int i = 1; i < raw.Length; i++)
        {
            int v = (int)raw[i];
            if (v == current) { count++; continue; }
            result.Add(new RLEEntry { type = current, count = count });
            current = v;
            count = 1;
        }
        result.Add(new RLEEntry { type = current, count = count });
        return result;
    }

    public static List<RLEEntry> EncodeWalls(WorldData world)
    {
        var result = new List<RLEEntry>();
        var raw = world.GetRawWallData();
        if (raw.Length == 0) return result;
        int current = (int)raw[0];
        int count = 1;
        for (int i = 1; i < raw.Length; i++)
        {
            int v = (int)raw[i];
            if (v == current) { count++; continue; }
            result.Add(new RLEEntry { type = current, count = count });
            current = v;
            count = 1;
        }
        result.Add(new RLEEntry { type = current, count = count });
        return result;
    }

    public static List<RLEEntry> EncodeLiquids(WorldData world)
    {
        var result = new List<RLEEntry>();
        var raw = world.GetRawLiquidData();
        if (raw.Length == 0) return result;
        int current = raw[0];
        int count = 1;
        for (int i = 1; i < raw.Length; i++)
        {
            int v = raw[i];
            if (v == current) { count++; continue; }
            result.Add(new RLEEntry { type = current, count = count });
            current = v;
            count = 1;
        }
        result.Add(new RLEEntry { type = current, count = count });
        return result;
    }

    public static void DecodeInto(WorldData world, List<RLEEntry> blockRLE, List<RLEEntry> wallRLE, List<RLEEntry> liquidRLE)
    {
        int idx = 0;
        foreach (var entry in blockRLE)
            for (int i = 0; i < entry.count && idx < world.Width * world.Height; i++, idx++)
            {
                int x = idx % world.Width;
                int y = idx / world.Width;
                world.GetRawData()[idx] = (BlockType)entry.type;
            }

        idx = 0;
        foreach (var entry in wallRLE)
            for (int i = 0; i < entry.count && idx < world.Width * world.Height; i++, idx++)
                world.GetRawWallData()[idx] = (WallType)entry.type;

        idx = 0;
        foreach (var entry in liquidRLE)
            for (int i = 0; i < entry.count && idx < world.Width * world.Height; i++, idx++)
            {
                byte level = (byte)entry.type;
                world.GetRawLiquidData()[idx] = level;
                if (level > 0)
                    world.GetRawData()[idx] = BlockType.Water;
            }
    }
}