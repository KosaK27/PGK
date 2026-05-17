using UnityEngine;
using UnityEngine.InputSystem;

public class BossSpawner : MonoBehaviour
{
    public static BossSpawner Instance { get; private set; }

    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private float spawnOffsetX = 8f;
    [SerializeField] private float spawnOffsetY = 4f;

    private GameObject _activeBoss;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Keyboard.current[Key.B].wasPressedThisFrame)
            SpawnBoss();
    }

    public void SpawnBoss()
    {
        SpawnBossFromItem(bossPrefab);
    }

    public void SpawnBossFromItem(GameObject prefab)
    {
        if (_activeBoss != null) return;
        if (prefab == null) return;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        Vector3 spawnPos = playerObj.transform.position + new Vector3(spawnOffsetX, spawnOffsetY, 0f);
        _activeBoss = Instantiate(prefab, spawnPos, Quaternion.identity);

        MusicManager.Instance?.PlayBoss();

        var stats = _activeBoss.GetComponent<EntityStats>();
        if (stats != null)
            stats.OnDeath += () =>
            {
                _activeBoss = null;
                MusicManager.Instance?.PlayNormal();
            };

        var playerStats = playerObj.GetComponent<PlayerStats>();
        if (playerStats != null)
            playerStats.OnHealthChanged += OnPlayerHealthChanged;
    }

    private void OnPlayerHealthChanged(int current, int max)
    {
        if (current <= 0 && _activeBoss != null)
        {
            Destroy(_activeBoss);
            _activeBoss = null;
            MusicManager.Instance?.PlayNormal();
        }
    }
}