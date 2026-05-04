using System.Collections;
using UnityEngine;

public class BossAttack_Tornado : MonoBehaviour
{
    [SerializeField] private float positioningSpeed = 5f;
    [SerializeField] private float heightAbovePlayer = 5f;
    [SerializeField] private GameObject tornadoPrefab;
    [SerializeField] private float spawnOffset = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip tornadoSound;
    [SerializeField] private AudioSource audioSource;

    public IEnumerator Execute(Transform boss, Transform player, Rigidbody2D rb)
    {
        Vector2 targetPos = new Vector2(player.position.x, player.position.y + heightAbovePlayer);

        while (Vector2.Distance(boss.position, targetPos) > 0.15f)
        {
            Vector2 next = Vector2.MoveTowards(boss.position, targetPos, positioningSpeed * Time.deltaTime);
            rb.linearVelocity = (next - (Vector2)boss.position) / Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.4f);

        if (tornadoSound != null && audioSource != null)
            audioSource.PlayOneShot(tornadoSound, 5f);

        if (tornadoPrefab != null)
        {
            Vector2 leftSpawn = new Vector2(boss.position.x - spawnOffset, boss.position.y);
            Vector2 rightSpawn = new Vector2(boss.position.x + spawnOffset, boss.position.y);

            var leftTornado = Instantiate(tornadoPrefab, leftSpawn, Quaternion.identity);
            var rightTornado = Instantiate(tornadoPrefab, rightSpawn, Quaternion.identity);

            var leftComp = leftTornado.GetComponent<TornadoProjectile>();
            var rightComp = rightTornado.GetComponent<TornadoProjectile>();

            float targetY = player.position.y;

            if (leftComp != null) leftComp.Init(-1f, targetY);
            if (rightComp != null) rightComp.Init(1f, targetY);
        }

        yield return new WaitForSeconds(0.5f);
    }
}