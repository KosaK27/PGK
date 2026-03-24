using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    private Rigidbody2D rb;
    public bool isGrounded;

    private float groundedTimer;
    private const float GROUNDED_COOLDOWN = 0.1f;

    private PlayerStats stats;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (groundedTimer > 0)
        {
            groundedTimer -= Time.deltaTime;
            isGrounded = true;
        }

        Move();
        Jump();
        DamageTest();
    }

    void Move()
    {
        float move = 0;

        if (Keyboard.current.aKey.isPressed)
            move = -1;

        if (Keyboard.current.dKey.isPressed)
            move = 1;

        rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);
    }

    void Jump()
    {
        if (Keyboard.current.spaceKey.isPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void DamageTest()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            stats.TakeDamage(50);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                groundedTimer = GROUNDED_COOLDOWN;
                return;
            }
        }
        isGrounded = false;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        groundedTimer = 0f;
        isGrounded = false;
    }
}