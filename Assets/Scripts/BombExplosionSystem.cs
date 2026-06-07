using UnityEngine;

public class BombExplosionSystem : MonoBehaviour
{
    public static BombExplosionSystem Instance { get; private set; }

    [SerializeField] private BlockRegistry blockRegistry;
    [SerializeField] private ItemRegistry itemRegistry;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Explode(Vector3 worldPos, float radius, int playerDamage, int entityDamage, float knockbackForce)
    {
        DestroyBlocks(worldPos, radius);
        DamageEntities(worldPos, radius, playerDamage, entityDamage, knockbackForce);
        ParticleManager.Instance?.EmitBlockBreak(
            WorldManager.Instance.WorldToCell(worldPos),
            BlockType.Air);
        BlockAudioManager.Instance?.PlayExplosion();
    }

    private void DestroyBlocks(Vector3 worldPos, float radius)
    {
        int radiusCells = Mathf.CeilToInt(radius);
        Vector3Int center = WorldManager.Instance.WorldToCell(worldPos);

        for (int x = -radiusCells; x <= radiusCells; x++)
        for (int y = -radiusCells; y <= radiusCells; y++)
        {
            var cell = new Vector3Int(center.x + x, center.y + y, 0);
            Vector3 cellWorld = WorldManager.Instance.CellToWorld(cell.x, cell.y) + new Vector3(0.5f, 0.5f);

            if (Vector2.Distance(worldPos, cellWorld) > radius) continue;

            var cellV2 = new Vector2Int(cell.x, cell.y);

            if (MultitileObjectSystem.Instance.IsOccupied(cellV2))
            {
                var obj = MultitileObjectSystem.Instance.Get(cellV2);
                if (obj == null) continue;
                if (IsChestWithItems(obj)) continue;
                TryDestroyMultitile(obj);
                continue;
            }

            if (MultitileObjectSystem.Instance.IsSupporting(cellV2))
            {
                var supported = GetObjectSupportedBy(cellV2);
                if (supported != null && !IsChestWithItems(supported)) continue;
                if (supported != null && IsChestWithItems(supported)) continue;
            }

            var blockType = WorldManager.Instance.GetBlock(cell.x, cell.y);
            if (blockType == BlockType.Air) continue;

            var data = blockRegistry.Get(blockType);
            if (data == null || !data.destructible) continue;

            Vector3 dropPos = cellWorld;
            WorldManager.Instance.DestroyBlock(cell.x, cell.y);
            ParticleManager.Instance?.EmitBlockBreak(cell, blockType);

            if (data.dropType != BlockType.Air)
            {
                var itemDef = itemRegistry.GetByBlockType(data.dropType);
                if (itemDef != null)
                    ItemDropSystem.Instance.DropItem(new ItemStack(itemDef, data.dropAmount), dropPos);
            }
        }
    }

    private void TryDestroyMultitile(MultitileObject obj)
    {
        var def = obj.Definition;
        var origin = obj.Origin;

        for (int y = 0; y < def.size.y; y++)
        for (int x = 0; x < def.size.x; x++)
        {
            var supportCell = new Vector2Int(origin.x + x, origin.y - 1);
            var blockType = WorldManager.Instance.GetBlock(supportCell.x, supportCell.y);
            if (blockType == BlockType.Air) continue;
        }

        if (ContainerUIManager.Instance.IsOpen)
            ContainerUIManager.Instance.CloseContainer();

        if (def.dropItem != null)
        {
            Vector2 dropPos = new Vector2(
                origin.x + def.size.x * 0.5f,
                origin.y + def.size.y * 0.5f);
            ItemDropSystem.Instance.DropItem(new ItemStack(def.dropItem, def.dropAmount), dropPos);
        }

        var cellsToCheck = new System.Collections.Generic.List<Vector2Int>();
        for (int y = 0; y < def.size.y; y++)
        for (int x = 0; x < def.size.x; x++)
            cellsToCheck.Add(new Vector2Int(origin.x + x, origin.y + y));

        MultitileObjectSystem.Instance.TryBreak(cellsToCheck[0], float.MaxValue);
    }

    private bool IsChestWithItems(MultitileObject obj)
    {
        if (obj is not ChestObject chest) return false;
        for (int i = 0; i < chest.Container.SlotCount; i++)
        {
            var slot = chest.Container.GetSlot(i);
            if (slot != null && !slot.IsEmpty) return true;
        }
        return false;
    }

    private MultitileObject GetObjectSupportedBy(Vector2Int supportCell)
    {
        var above = new Vector2Int(supportCell.x, supportCell.y + 1);
        return MultitileObjectSystem.Instance.Get(above);
    }

    private void DamageEntities(Vector3 worldPos, float radius, int playerDamage, int entityDamage, float knockbackForce)
    {
        var colliders = Physics2D.OverlapCircleAll(worldPos, radius);
        foreach (var col in colliders)
        {
            var playerStats = col.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(playerDamage, worldPos, knockbackForce);
                continue;
            }

            var entityStats = col.GetComponent<EntityStats>();
            if (entityStats != null)
                entityStats.TakeDamage(entityDamage, worldPos, knockbackForce);
        }
    }
}