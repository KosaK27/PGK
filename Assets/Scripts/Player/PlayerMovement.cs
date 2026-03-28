using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Double Jump")]
    [SerializeField] private int maxJumps = 2;
    private int _jumpsLeft;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.8f;

    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashDir;
    private bool _isDashing;

    private Rigidbody2D rb;
    public bool isGrounded;
    private float groundedTimer;
    private const float GROUNDED_COOLDOWN = 0.1f;
    private PlayerStats stats;
    private SpriteRenderer _sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        _sr = GetComponentInChildren<SpriteRenderer>();
        _jumpsLeft = maxJumps;
    }

    void Update()
    {
        if (groundedTimer > 0) { groundedTimer -= Time.deltaTime; isGrounded = true; }

        _dashCooldownTimer -= Time.deltaTime;

        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            rb.linearVelocity = new Vector2(_dashDir * dashSpeed, 0f);
            if (_dashTimer <= 0f)
                _isDashing = false;
            DamageTest();
            return;
        }

        Move();
        Jump();
        TryDash();
        DamageTest();
    }

    void Move()
    {
        float move = 0;
        if (Keyboard.current.aKey.isPressed) move = -1;
        if (Keyboard.current.dKey.isPressed) move = 1;
        rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);
    }

    void Jump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && _jumpsLeft > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            _jumpsLeft--;
        }
    }

    void TryDash()
    {
        if (!Keyboard.current.leftShiftKey.wasPressedThisFrame) return;
        if (_dashCooldownTimer > 0f) return;

        float dir = 0f;
        if (Keyboard.current.dKey.isPressed) dir = 1f;
        else if (Keyboard.current.aKey.isPressed) dir = -1f;
        else dir = (_sr != null && _sr.flipX) ? -1f : 1f;

        _dashDir = dir;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;
        _isDashing = true;
    }

    void DamageTest()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame) stats.TakeDamage(50);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        foreach (ContactPoint2D c in col.contacts)
        {
            if (c.normal.y > 0.5f)
            {
                if (!isGrounded) _jumpsLeft = maxJumps;
                isGrounded = true;
                groundedTimer = GROUNDED_COOLDOWN;
                return;
            }
        }
        isGrounded = false;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        groundedTimer = 0f;
        isGrounded = false;
    }
}