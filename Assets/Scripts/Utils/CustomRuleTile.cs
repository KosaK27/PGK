using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "CustomRuleTile", menuName = "Tiles/Custom Rule Tile")]
public class CustomRuleTile : RuleTile<CustomRuleTile.Neighbor>
{
    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int None = 3;
        public const int Any = 4;
    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        switch (neighbor)
        {
            case Neighbor.This: return tile == this;
            case Neighbor.NotThis: return tile != this;
            case Neighbor.None: return tile == null;
            case Neighbor.Any: return tile != null;
        }
        return base.RuleMatch(neighbor, tile);
    }
}