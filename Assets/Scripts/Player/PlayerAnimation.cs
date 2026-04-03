using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimation : MonoBehaviour
{
    [Header("Leg Sprites")]
    public Sprite legsIdle;
    public Sprite[] legsWalk = new Sprite[4];
    public Sprite legsJump;

    [Header("Front Arm Sprites")]
    public Sprite frontArmIdle;
    public Sprite[] frontArmWalk = new Sprite[4];
    public Sprite frontArmJump;

    [Header("Back Arm Sprites")]
    public Sprite backArmIdle;
    public Sprite[] backArmWalk = new Sprite[4];
    public Sprite backArmJump;

    [Header("Renderers")]
    public SpriteRenderer legsRenderer;
    public SpriteRenderer frontArmRenderer;
    public SpriteRenderer backArmRenderer;

    [Header("Settings")]
    public float walkFrameRate = 8f;

    private PlayerMovement _movement;
    private float _walkTimer;
    private int _walkFrame;

    void Start()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        var state = _movement.State;
        if (state == PlayerState.Dead) return;

        bool movingLeft = Keyboard.current.aKey.isPressed;
        bool movingRight = Keyboard.current.dKey.isPressed;

        if (movingRight) transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (movingLeft) transform.localScale = new Vector3(1f, 1f, 1f);

        bool walking = state == PlayerState.Walk;
        if (walking)
        {
            _walkTimer += Time.deltaTime;
            if (_walkTimer >= 1f / walkFrameRate) { _walkTimer = 0f; _walkFrame = (_walkFrame + 1) % 4; }
        }
        else
        {
            _walkTimer = 0f;
            _walkFrame = 0;
        }

        switch (state)
        {
            case PlayerState.Jump:
            case PlayerState.Dash:
                legsRenderer.sprite = legsJump;
                frontArmRenderer.sprite = frontArmJump;
                backArmRenderer.sprite = backArmJump;
                break;
            case PlayerState.Walk:
                legsRenderer.sprite = legsWalk[_walkFrame];
                frontArmRenderer.sprite = frontArmWalk[_walkFrame];
                backArmRenderer.sprite = backArmWalk[_walkFrame];
                break;
            default:
                legsRenderer.sprite = legsIdle;
                frontArmRenderer.sprite = frontArmIdle;
                backArmRenderer.sprite = backArmIdle;
                break;
        }
    }
}