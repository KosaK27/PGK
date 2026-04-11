using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Stats")]
    [SerializeField] private int damage = 5;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float cooldown = 2f;

    [Header("Flags")]
    [SerializeField] private bool hitsPlayer = true;
    [SerializeField] private bool hitsEntities = false;

    private float _cooldownTimer;
    public bool CanShoot => _cooldownTimer <= 0f;

    void Update() => _cooldownTimer -= Time.deltaTime;

    public void Shoot(Vector2 origin, Vector2 direction)
    {
        if (!CanShoot) return;

        var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var p = go.GetComponent<Projectile>();
        if (p != null)
            p.Init(direction, damage, projectileSpeed,
                   hitsPlayer, hitsEntities, gameObject);

        _cooldownTimer = cooldown;
    }
}