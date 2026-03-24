using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    public static PlayerSpawner Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnPlayer();
    }

    public Vector3 GetSpawnPosition()
    {
        var world = WorldManager.Instance;
        int worldX = 0;
        int surfaceY = world.GetSurfaceWorldY(worldX);
        return new Vector3(worldX + 1f, surfaceY + 2f, 0f);
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null) { Debug.LogWarning("Brak Player Prefab!"); return; }

        var spawnPos = GetSpawnPosition();
        var player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        var cam = Camera.main.GetComponent<CameraFollow>();
        if (cam != null) cam.target = player.transform;

        ChunkManager.Instance.SetPlayer(player.transform);
    }
}