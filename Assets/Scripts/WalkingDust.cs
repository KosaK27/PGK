using UnityEngine;

public class WalkingDust : MonoBehaviour
{
    [SerializeField] private float emitInterval = 0.13f;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, -0.45f);

    private float _timer;
    private PlayerMovement _movement;
    private Rigidbody2D _rb;

    void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        bool shouldEmit = _movement.isGrounded &&
                          _movement.LocomotionState == PlayerLocomotionState.Walk &&
                          !_movement.IsDashing;

        if (!shouldEmit) { _timer = 0f; return; }

        _timer += Time.deltaTime;
        if (_timer >= emitInterval)
        {
            _timer = 0f;
            EmitDust();
        }
    }

    private void EmitDust()
    {
        Vector3 checkPos = transform.position + new Vector3(0f, -2f, 0f);
        Vector3Int feetCell = WorldManager.Instance.WorldToCell(checkPos);
        BlockType blockBelow = WorldManager.Instance.GetBlock(feetCell.x, feetCell.y);
        if (blockBelow == BlockType.Air) return;
        Vector3 spawnPos = transform.position + (Vector3)spawnOffset;
        ParticleManager.Instance.EmitDust(spawnPos, blockBelow);
    }
}