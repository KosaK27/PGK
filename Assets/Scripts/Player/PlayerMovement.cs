using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [SerializeField] private int maxJumps = 2;

    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.8f;

    public bool isGrounded;
    public bool IsDashing { get; private set; }
    public PlayerLocomotionState LocomotionState { get; private set; }
    public PlayerAirState AirState { get; private set; }
    public PlayerActionState ActionState { get; set; }

    public PlayerState State
    {
        get
        {
            if (ActionState == PlayerActionState.Dead)
                return PlayerState.Dead;

            if (ActionState == PlayerActionState.UsingWeapon)
                return PlayerState.UseWeapon;

            if (ActionState == PlayerActionState.UsingTool)
                return PlayerState.UseTool;

            if (IsDashing)
                return PlayerState.Dash;

            if (AirState == PlayerAirState.Jumping)
                return PlayerState.Jump;

            if (LocomotionState == PlayerLocomotionState.Walk)
                return PlayerState.Walk;

            return PlayerState.Idle;
        }
    }

    private Rigidbody2D _rb;
    private int _jumpsLeft;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashDir;
    private float _groundedTimer;
    private const float GROUNDED_COOLDOWN = 0.1f;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _jumpsLeft = maxJumps;
    }

    void Update()
    {
        if (ActionState == PlayerActionState.Dead) return;

        if (_groundedTimer > 0) { _groundedTimer -= Time.deltaTime; isGrounded = true; }
        _dashCooldownTimer -= Time.deltaTime;

        if (IsDashing)
        {
            _dashTimer -= Time.deltaTime;
            _rb.linearVelocity = new Vector2(_dashDir * dashSpeed, 0f);
            if (_dashTimer <= 0f) IsDashing = false;
        }
        else
        {
            Move();
            Jump();
            TryDash();
        }

        UpdateStates();
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
        else dir = transform.localScale.x > 0 ? 1f : -1f;

        _dashDir = dir;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;
        IsDashing = true;
    }

    private void UpdateStates()
    {
        AirState = isGrounded ? PlayerAirState.Grounded : PlayerAirState.Jumping;
        float vx = Mathf.Abs(_rb.linearVelocity.x);
        LocomotionState = vx > 0.05f ? PlayerLocomotionState.Walk : PlayerLocomotionState.Idle;
    }

    public void SetDead(bool dead)
    {
        ActionState = dead ? PlayerActionState.Dead : PlayerActionState.None;
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