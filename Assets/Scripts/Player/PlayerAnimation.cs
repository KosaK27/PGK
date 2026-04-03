using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimation : MonoBehaviour
{
    public Sprite legsIdle;
    public Sprite[] legsWalk = new Sprite[4];
    public Sprite legsJump;

    public Sprite frontArmIdle;
    public Sprite[] frontArmWalk = new Sprite[4];
    public Sprite frontArmJump;

    public Sprite backArmIdle;
    public Sprite[] backArmWalk = new Sprite[4];
    public Sprite backArmJump;

    public SpriteRenderer legsRenderer;
    public SpriteRenderer frontArmRenderer;
    public SpriteRenderer backArmRenderer;

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
        if (_movement.ActionState == PlayerActionState.Dead) return;

        bool movingLeft = Keyboard.current.aKey.isPressed;
        bool movingRight = Keyboard.current.dKey.isPressed;

        if (movingRight) transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (movingLeft) transform.localScale = new Vector3(1f, 1f, 1f);

        bool inAir = _movement.AirState == PlayerAirState.Jumping;
        bool walking = _movement.LocomotionState == PlayerLocomotionState.Walk;

        if (inAir || _movement.IsDashing)
        {
            SetArms(frontArmJump, backArmJump);
            legsRenderer.sprite = legsJump;
            _walkTimer = 0f;
            _walkFrame = 0;
            return;
        }

        if (walking)
        {
            _walkTimer += Time.deltaTime;
            if (_walkTimer >= 1f / walkFrameRate)
            {
                _walkTimer = 0f;
                _walkFrame = (_walkFrame + 1) % 4;
            }

            legsRenderer.sprite = legsWalk[_walkFrame];
            SetArms(frontArmWalk[_walkFrame], backArmWalk[_walkFrame]);
            return;
        }

        _walkTimer = 0f;
        _walkFrame = 0;
        legsRenderer.sprite = legsIdle;
        SetArms(frontArmIdle, backArmIdle);
    }

    private void SetArms(Sprite front, Sprite back)
    {
        if (_movement.ActionState == PlayerActionState.UsingTool ||
            _movement.ActionState == PlayerActionState.UsingWeapon)
            return;

        frontArmRenderer.sprite = front;
        backArmRenderer.sprite = back;
    }
}