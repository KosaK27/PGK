using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    private bool _spawned = false;

    void LateUpdate()
    {
        if (_spawned) return;
        _spawned = true;
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        var world = WorldManager.Instance;
        int worldX = 0;

        int surfaceY = world.GetSurfaceWorldY(worldX);
        var spawnPos = new Vector3(worldX + 0.5f, surfaceY + 1.5f, 0f);

        if (playerPrefab == null) { Debug.LogWarning("Brak Player Prefab!"); return; }

        var player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        var cam = Camera.main.GetComponent<CameraFollow>();
        if (cam != null) cam.target = player.transform;

        ChunkManager.Instance.SetPlayer(player.transform);
    }
}