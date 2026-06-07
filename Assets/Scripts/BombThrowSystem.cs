using UnityEngine;
using UnityEngine.InputSystem;

public class BombThrowSystem : MonoBehaviour
{
    public static BombThrowSystem Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private float throwForce = 12f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (mainCamera == null) mainCamera = Camera.main;
    }

    public void ThrowBomb(GameObject bombPrefab, Vector3 spawnPos, ItemDefinition item)
    {
        if (bombPrefab == null) return;

        var mousePos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
        mouseWorld.z = 0;

        Vector2 dir = ((Vector2)mouseWorld - (Vector2)spawnPos).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;

        var bomb = Object.Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        var projectile = bomb.GetComponent<BombProjectile>();
        projectile?.Launch(dir * throwForce);
    }
}