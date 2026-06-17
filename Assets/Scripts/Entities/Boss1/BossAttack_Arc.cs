using System.Collections;
using UnityEngine;

public class BossAttack_Arc : MonoBehaviour
{
    [SerializeField] private float positioningSpeed = 5f;
    [SerializeField] private float sideOffset = 10f;
    [SerializeField] private float arcHeight = 5f;
    [SerializeField] private float arcDuration = 2.5f;
    [SerializeField] private float arcSpeed = 8f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int projectileDamage = 10;
    [SerializeField] private float projectileSpeed = 8f;

    public IEnumerator Execute(Transform boss, Transform player, Rigidbody2D rb)
    {
        Vector2 startPos = new Vector2(player.position.x - sideOffset, player.position.y);

        while (Vector2.Distance(boss.position, startPos) > 0.2f)
        {
            startPos = new Vector2(player.position.x - sideOffset, player.position.y);
            Vector2 dir = (startPos - (Vector2)boss.position).normalized;
            rb.linearVelocity = dir * positioningSpeed;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);

        float elapsed = 0f;
        float[] shootTimes = { arcDuration * 0.25f, arcDuration * 0.5f, arcDuration * 0.75f };
        int shotsFired = 0;

        Vector2 arcStartPos = new Vector2(player.position.x - sideOffset, player.position.y);

        while (elapsed < arcDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / arcDuration);

            Vector2 arcEndPos = new Vector2(player.position.x + sideOffset, player.position.y);
            Vector2 arcMidPos = new Vector2(player.position.x, player.position.y + arcHeight);

            Vector2 targetPos = (1 - t) * (1 - t) * arcStartPos
                              + 2 * (1 - t) * t * arcMidPos
                              + t * t * arcEndPos;

            Vector2 toTarget = targetPos - (Vector2)boss.position;
            float dist = toTarget.magnitude;
            Vector2 moveDir = dist > 0.01f ? toTarget.normalized : Vector2.zero;
            rb.linearVelocity = moveDir * Mathf.Min(arcSpeed, dist / Time.deltaTime);

            while (shotsFired < shootTimes.Length && elapsed >= shootTimes[shotsFired])
            {
                FireProjectile(boss, player);
                shotsFired++;
            }

            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
    }

    private void FireProjectile(Transform boss, Transform player)
    {
        if (projectilePrefab == null) return;

        PlayerAudioManager.Instance?.PlayLightning();

        Vector2 dir = ((Vector2)player.position - (Vector2)boss.position).normalized;
        var go = Instantiate(projectilePrefab, boss.position, Quaternion.identity);
        var proj = go.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(dir, projectileDamage, projectileSpeed, true, false);
    }
}