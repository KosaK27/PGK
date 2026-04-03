using UnityEngine;

public static class BlockColorMapper
{
    private static readonly Color Air = new(0.08f, 0.08f, 0.15f, 1f);
    private static readonly Color Dirt = new(0.55f, 0.35f, 0.15f, 1f);
    private static readonly Color Stone = new(0.45f, 0.45f, 0.45f, 1f);
    private static readonly Color Sand = new(0.87f, 0.78f, 0.45f, 1f);
    private static readonly Color Grass = new(0.25f, 0.65f, 0.20f, 1f);
    private static readonly Color Copper = new(0.80f, 0.45f, 0.20f, 1f);
    private static readonly Color Iron = new(0.75f, 0.75f, 0.80f, 1f);
    private static readonly Color Coal = new(0.20f, 0.20f, 0.20f, 1f);

    public static Color Get(BlockType block) => block switch
    {
        BlockType.Dirt => Dirt,
        BlockType.Stone => Stone,
        BlockType.Sand => Sand,
        BlockType.Grass => Grass,
        BlockType.Snow => Color.white,
        BlockType.CopperOre => Copper,
        BlockType.IronOre => Iron,
        BlockType.CoalOre => Coal,
        _ => Air,
    };
}