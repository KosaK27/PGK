using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimator : MonoBehaviour
{
    public Sprite idleSprite;
    public Sprite walkSprite1;
    public Sprite walkSprite2;
    public Sprite jumpSprite;
    public Sprite fallSprite;
    public float walkFrameRate = 8f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private PlayerMovement movement;
    private float walkTimer;
    private int walkFrame;
    private bool facingRight = true;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        bool isGrounded = movement.isGrounded;
        bool isWalking = Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed;
        bool isJumping = rb.linearVelocity.y > 0.1f && !isGrounded;
        bool isFalling = rb.linearVelocity.y < -0.1f && !isGrounded;

        // Flip based on input
        if (Keyboard.current.aKey.isPressed)
            facingRight = false;
        else if (Keyboard.current.dKey.isPressed)
            facingRight = true;

        sr.flipX = facingRight;

        if (isJumping)
        {
            sr.sprite = jumpSprite;
        }
        else if (isFalling)
        {
            sr.sprite = fallSprite;
        }
        else if (isWalking)
        {
            walkTimer += Time.deltaTime;
            if (walkTimer >= 1f / walkFrameRate)
            {
                walkTimer = 0;
                walkFrame = (walkFrame + 1) % 2;
            }
            sr.sprite = walkFrame == 0 ? walkSprite1 : walkSprite2;
        }
        else
        {
            sr.sprite = idleSprite;
            walkTimer = 0;
            walkFrame = 0;
        }
    }
}