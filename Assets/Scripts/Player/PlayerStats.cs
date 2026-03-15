using UnityEngine;
using System.Collections;
public class PlayerStats : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;
    public Vector3 respawnPoint = new Vector3(100f, 52.5f, 0f);

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Rigidbody2D>().simulated = false;
        GetComponent<PlayerMovement>().enabled = false;

        yield return new WaitForSeconds(3f);

        transform.position = respawnPoint;
        currentHP = maxHP;
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Rigidbody2D>().simulated = true;
        GetComponent<PlayerMovement>().enabled = true;
    }
}