using UnityEngine;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;
    public Vector3 respawnPoint = new Vector3(100f, 52.5f, 0f);

    private HitEffect _hitEffect;

    void Start()
    {
        currentHP = maxHP;
        _hitEffect = GetComponent<HitEffect>();
    }

    public void TakeDamage(int damage, Vector2? sourcePosition = null)
    {
        currentHP -= damage;

        if (sourcePosition.HasValue && _hitEffect != null)
            _hitEffect.TriggerHit(sourcePosition.Value);

        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Rigidbody2D>().simulated = false;
        GetComponent<PlayerMovement>().enabled = false;
        yield return new WaitForSeconds(3f);
        transform.position = respawnPoint;
        currentHP = maxHP;
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Rigidbody2D>().simulated = true;
        GetComponent<PlayerMovement>().enabled = true;
    }
}