using UnityEngine;

public class WallPlaceSystem : MonoBehaviour
{
    public static WallPlaceSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryPlace(Vector3Int cell, WallType typeToPlace)
    {
        if (!ChunkManager.Instance.IsChunkLoaded(new Vector2(cell.x, cell.y))) return false;
        if (WorldManager.Instance.GetWall(cell.x, cell.y) != WallType.None) return false;

        WorldManager.Instance.PlaceWall(cell.x, cell.y, typeToPlace);
        return true;
    }
}