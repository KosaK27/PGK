using UnityEngine;

public class FallingBlock : MonoBehaviour
{
    private BlockType _type;
    private Rigidbody2D _rb;
    private bool _landed;

    public void Init(BlockType type)
    {
        _type = type;
        _rb = GetComponent<Rigidbody2D>();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (_landed) return;

        foreach (var c in col.contacts)
        {
            if (c.normal.y > 0.5f)
            {
                _landed = true;
                Land();
                return;
            }
        }
    }

    void Land()
    {
        int wx = Mathf.RoundToInt(transform.position.x - 0.5f);
        int wy = Mathf.RoundToInt(transform.position.y - 0.5f);

        for (int y = wy; y <= wy + 4; y++)
        {
            var current = WorldManager.Instance.GetBlock(wx, y);
            var above = WorldManager.Instance.GetBlock(wx, y + 1);

            if (current == BlockType.Air)
            {
                WorldManager.Instance.PlaceBlock(wx, y, _type);
                GravityBlockSystem.Instance.NotifyNeighbors(wx, y + 1);
                break;
            }
        }

        GravityBlockSystem.Instance.RemoveFalling(this);
        Destroy(gameObject);
    }
}