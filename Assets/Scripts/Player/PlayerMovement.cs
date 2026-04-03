using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Double Jump")]
    [SerializeField] private int maxJumps = 2;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.8f;

    public bool isGrounded;
    public PlayerState State { get; private set; } = PlayerState.Idle;

    private Rigidbody2D _rb;
    private PlayerStats _stats;
    private int _jumpsLeft;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashDir;
    private bool _isDashing;
    private float _groundedTimer;
    private const float GROUNDED_COOLDOWN = 0.1f;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<PlayerStats>();
        _jumpsLeft = maxJumps;
    }

    void Update()
    {
        if (State == PlayerState.Dead) return;

        if (_groundedTimer > 0) { _groundedTimer -= Time.deltaTime; isGrounded = true; }
        _dashCooldownTimer -= Time.deltaTime;

        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            _rb.linearVelocity = new Vector2(_dashDir * dashSpeed, 0f);
            if (_dashTimer <= 0f) { _isDashing = false; }
            UpdateState();
            return;
        }

        Move();
        Jump();
        TryDash();
        UpdateState();
    }

    private void Move()
    {
        float move = 0;
        if (Keyboard.current.aKey.isPressed) move = -1;
        if (Keyboard.current.dKey.isPressed) move = 1;
        _rb.linearVelocity = new Vector2(move * moveSpeed, _rb.linearVelocity.y);
    }

    private void Jump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && _jumpsLeft > 0)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
            _jumpsLeft--;
        }
    }

    private void TryDash()
    {
        if (!Keyboard.current.leftShiftKey.wasPressedThisFrame) return;
        if (_dashCooldownTimer > 0f) return;

        float dir = 0f;
        if (Keyboard.current.dKey.isPressed) dir = 1f;
        else if (Keyboard.current.aKey.isPressed) dir = -1f;
        else dir = transform.localScale.x > 0 ? -1f : 1f;

        _dashDir = dir;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;
        _isDashing = true;
    }

    private void UpdateState()
    {
        if (_isDashing) { State = PlayerState.Dash; return; }
        if (!isGrounded) { State = PlayerState.Jump; return; }

        float vx = Mathf.Abs(_rb.linearVelocity.x);
        State = vx > 0.05f ? PlayerState.Walk : PlayerState.Idle;
    }

    public void SetDead(bool dead)
    {
        State = dead ? PlayerState.Dead : PlayerState.Idle;
    }

    void OnCollisionStay2D(Collision2D col)
    {
        foreach (ContactPoint2D c in col.contacts)
        {
            if (c.normal.y > 0.5f)
            {
                if (!isGrounded) _jumpsLeft = maxJumps;
                isGrounded = true;
                _groundedTimer = GROUNDED_COOLDOWN;
                return;
            }
        }
        isGrounded = false;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        _groundedTimer = 0f;
        isGrounded = false;
    }
}