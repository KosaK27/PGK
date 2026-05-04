using System.Collections;
using UnityEngine;

public class BossAttack_Charge : MonoBehaviour
{
    [SerializeField] private float positioningSpeed = 8f;
    [SerializeField] private float waitBeforeCharge = 0.6f;
    [SerializeField] private float chargeSpeed = 18f;
    [SerializeField] private float chargeDistance = 12f;
    [SerializeField] private float sideOffset = 6f;
    [Header("Audio")]
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private AudioSource audioSource;


    public IEnumerator Execute(Transform boss, Transform player, Rigidbody2D rb)
    {
        float side = Mathf.Sign(player.position.x - boss.position.x);
        Vector2 startPos = new Vector2(player.position.x - side * sideOffset, player.position.y);

        while (Vector2.Distance(boss.position, startPos) > 0.2f)
        {
            startPos = new Vector2(player.position.x - side * sideOffset, player.position.y);
            Vector2 dir = (startPos - (Vector2)boss.position).normalized;
            rb.linearVelocity = dir * positioningSpeed;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(waitBeforeCharge);

        if (chargeSound != null) audioSource.PlayOneShot(chargeSound);

        float charged = 0f;
        Vector2 chargeDir = new Vector2(side, 0f);

        while (charged < chargeDistance)
        {
            rb.linearVelocity = chargeDir * chargeSpeed;
            charged += chargeSpeed * Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
    }
}