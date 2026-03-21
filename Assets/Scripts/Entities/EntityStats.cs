using System;
using UnityEngine;

public class EntityStats : MonoBehaviour
{
    public EntityData data;

    public int CurrentHP { get; private set; }
    public bool IsDead { get; private set; }

    public event Action OnDeath;
    public event Action<int, int> OnHPChanged;

    void Awake()
    {
        if (data != null)
            CurrentHP = data.maxHP;
    }

    public void Initialize(EntityData entityData)
    {
        data = entityData;
        CurrentHP = data.maxHP;
        IsDead = false;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, data.maxHP);

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