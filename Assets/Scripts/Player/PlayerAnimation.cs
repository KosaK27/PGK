using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimator : MonoBehaviour
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

    [Header("Child Renderers")]
    public SpriteRenderer legsRenderer;
    public SpriteRenderer frontArmRenderer;
    public SpriteRenderer backArmRenderer;
    public SpriteRenderer torsoRenderer;
    public SpriteRenderer headRenderer;
    public SpriteRenderer hairRenderer;

    [Header("Settings")]
    public float walkFrameRate = 8f;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private float walkTimer;
    private int walkFrame;
    private bool facingRight = true;

    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        bool isGrounded  = movement.isGrounded;
        bool movingLeft  = Keyboard.current.aKey.isPressed;
        bool movingRight = Keyboard.current.dKey.isPressed;
        bool isWalking   = movingLeft || movingRight;
        if (movingRight) facingRight = true;
        else if (movingLeft) facingRight = false;

        transform.localScale = new Vector3(facingRight ? -1f : 1f, 1f, 1f);

        if (isWalking && isGrounded)
        {
            walkTimer += Time.deltaTime;
            if (walkTimer >= 1f / walkFrameRate)
            {
                walkTimer = 0f;
                walkFrame = (walkFrame + 1) % 4;
            }
        }
        else
        {
            walkTimer = 0f;
            walkFrame = 0;
        }

        if (!isGrounded)
        {
            legsRenderer.sprite     = legsJump;
            frontArmRenderer.sprite = frontArmJump;
            backArmRenderer.sprite  = backArmJump;
        }
        else if (isWalking)
        {
            legsRenderer.sprite     = legsWalk[walkFrame];
            frontArmRenderer.sprite = frontArmWalk[walkFrame];
            backArmRenderer.sprite  = backArmWalk[walkFrame];
        }
        else
        {
            legsRenderer.sprite     = legsIdle;
            frontArmRenderer.sprite = frontArmIdle;
            backArmRenderer.sprite  = backArmIdle;
        }
    }
}