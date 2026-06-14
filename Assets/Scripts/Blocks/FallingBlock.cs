using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class FallingBlock : MonoBehaviour
{
    public BlockType BlockType { get; private set; }

    private Rigidbody2D _rigidbody;
    private bool _landed;

    public void Init(BlockType type, BlockSpriteMap spriteMap)
    {
        BlockType = type;

        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 3f;
        _rigidbody.freezeRotation = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = GetComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);
        col.isTrigger = true;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Foreground";
        spriteRenderer.sortingOrder = 50;

        var sprite = spriteMap != null ? spriteMap.Get(type) : null;
        if (sprite != null)
            spriteRenderer.sprite = sprite;
        else
            spriteRenderer.color = FallbackColor(type);
    }

    void FixedUpdate()
    {
        if (_landed) return;
        PushOverlappingEntities();
        CheckLanding();
    }

    void PushOverlappingEntities()
    {
        var hits = Physics2D.OverlapBoxAll(transform.position, new Vector2(0.85f, 0.85f), 0f);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.GetComponent<FallingBlock>() != null) continue;

            var entityRigidbody = hit.attachedRigidbody;
            if (entityRigidbody == null || entityRigidbody.bodyType == RigidbodyType2D.Kinematic) continue;

            var delta = hit.transform.position - transform.position;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                entityRigidbody.linearVelocity = new Vector2(
                    Mathf.Sign(delta.x) * Mathf.Max(Mathf.Abs(entityRigidbody.linearVelocity.x), Mathf.Abs(delta.x) * 8f),
                    entityRigidbody.linearVelocity.y
                );
            }
            else if (delta.y > 0f)
            {
                entityRigidbody.linearVelocity = new Vector2(
                    entityRigidbody.linearVelocity.x,
                    Mathf.Max(entityRigidbody.linearVelocity.y, 6f)
                );
            }
        }
    }

    void CheckLanding()
    {
        if (_rigidbody.linearVelocity.y >= 0.1f) return;

        int worldX = Mathf.RoundToInt(transform.position.x - 0.5f);
        int worldY = Mathf.RoundToInt(transform.position.y - 0.5f);

        var worldManager = WorldManager.Instance;
        if (worldManager == null) return;

        var blockBelow = worldManager.GetBlock(worldX, worldY - 1);
        var multitileSystem = MultitileObjectSystem.Instance;

        if (blockBelow == BlockType.Water)
        {
            Land(worldX, worldY, worldManager, LandingContext.Water);
            return;
        }

        if (multitileSystem.IsOccupied(new Vector2Int(worldX, worldY - 1)))
        {
            Land(worldX, worldY, worldManager, LandingContext.Multitile);
            return;
        }

        if (blockBelow == BlockType.Air) return;

        var dataBelow = worldManager.GetBlockDataForType(blockBelow);
        if (dataBelow != null && !dataBelow.isSolid)
        {
            Land(worldX, worldY, worldManager, LandingContext.NonSolid);
            return;
        }

        Land(worldX, worldY, worldManager, LandingContext.Solid);
    }

    enum LandingContext { Solid, NonSolid, Water, Multitile }

    void Land(int worldX, int worldY, WorldManager worldManager, LandingContext context)
    {
        _landed = true;

        if (context == LandingContext.Water || context == LandingContext.Multitile)
        {
            DropAsItem(worldManager);
        }
        else if (context == LandingContext.NonSolid)
        {
            BreakBlockAt(worldX, worldY - 1, worldManager);
            worldManager.PlaceBlock(worldX, worldY - 1, BlockType);
        }
        else
        {
            worldManager.PlaceBlock(worldX, worldY, BlockType);
        }

        GravityBlockSystem.Instance.RemoveFalling(this);
        Destroy(gameObject);
    }

    void BreakBlockAt(int worldX, int worldY, WorldManager worldManager)
    {
        var blockData = worldManager.GetBlockDataForType(worldManager.GetBlock(worldX, worldY));
        if (blockData != null && blockData.dropType != BlockType.Air && ItemRegistry.Instance != null)
        {
            var itemDefinition = ItemRegistry.Instance.GetByBlockType(blockData.dropType);
            if (itemDefinition != null)
                ItemDropSystem.Instance?.DropItem(
                    new ItemStack(itemDefinition, blockData.dropAmount),
                    new Vector2(worldX + 0.5f, worldY + 0.5f)
                );
        }
        worldManager.DestroyBlock(worldX, worldY);
    }

    void DropAsItem(WorldManager worldManager)
    {
        if (ItemDropSystem.Instance == null) return;
        if (ItemRegistry.Instance == null) return;

        var blockData = worldManager.GetBlockDataForType(BlockType);
        if (blockData == null || blockData.dropType == BlockType.Air) return;

        var itemDefinition = ItemRegistry.Instance.GetByBlockType(blockData.dropType);
        if (itemDefinition == null) return;

        ItemDropSystem.Instance.DropItem(
            new ItemStack(itemDefinition, blockData.dropAmount),
            transform.position
        );
    }

    static Color FallbackColor(BlockType type) => type switch
    {
        BlockType.Sand => new Color(0.87f, 0.78f, 0.45f), _ => Color.white
    };
}