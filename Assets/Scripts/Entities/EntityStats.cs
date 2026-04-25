using System;
using UnityEngine;

public class EntityStats : MonoBehaviour
{
    public EntityData data;

    public int CurrentHP { get; private set; }
    public bool IsDead { get; private set; }

    public event Action OnDeath;
    public event Action<int, int> OnHPChanged;

    private HitEffect _hitEffect;
    private EntityAI _ai;

    void Awake()
    {
        if (data != null)
            CurrentHP = data.maxHP;
        _hitEffect = GetComponent<HitEffect>();
        _ai = GetComponent<EntityAI>();
    }

    public void Initialize(EntityData entityData)
    {
        data = entityData;
        CurrentHP = data.maxHP;
        IsDead = false;
    }

    public void TakeDamage(int amount, Vector2? sourcePosition = null, float knockbackForce = 10f)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, data.maxHP);

        if (sourcePosition.HasValue)
        {
            if (_hitEffect != null)
                _hitEffect.TriggerHit(sourcePosition.Value);

            float resistance = Mathf.Clamp(data.knockbackResistance, 0f, 10f);
            float multiplier = 1f - resistance / 10f;

            if (multiplier > 0f)
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 horizontal = new Vector2(
                        transform.position.x - sourcePosition.Value.x, 0f);

                    if (horizontal.sqrMagnitude < 0.001f)
                        horizontal = Vector2.right;
                    else
                        horizontal.Normalize();

                    Vector2 force = new Vector2(horizontal.x * knockbackForce, knockbackForce) * multiplier;
                    rb.linearVelocity = Vector2.zero;
                    rb.AddForce(force, ForceMode2D.Impulse);
                    _ai?.ApplyKnockback();
                }
            }
        }

        if (CurrentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Min(data.maxHP, CurrentHP + amount);
        OnHPChanged?.Invoke(CurrentHP, data.maxHP);
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject, 0.3f);
    }
}