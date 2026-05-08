using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1)]
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private ItemRegistry _itemRegistry;
    [SerializeField] private MultitileObjectRegistry _multitileRegistry;
    [SerializeField] private ItemDefinition[] _startItems;

    private bool _saved = false;
    private GameObject _player;
    private WorldManager _worldManager;

    void Start()
    {
        _worldManager = WorldManager.Instance;
        _itemRegistry.Initialize();
        _multitileRegistry.Initialize();
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        yield return null;

        _player = PlayerSpawner.Instance.SpawnPlayer();

        Rigidbody2D rb = null;
        if (_player != null)
        {
            rb = _player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;
        }

        yield return new WaitUntil(() => ChunkManager.Instance.IsChunkLoaded(_player.transform.position));

        var sm = SaveManager.Instance;

        if (sm == null)
        {
            if (rb != null) rb.simulated = true;
            yield break;
        }

        var worldSave = sm.SelectedWorld;
        var charSave = sm.SelectedCharacter;
        bool isNewSession = charSave == null || charSave.inventorySlots.Count == 0;

        if (worldSave != null && worldSave.blockDiffs != null && worldSave.blockDiffs.Count > 0)
        {
            sm.RestoreWorldState(worldSave, _worldManager.Data);
            ChunkManager.Instance.RebuildAll(_worldManager.OffsetX, _worldManager.OffsetY);
            yield return null;
            RestoreMultitileObjects(worldSave);
        }
        else
        {
            PlacePendingObjects();
        }

        yield return null;

        if (charSave != null)
        {
            Vector3 fallback = PlayerSpawner.Instance != null ? PlayerSpawner.Instance.GetSpawnPosition() : _player.transform.position;
            sm.RestoreCharacterState(charSave, _player, _itemRegistry, worldSave?.id ?? "", fallback);

            yield return new WaitUntil(() => ChunkManager.Instance.IsChunkLoaded(_player.transform.position));
            ChunkManager.Instance.RebuildAll(_worldManager.OffsetX, _worldManager.OffsetY);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        if (isNewSession && _startItems != null)
        {
            foreach (var item in _startItems)
                if (item != null) InventorySystem.Instance.AddItem(new ItemStack(item, 1));
        }

        if (rb != null) rb.simulated = true;
    }

    void PlacePendingObjects()
    {
        var pending = WorldDataTransfer.PendingPlacements;
        if (pending == null) return;
        foreach (var p in pending)
        {
            var obj = MultitileObjectSystem.Instance.PlaceDirect(p.worldPos, p.definition);
            if (p.fillWithLoot && obj is ChestObject chest)
                chest.FillWithLoot();
        }
        WorldDataTransfer.PendingPlacements = null;
    }

    void OnApplicationQuit() => AutoSave();
    void OnDestroy() => AutoSave();

    void AutoSave()
    {
        if (_saved) return;
        _saved = true;

        var sm = SaveManager.Instance;
        if (sm == null) return;

        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player");

        var charSave = sm.SelectedCharacter;
        var worldSave = sm.SelectedWorld;

        if (charSave != null && _player != null)
        {
            sm.CaptureCharacterState(charSave, _player, worldSave?.id ?? "");
            sm.SaveCharacter(charSave);
        }

        if (worldSave != null && _worldManager != null)
        {
            sm.CaptureWorldState(worldSave, _worldManager.Data, _worldManager.OriginalData);
            CaptureMultitileObjects(worldSave);
            sm.SaveWorld(worldSave);
        }
    }

    void CaptureMultitileObjects(WorldSaveData worldSave)
    {
        worldSave.multitileObjects.Clear();
        foreach (var obj in MultitileObjectSystem.Instance.GetAllObjects())
        {
            string defName;
            int openDir = 0;

            if (obj is DoorObject doorObj)
            {
                defName = string.IsNullOrEmpty(doorObj.DoorDefinition.sourceName)
                    ? doorObj.DoorDefinition.name
                    : doorObj.DoorDefinition.sourceName;
                openDir = (int)doorObj.DoorDefinition.openDirection;
            }
            else
            {
                defName = obj.Definition.name;
            }

            var entry = new MultitileObjectSave
            {
                definitionName = defName,
                originX = obj.Origin.x,
                originY = obj.Origin.y,
                openDirection = openDir
            };

            if (obj is ChestObject chest)
            {
                for (int i = 0; i < chest.Container.SlotCount; i++)
                {
                    var slot = chest.Container.GetSlot(i);
                    if (slot == null || slot.IsEmpty) continue;
                    entry.containerSlots.Add(new ItemSlotSave { slotIndex = i, itemId = slot.item.itemId, amount = slot.amount });
                }
            }

            worldSave.multitileObjects.Add(entry);
        }
    }

    void RestoreMultitileObjects(WorldSaveData worldSave)
    {
        foreach (var saved in worldSave.multitileObjects)
        {
            var def = _multitileRegistry.Get(saved.definitionName);
            if (def == null) { Debug.LogWarning($"[GameBootstrap] Unknown definition '{saved.definitionName}'"); continue; }

            if (def is DoorDefinition doorDef)
            {
                var cloned = Instantiate(doorDef);
                cloned.sourceName = doorDef.name;
                cloned.openDirection = (DoorOpenDirection)saved.openDirection;
                def = cloned;
            }

            var origin = new Vector2Int(saved.originX, saved.originY);
            MultitileObjectSystem.Instance.PlaceDirect(origin, def);

            if (saved.containerSlots.Count == 0) continue;
            var placed = MultitileObjectSystem.Instance.Get(origin);
            if (placed is not ChestObject chest) continue;
            foreach (var slot in saved.containerSlots)
            {
                var item = _itemRegistry.Get(slot.itemId);
                if (item == null) continue;
                chest.Container.SetSlot(slot.slotIndex, new ItemStack(item, slot.amount));
            }
        }
    }

    public void OnReturnToMenu()
    {
        AutoSave();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}