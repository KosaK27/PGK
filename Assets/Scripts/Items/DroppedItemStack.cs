using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DroppedItemStack : MonoBehaviour
{
    public ItemStack ItemStack { get; set; }

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private float _lifetime;
    private float _pickupDelay = 0.5f;
    private float _spawnTime;
    public bool hasSettled = false;
    private float _scaleTimer = 0f;
    private float _scaleDuration = 0.15f;
    private DroppedItemStack _stackTarget = null;
    private float _stackCheckTimer = 0f;
    private float _stackCheckInterval = 0.2f;
    private bool _frozen = false;
    private float _defaultLifetime;

    public bool CanBePickedUp => Time.time > _spawnTime + _pickupDelay;

    public void Initialize(ItemStack stack, float lifetime, Vector2 force)
    {
        ItemStack = stack;
        _lifetime = lifetime;
        _spawnTime = Time.time;
        _defaultLifetime = lifetime;

        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();

        _sr.sprite = stack.item.sprite;
        _rb.gravityScale = 2f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.AddForce(force, ForceMode2D.Impulse);

        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (!ChunkManager.Instance.IsChunkLoaded(transform.position))
        {
            Freeze();
            return;
        }
        Unfreeze();
        if (_stackTarget != null)
        {
            if (_stackTarget.ItemStack == null || _stackTarget.ItemStack.amount <= 0)
            {
                _stackTarget = null;
            }
            else
            {
                Vector2 dir = (Vector2)_stackTarget.transform.position - (Vector2)transform.position;
                float dist = dir.magnitude;

                if (dist < 0.15f)
                {
                    _rb.linearVelocity = Vector2.zero;
                    RestoreCollision();
                    ItemDropSystem.Instance.MergeInto(_stackTarget, this);
                    return;
                }

                float startDist = 1.5f;
                float scaleMultiplier = Mathf.Clamp01(dist / startDist);
                float baseScale = 0.75f;
                transform.localScale = new Vector3(baseScale * scaleMultiplier, baseScale * scaleMultiplier, 1f);

                _rb.linearVelocity = dir.normalized * 5f;
            }
        }

        if (_scaleTimer < _scaleDuration)
        {
            _scaleTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_scaleTimer / _scaleDuration);
            float scale = Mathf.Lerp(0f, 0.75f, t);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        if (_scaleTimer >= _scaleDuration && _stackTarget == null)
        {
            _stackCheckTimer += Time.deltaTime;
            if (_stackCheckTimer >= _stackCheckInterval)
            {
                _stackCheckTimer = 0f;
                ItemDropSystem.Instance.TryStackWithNearby(this);
            }
        }

        _lifetime -= Time.deltaTime;
        if (_lifetime < 5f)
        {
            float blink = Mathf.Sin(Time.time * 10f);
            _sr.enabled = blink > 0f;
        }
        if (_lifetime <= 0f)
            Destroy(gameObject);
    }

    public void SetStackTarget(DroppedItemStack target)
    {
        if (target._stackTarget == this) return;
        
        _stackTarget = target;
        _rb.bodyType = RigidbodyType2D.Dynamic;

        var col = GetComponent<BoxCollider2D>();
        col.excludeLayers = LayerMask.GetMask("Player", "Enemy", "Default");
    }

    public void AddAmount(int amount)
    {
        ItemStack = new ItemStack(ItemStack.item, ItemStack.amount + amount);
    }

    public void RestoreCollision()
    {
        var col = GetComponent<BoxCollider2D>();
        col.excludeLayers = LayerMask.GetMask("Player", "Enemy");
    }

    public void Freeze()
    {
        if (_frozen) return;
        _frozen = true;
        _rb.bodyType = RigidbodyType2D.Static;
        _sr.enabled = false;
    }

    public void Unfreeze()
    {
        if (!_frozen) return;
        _frozen = false;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _sr.enabled = true;
    }

    public void ResetLifetime()
    {
        _lifetime = _defaultLifetime;
    }
}