using System.Collections.Generic;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    public static EntitySpawner Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private List<EntityData> spawnTable = new();
    [SerializeField] private int maxEntities = 10;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float spawnMinDist = 12f;
    [SerializeField] private float spawnMaxDist = 20f;

    private float _spawnTimer;
    private Transform _player;
    private List<GameObject> _activeEntities = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
            else return;
        }

        _activeEntities.RemoveAll(e => e == null);

        if (spawnTable.Count == 0) return;
        if (_activeEntities.Count >= maxEntities) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = spawnInterval;
            TrySpawn();
        }
    }

    private void TrySpawn()
    {
        var data = PickWeighted();
        if (data?.prefab == null) return;

        float side = Random.value > 0.5f ? 1f : -1f;
        float dist = Random.Range(spawnMinDist, spawnMaxDist);
        var spawnPos = _player.position + new Vector3(side * dist, 0, 0);

        int worldX = Mathf.RoundToInt(spawnPos.x);
        int surfaceY = WorldManager.Instance.GetSurfaceWorldY(worldX);
        spawnPos = new Vector3(worldX + 0.5f, surfaceY + 1.5f, 0);

        if (!ChunkManager.Instance.IsChunkLoaded(spawnPos)) return;

        var go = Instantiate(data.prefab, spawnPos, Quaternion.identity);
        var entityStats = go.GetComponent<EntityStats>();
        if (entityStats != null) entityStats.Initialize(data);

        _activeEntities.Add(go);
    }

    private EntityData PickWeighted()
    {
        float total = 0f;
        foreach (var d in spawnTable) total += d.spawnWeight;

        float r = Random.Range(0f, total);
        float acc = 0f;
        foreach (var d in spawnTable)
        {
            acc += d.spawnWeight;
            if (r <= acc) return d;
        }
        return spawnTable[^1];
    }
}