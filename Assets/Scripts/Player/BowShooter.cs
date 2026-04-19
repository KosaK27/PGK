using UnityEngine;
using UnityEngine.InputSystem;

public class BowShooter : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject arrowPrefab;

    [Header("Ammo")]
    [SerializeField] private ItemDefinition arrowItemDef;

    [Header("Refs")]
    [SerializeField] private Camera mainCamera;

    private float _cooldownTimer;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        var selected = InventorySystem.Instance.SelectedItem;
        bool hasBow = selected != null && !selected.IsEmpty
                       && selected.item.isWeapon
                       && selected.item.weaponType == WeaponType.Bow;

        if (!hasBow) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (_cooldownTimer > 0f) return;

        if (arrowItemDef != null && !InventorySystem.Instance.HasItem(arrowItemDef, 1))
        {
            Debug.Log("Brak strza³!");
            return;
        }

        Shoot(selected.item);
    }

    void Shoot(ItemDefinition bowDef)
    {
        var mousePos = Mouse.current.position.ReadValue();
        var worldPos = mainCamera.ScreenToWorldPoint(
                           new Vector3(mousePos.x, mousePos.y, 0));
        worldPos.z = 0;

        Vector2 dir = ((Vector2)worldPos - (Vector2)transform.position).normalized;
        Vector2 origin = (Vector2)transform.position + dir * 0.8f;

        var go = Instantiate(arrowPrefab, origin, Quaternion.identity);
        var p = go.GetComponent<Projectile>();
        if (p != null)
            p.Init(dir, bowDef.damage, bowDef.projectileSpeed,
                   false, true, gameObject);

        if (arrowItemDef != null)
            InventorySystem.Instance.RemoveItem(arrowItemDef, 1);

        _cooldownTimer = bowDef.shootCooldown;
    }
}