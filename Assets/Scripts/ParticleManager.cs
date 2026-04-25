using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [SerializeField] private ParticleSystem blockBreakPrefab;
    [SerializeField] private ParticleSystem slashPrefab;
    [SerializeField] private ParticleSystem hitPrefab;
    [SerializeField] private ParticleSystem dustPrefab;
    [SerializeField] private BlockRegistry blockRegistry;
    [SerializeField] private WallRegistry wallRegistry;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void EmitBlockBreak(Vector3Int cell, BlockType blockType)
    {
        var data = blockRegistry.Get(blockType);
        Color color = data != null ? data.particleColor : Color.gray;
        Vector3 worldPos = WorldManager.Instance.CellToWorld(cell.x, cell.y) + new Vector3(0.5f, 0.5f, 0f);
        SpawnWithColor(blockBreakPrefab, worldPos, Quaternion.identity, color);
    }

    public void EmitWallBreak(Vector3Int cell, WallType wallType)
    {
        var data = wallRegistry.Get(wallType);
        Color color = data != null ? data.particleColor : Color.gray;
        Vector3 worldPos = WorldManager.Instance.CellToWorld(cell.x, cell.y) + new Vector3(0.5f, 0.5f, 0f);
        SpawnWithColor(blockBreakPrefab, worldPos, Quaternion.identity, color);
    }

    public void EmitSlash(Vector3 position, float angle)
    {
        SpawnOneShot(slashPrefab, position, Quaternion.Euler(0f, 0f, angle));
    }

    public void EmitHit(Vector3 position)
    {
        SpawnOneShot(hitPrefab, position, Quaternion.identity);
    }

    public void EmitDust(Vector3 position, BlockType blockType)
    {
        var data = blockRegistry.Get(blockType);
        Color color = data != null ? data.particleColor : Color.gray;
        SpawnWithColor(dustPrefab, position, Quaternion.identity, color);
    }

    private void SpawnOneShot(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        var ps = Instantiate(prefab, position, rotation);
        ps.Play();
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    private void SpawnWithColor(ParticleSystem prefab, Vector3 position, Quaternion rotation, Color color)
    {
        if (prefab == null) return;
        var ps = Instantiate(prefab, position, rotation);
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color * 0.85f, color);
        ps.Play();
        Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax);
    }
}