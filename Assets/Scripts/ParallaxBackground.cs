using UnityEngine;
using System.Collections.Generic;

public class ParallaxBackground : MonoBehaviour
{
    public Transform cam;
    public Sprite surfaceSprite;
    public Sprite undergroundSprite;
    public float surfaceThresholdY = -30f;
    public float tileWidth = 60f;
    public float tileHeight = 33.75f;

    public float surfaceParallaxX = 0.1f;
    public float surfaceParallaxY = 0.05f;
    public float undergroundParallaxX = 0.2f;
    public float undergroundParallaxY = 0.2f;

    public float backgroundOffsetY = 0f;

    private Dictionary<Vector2Int, SpriteRenderer> tiles = new Dictionary<Vector2Int, SpriteRenderer>();
    private bool wasUnderground;

    void Start()
    {
        wasUnderground = cam.position.y < surfaceThresholdY;
    }

    void Update()
    {
        bool underground = cam.position.y < surfaceThresholdY;

        if (underground != wasUnderground)
        {
            ClearAll();
            wasUnderground = underground;
        }

        float px = underground ? undergroundParallaxX : surfaceParallaxX;
        float py = underground ? undergroundParallaxY : surfaceParallaxY;

        int centerX = Mathf.RoundToInt(cam.position.x / tileWidth);
        int centerY = underground ? Mathf.RoundToInt(cam.position.y / tileHeight) : 0;

        HashSet<Vector2Int> needed = new HashSet<Vector2Int>();
        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            if (underground)
                for (int y = centerY - 1; y <= centerY + 1; y++)
                    needed.Add(new Vector2Int(x, y));
            else
                needed.Add(new Vector2Int(x, 0));
        }

        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var key in tiles.Keys)
            if (!needed.Contains(key))
                toRemove.Add(key);
        foreach (var key in toRemove)
        {
            Destroy(tiles[key].gameObject);
            tiles.Remove(key);
        }

        foreach (var key in needed)
        {
            if (!tiles.ContainsKey(key))
            {
                GameObject go = new GameObject("Background_" + key.x + "_" + key.y);
                go.transform.SetParent(transform);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = underground ? undergroundSprite : surfaceSprite;
                sr.sortingOrder = -200;
                tiles[key] = sr;
            }
        }

        foreach (var kvp in tiles)
        {
            float wx = cam.position.x + (kvp.Key.x - centerX) * tileWidth - (cam.position.x * px % tileWidth);
            float wy = underground
                ? cam.position.y + (kvp.Key.y - centerY) * tileHeight - (cam.position.y * py % tileHeight) + backgroundOffsetY
                : cam.position.y - (cam.position.y * py) + backgroundOffsetY;

            kvp.Value.transform.position = new Vector3(wx, wy, 10f);
        }
    }

    void ClearAll()
    {
        foreach (var kvp in tiles)
            Destroy(kvp.Value.gameObject);
        tiles.Clear();
    }
}