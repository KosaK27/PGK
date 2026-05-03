using System.Collections.Generic;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    public static EntitySpawner Instance { get; private set; }

    [Header("Config")]
    public List<EntityData> spawnTable = new();
    public int maxEntitiesCap = 20;
    public float baseSpawnInterval = 2f;
    public float spawnMinDist = 14f;
    public float spawnMaxDist = 26f;
    public LayerMask groundLayer;

    private float _spawnTimer;
    private Transform _player;
    private Camera _mainCam;
    private List<GameObject> _activeEntities = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _mainCam = Camera.main;
    }

    void Update()
    {
        if (_player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
            return;
        }

        _activeEntities.RemoveAll(e => e == null);
        if (IsBossAlive() || _activeEntities.Count >= maxEntitiesCap) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            TrySpawn();
            _spawnTimer = baseSpawnInterval;
        }
    }

    private void TrySpawn()
    {
        bool isNight = false;
        if (DayNightSystem.Instance != null)
        {
            float t = DayNightSystem.Instance.timeOfDay;
            isNight = (t < 0.22f || t > 0.78f);
        }

        float side = Random.value > 0.5f ? 1f : -1f;
        float xOffset = side * Random.Range(spawnMinDist, spawnMaxDist);
        int worldX = Mathf.RoundToInt(_player.position.x + xOffset);
        int surfaceY = WorldManager.Instance.GetSurfaceWorldY(worldX);

        bool playerInCave = _player.position.y < surfaceY - 5;
        bool attemptCave = playerInCave ? (Random.value > 0.2f) : (Random.value > 0.8f);

        Vector3 spawnPos = Vector3.zero;
        bool foundValidSpot = false;

        if (attemptCave)
        {
            float caveSearchY = surfaceY - Random.Range(15, 40);
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(worldX + 0.5f, caveSearchY), Vector2.down, 20f, groundLayer);

            if (hit.collider != null)
            {
                spawnPos = new Vector3(hit.point.x, hit.point.y + 1.5f, 0);
                foundValidSpot = true;
            }
        }
        else
        {
            spawnPos = new Vector3(worldX + 0.5f, surfaceY + 1.5f, 0);
            foundValidSpot = true;
        }

        if (!foundValidSpot) return;

        Vector3 screenPoint = _mainCam.WorldToViewportPoint(spawnPos);
        if (screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1) return;

        EntityData selected = GetValidEntity(isNight, attemptCave);
        if (selected != null && selected.prefab != null)
        {
            Instantiate(selected.prefab, spawnPos, Quaternion.identity);
            Debug.Log($"Zrespiono {selected.displayName} na {(attemptCave ? "w jaskini" : "powierzchni")}");
        }
    }

    private EntityData GetValidEntity(bool isNight, bool isCave)
    {
        List<EntityData> pool = new();
        float totalWeight = 0;

        foreach (var d in spawnTable)
        {
            if (d == null) continue;
            bool timeOk = (isNight && d.spawnInNight) || (!isNight && d.spawnInDay);
            bool locOk = (isCave && d.spawnInCaves) || (!isCave && d.spawnOnSurface);

            if (timeOk && locOk)
            {
                pool.Add(d);
                totalWeight += d.spawnWeight;
            }
        }

        if (pool.Count == 0) return null;
        float r = Random.Range(0f, totalWeight);
        float acc = 0;
        foreach (var d in pool)
        {
            acc += d.spawnWeight;
            if (r <= acc) return d;
        }
        return pool[pool.Count - 1];
    }

    private bool IsBossAlive()
    {
        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        return boss != null && boss.activeInHierarchy;
    }
}