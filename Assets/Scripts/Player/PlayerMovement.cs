using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    [SerializeField] private int maxJumpsBase = 1;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Water Settings")]
    [SerializeField] private float swimUpSpeed = 2f;
    [SerializeField] private float waterGravityScale = 0.4f;
    [SerializeField] private float waterMoveSpeedMultiplier = 0.6f;
    [SerializeField] private float waterLeapForce = 10f;

    public bool isGrounded;
    public bool IsDashing { get; private set; }
    private bool _isInWater;
    private bool _isSteppingUp;
    private float _stepUpTargetY;

    public PlayerLocomotionState LocomotionState { get; private set; }
    public PlayerAirState AirState { get; private set; }
    public PlayerActionState ActionState { get; set; }

    public PlayerState State
    {
        get
        {
            if (ActionState == PlayerActionState.Dead) return PlayerState.Dead;
            if (ActionState == PlayerActionState.UsingWeapon) return PlayerState.UseWeapon;
            if (ActionState == PlayerActionState.UsingTool) return PlayerState.UseTool;
            if (IsDashing) return PlayerState.Dash;
            if (AirState == PlayerAirState.Jumping) return PlayerState.Jump;
            if (LocomotionState == PlayerLocomotionState.Walk) return PlayerState.Walk;
            return PlayerState.Idle;
        }
    }

    private Rigidbody2D _rb;
    private int _jumpsLeft;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashDir;
    private float _groundedTimer;
    private float _defaultGravityScale;
    private float _knockbackTimer;
    private bool _isJumping;
    private float _jumpTimer;
    private const float GROUNDED_COOLDOWN = 0.1f;
    private const float KNOCKBACK_DURATION = 0.15f;
    private const float MAX_JUMP_HOLD_TIME = 0.3f;

    private bool HasDash => AccessorySystem.Instance != null && AccessorySystem.Instance.HasEffect(AccessoryEffect.LightningBoots);
    private bool HasDoubleJump => AccessorySystem.Instance != null && AccessorySystem.Instance.HasEffect(AccessoryEffect.BatWings);
    private bool HasStepUp => AccessorySystem.Instance != null && (AccessorySystem.Instance.HasEffect(AccessoryEffect.LeatherBoots) || AccessorySystem.Instance.HasEffect(AccessoryEffect.LightningBoots));
    private int MaxJumps => HasDoubleJump ? 2 : maxJumpsBase;

    public void ApplyKnockback()
    {
        _knockbackTimer = KNOCKBACK_DURATION;
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _jumpsLeft = MaxJumps;
        _defaultGravityScale = _rb.gravityScale;
    }

    void Update()
    {
        if (ActionState == PlayerActionState.Dead) return;

        CheckWater();

        if (_groundedTimer > 0) { _groundedTimer -= Time.deltaTime; isGrounded = true; }
        _dashCooldownTimer -= Time.deltaTime;

        if (_isSteppingUp)
        {
            StepUpMove();
            return;
        }

        if (IsDashing)
        {
            _dashTimer -= Time.deltaTime;
            _rb.linearVelocity = new Vector2(_dashDir * dashSpeed, 0f);
            if (_dashTimer <= 0f) IsDashing = false;
        }
        else
        {
            Move();

            if (_isInWater)
            {
                Swim();
                IsDashing = false;
            }
            else
            {
                Jump();
                if (HasDash) TryDash();
                if (HasStepUp) TryStepUp();
            }
        }

        UpdateStates();

        bool isMoving = LocomotionState == PlayerLocomotionState.Walk;
        PlayerAudioManager.Instance?.TryPlayFootstep(isMoving, isGrounded);
    }

    private void TryStepUp()
    {
        if (!isGrounded) return;

        float moveInput = 0f;
        if (Keyboard.current.aKey.isPressed) moveInput = -1f;
        if (Keyboard.current.dKey.isPressed) moveInput = 1f;
        if (moveInput == 0f) return;

        int layerMask = ~LayerMask.GetMask("Player");

        Vector2 direction = new Vector2(moveInput, 0f);
        Vector2 wallCheckOrigin = (Vector2)transform.position + new Vector2(0f, -1.35f);
        var hitWall = Physics2D.Raycast(wallCheckOrigin, direction, 1.0f, layerMask);
        if (!hitWall.collider) return;

        Vector2 landingCenter = (Vector2)transform.position + new Vector2(moveInput * 0.5f, 1f);
        var overlap = Physics2D.OverlapBox(landingCenter, new Vector2(2.5f, 2.8f), 0f, layerMask);
        if (overlap) return;

        _isSteppingUp = true;
        _stepUpTargetY = transform.position.y + 1.1f;
    }

    private void StepUpMove()
    {
        float newY = Mathf.MoveTowards(transform.position.y, _stepUpTargetY, 20f * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);

        if (Mathf.Abs(transform.position.y - _stepUpTargetY) < 0.01f)
        {
            transform.position = new Vector3(transform.position.x, _stepUpTargetY, transform.position.z);
            _isSteppingUp = false;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        }
    }

    private void CheckWater()
    {
        Vector3Int feetCell = WorldManager.Instance.WorldToCell(transform.position + new Vector3(0, 0.1f, 0));
        Vector3Int bodyCell = WorldManager.Instance.WorldToCell(transform.position + new Vector3(0, 0.8f, 0));

        BlockType feetBlock = WorldManager.Instance.GetBlock(feetCell.x, feetCell.y);
        BlockType bodyBlock = WorldManager.Instance.GetBlock(bodyCell.x, bodyCell.y);

        _isInWater = (feetBlock == BlockType.Water || bodyBlock == BlockType.Water);

        if (_isInWater)
        {
            _rb.gravityScale = waterGravityScale;
            _jumpsLeft = MaxJumps;
        }
        else
        {
            _rb.gravityScale = _defaultGravityScale;
        }
    }

    private void Move()
    {
        if (_isSteppingUp) return;

        float move = 0;
        if (Keyboard.current.aKey.isPressed) move = -1;
        if (Keyboard.current.dKey.isPressed) move = 1;

        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.deltaTime;
            if (move != 0)
                _rb.linearVelocity = new Vector2(move * moveSpeed, _rb.linearVelocity.y);
            return;
        }

        float currentSpeed = _isInWater ? moveSpeed * waterMoveSpeedMultiplier : moveSpeed;
        _rb.linearVelocity = new Vector2(move * currentSpeed, _rb.linearVelocity.y);
    }

    private void Swim()
    {
        if (Keyboard.current.spaceKey.isPressed)
        {
            Vector3Int cellAbove = WorldManager.Instance.WorldToCell(transform.position + new Vector3(0, 1.5f, 0));
            BlockType blockAbove = WorldManager.Instance.GetBlock(cellAbove.x, cellAbove.y);

            if (blockAbove == BlockType.Air)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, waterLeapForce);
                _isInWater = false;
            }
            else
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, swimUpSpeed);
            }
        }
    }

    private const float MIN_JUMP_FORCE = 6f;

    private void Jump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && _jumpsLeft > 0)
        {
            _isJumping = true;
            _jumpTimer = 0f;
            _jumpsLeft--;
            isGrounded = false;
            _groundedTimer = 0;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, MIN_JUMP_FORCE);

            if (_jumpsLeft == MaxJumps - 1)
                PlayerAudioManager.Instance?.PlayJump();
            else
                PlayerAudioManager.Instance?.PlayDoubleJump();
        }

        if (_isJumping)
        {
            if (Keyboard.current.spaceKey.isPressed && _jumpTimer < MAX_JUMP_HOLD_TIME)
            {
                float t = _jumpTimer / MAX_JUMP_HOLD_TIME;
                float currentJumpSpeed = Mathf.Lerp(MIN_JUMP_FORCE, jumpForce, t);
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, currentJumpSpeed);
                _jumpTimer += Time.deltaTime;
            }
            else
            {
                _isJumping = false;
            }
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

        PlayerAudioManager.Instance?.PlayDash();
    }

    private void UpdateStates()
    {
        if (_isInWater)
            AirState = PlayerAirState.Jumping;
        else
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
        if (_isInWater) return;

        foreach (ContactPoint2D c in col.contacts)
        {
            if (c.normal.y > 0.5f)
            {
                if (!isGrounded) _jumpsLeft = MaxJumps;
                isGrounded = true;
                _groundedTimer = GROUNDED_COOLDOWN;
                if (_rb.linearVelocity.y <= 0f)
                    _isJumping = false;
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