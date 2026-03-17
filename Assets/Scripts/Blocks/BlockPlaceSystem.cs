using UnityEngine;

public class BlockPlaceSystem : MonoBehaviour
{
    public static BlockPlaceSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryPlace(Vector3Int cell, BlockType typeToPlace)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        bool hasNeighbor = HasNeighbor(cell);
        sw.Stop();
        Debug.Log($"HasNeighbor: {sw.ElapsedMilliseconds}ms");
        
        if (!hasNeighbor) return false;
        if (WorldManager.Instance.GetBlock(cell.x, cell.y) != BlockType.Air) return false;

        WorldManager.Instance.PlaceBlock(cell.x, cell.y, typeToPlace);
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