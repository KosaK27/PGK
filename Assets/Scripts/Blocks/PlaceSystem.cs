using UnityEngine;

public class PlaceSystem : MonoBehaviour
{
    public static PlaceSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryPlace(Vector3Int cell, BlockType blockType)
    {
        if (!ChunkManager.Instance.IsChunkLoaded(new Vector2(cell.x, cell.y))) return false;
        if (!HasNeighbor(cell)) return false;
        if (WorldManager.Instance.GetBlock(cell.x, cell.y) != BlockType.Air) return false;

        WorldManager.Instance.PlaceBlock(cell.x, cell.y, blockType);
        return true;
    }

    public bool TryPlace(Vector3Int cell, WallType wallType)
    {
        if (!ChunkManager.Instance.IsChunkLoaded(new Vector2(cell.x, cell.y))) return false;
        if (WorldManager.Instance.GetWall(cell.x, cell.y) != WallType.None) return false;

        WorldManager.Instance.PlaceWall(cell.x, cell.y, wallType);
        return true;
    }

    private bool HasNeighbor(Vector3Int cell)
    {
        Vector3Int[] neighbors = {
            cell + Vector3Int.up,
            cell + Vector3Int.down,
            cell + Vector3Int.left,
            cell + Vector3Int.right,
        };

        foreach (var neighbor in neighbors)
        {
            if (WorldManager.Instance.GetBlock(neighbor.x, neighbor.y) != BlockType.Air)
                return true;
        }

        return false;
    }
}