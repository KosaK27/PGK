using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    private Rigidbody2D rb;
    private bool isGrounded;

    private PlayerStats stats;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
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
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
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
        isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;
    }
}