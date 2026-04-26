using UnityEngine;
using UnityEngine.InputSystem;

public class BossSpawner : MonoBehaviour
{
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private float spawnOffsetX = 8f;
    [SerializeField] private float spawnOffsetY = 4f;

    private GameObject _activeBoss;

    void Update()
    {
        if (Keyboard.current[Key.B].wasPressedThisFrame)
            SpawnBoss();
    }

    private void SpawnBoss()
    {
        if (_activeBoss != null) return;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        Vector3 spawnPos = playerObj.transform.position + new Vector3(spawnOffsetX, spawnOffsetY, 0f);
        _activeBoss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

        var stats = _activeBoss.GetComponent<EntityStats>();
        if (stats != null)
            stats.OnDeath += () => _activeBoss = null;

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
        }
    }
}