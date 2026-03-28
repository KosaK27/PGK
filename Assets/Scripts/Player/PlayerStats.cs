using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerStats : MonoBehaviour
{
    public int maxHP = 20;
    public int currentHP;

    private HitEffect _hitEffect;

    public event System.Action<int, int> OnHealthChanged;

    void Start()
    {
        currentHP = maxHP;
        _hitEffect = GetComponent<HitEffect>();
    }

    public void TakeDamage(int damage, Vector2? sourcePosition = null)
    {
        currentHP -= damage;
        currentHP  = Mathf.Max(0, currentHP);

        if (sourcePosition.HasValue && _hitEffect != null)
            _hitEffect.TriggerHit(sourcePosition.Value);

        OnHealthChanged?.Invoke(currentHP, maxHP);
        Debug.Log($"TakeDamage: {currentHP}/{maxHP}, listeners: {OnHealthChanged?.GetInvocationList().Length}");
        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        StartCoroutine(Respawn());
    }

    System.Collections.IEnumerator Respawn()
    {
        SetRenderersEnabled(false);
        GetComponent<Rigidbody2D>().simulated = false;
        GetComponent<PlayerMovement>().enabled = false;

        yield return new WaitForSeconds(3f);

        transform.position = PlayerSpawner.Instance.GetSpawnPosition();
        currentHP = maxHP;

        SetRenderersEnabled(true);
        GetComponent<Rigidbody2D>().simulated = true;
        GetComponent<PlayerMovement>().enabled = true;

        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void SetRenderersEnabled(bool enabled)
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = enabled;
    }
}