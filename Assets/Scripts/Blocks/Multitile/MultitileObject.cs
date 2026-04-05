using UnityEngine;

public class MultitileObject : MonoBehaviour
{
    public MultitileObjectDefinition Definition { get; private set; }
    public Vector2Int Origin { get; private set; }

    private SpriteRenderer _sr;
    private float _breakProgress;

    public void Initialize(MultitileObjectDefinition def, Vector2Int origin)
    {
        Definition = def;
        Origin = origin;

        transform.position = new Vector3(origin.x, origin.y, 0);

        var spriteGo = new GameObject("Sprite");
        spriteGo.transform.SetParent(transform);
        spriteGo.transform.localPosition = new Vector3(def.size.x * 0.5f, def.size.y * 0.5f, 0);
        _sr = spriteGo.AddComponent<SpriteRenderer>();
        _sr.sprite = def.sprite;
        _sr.sortingOrder = def.sortingOrder;

        if (def.hasCollision)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(def.size.x, def.size.y);
            col.offset = new Vector2(def.size.x * 0.5f, def.size.y * 0.5f);
        }
    }

    public virtual void Interact() { }

    public void AddBreakProgress(float amount) => _breakProgress += amount;
    public void ResetBreakProgress() => _breakProgress = 0f;
    public float GetProgress() => Definition != null ? _breakProgress / Definition.hardness : 0f;
    public bool IsComplete() => _breakProgress >= Definition.hardness;
}