using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class NPCSpawnData
{
    public NPCType npcType;
    public EntityData entityData;
}

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    [SerializeField] private List<NPCSpawnData> npcSpawnSettings = new();
    private Dictionary<NPCType, GameObject> _activeNPCs = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (DayNightSystem.Instance != null)
        {
            DayNightSystem.Instance.OnDayNightChanged += HandleDayNightChange;
        }
    }

    void OnDestroy()
    {
        if (DayNightSystem.Instance != null)
        {
            DayNightSystem.Instance.OnDayNightChanged -= HandleDayNightChange;
        }
    }

    private void HandleDayNightChange(bool isDay)
    {
        if (isDay) TrySpawnAllNPCs();
    }

    private void TrySpawnAllNPCs()
    {
        var allObjects = MultitileObjectSystem.Instance.GetAllObjects();
        var allBeds = allObjects.OfType<BedObject>().ToList();

        var types = _activeNPCs.Keys.ToList();
        foreach (var type in types)
        {
            if (_activeNPCs[type] == null) _activeNPCs.Remove(type);
        }

        foreach (var spawnData in npcSpawnSettings)
        {
            if (_activeNPCs.ContainsKey(spawnData.npcType)) continue;

            var matchingBeds = allBeds.Where(b => b.BedDef.npcType == spawnData.npcType).ToList();

            if (matchingBeds.Count > 0)
            {
                var selectedBed = matchingBeds[Random.Range(0, matchingBeds.Count)];

                Vector2 spawnPos = new Vector2(
                    selectedBed.Origin.x + selectedBed.Definition.size.x * 0.5f,
                    selectedBed.Origin.y + selectedBed.Definition.size.y + 0.5f
                );

                SpawnNPC(spawnData, spawnPos);
            }
        }
    }

    private void SpawnNPC(NPCSpawnData data, Vector2 spawnPos)
    {
        var go = Instantiate(data.entityData.prefab, spawnPos, Quaternion.identity);

        var entityStats = go.GetComponent<EntityStats>();
        if (entityStats != null) entityStats.Initialize(data.entityData);

        var friendlyAI = go.GetComponent<FriendlyNPCAI>();
        if (friendlyAI != null) friendlyAI.SetHome(spawnPos);

        _activeNPCs[data.npcType] = go;
    }
}