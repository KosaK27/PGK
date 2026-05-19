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

    [Range(0f, 1f)] public float surfaceDarkenStrength = 0.6f;

    private Dictionary<Vector2Int, SpriteRenderer> tiles = new();
    private bool wasUnderground;
    private bool _isUnderground;

    void Start()
    {
        wasUnderground = cam.position.y < surfaceThresholdY;
        _isUnderground = wasUnderground;
    }

    void LateUpdate()
    {
        _isUnderground = cam.position.y < surfaceThresholdY;

        if (_isUnderground != wasUnderground)
        {
            ClearAll();
            wasUnderground = _isUnderground;
        }

        float px = _isUnderground ? undergroundParallaxX : surfaceParallaxX;
        float py = _isUnderground ? undergroundParallaxY : surfaceParallaxY;

        int centerX = Mathf.RoundToInt(cam.position.x / tileWidth);
        int centerY = _isUnderground ? Mathf.RoundToInt(cam.position.y / tileHeight) : 0;

        HashSet<Vector2Int> needed = new();
        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            if (_isUnderground)
                for (int y = centerY - 1; y <= centerY + 1; y++)
                    needed.Add(new Vector2Int(x, y));
            else
                needed.Add(new Vector2Int(x, 0));
        }

        List<Vector2Int> toRemove = new();
        foreach (var key in tiles.Keys)
            if (!needed.Contains(key))
                toRemove.Add(key);

        foreach (var key in toRemove)
        {
            if (_isUnderground)
                LightingMaterialController.Instance?.UnregisterRenderer(tiles[key]);
            Destroy(tiles[key].gameObject);
            tiles.Remove(key);
        }

        foreach (var key in needed)
        {
            if (!tiles.ContainsKey(key))
            {
                var go = new GameObject("BgTile_" + key.x + "_" + key.y);
                go.transform.SetParent(transform);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _isUnderground ? undergroundSprite : surfaceSprite;
                sr.sortingOrder = -200;
                tiles[key] = sr;

                if (_isUnderground)
                    LightingMaterialController.Instance?.RegisterRenderer(sr, forceForeground: true);
            }
        }

        Color skyTint = Color.white;
        if (!_isUnderground && DayNightSystem.Instance != null)
        {
            Color sky = DayNightSystem.Instance.GetSkyColor();
            float brightness = DayNightSystem.Instance.AmbientBrightness;
            skyTint = Color.Lerp(Color.black, sky, Mathf.Lerp(1f - surfaceDarkenStrength, 1f, brightness));
        }

        foreach (var kvp in tiles)
        {
            float wx = cam.position.x + (kvp.Key.x - centerX) * tileWidth - (cam.position.x * px % tileWidth);
            float wy = _isUnderground
                ? cam.position.y + (kvp.Key.y - centerY) * tileHeight - (cam.position.y * py % tileHeight) + backgroundOffsetY
                : cam.position.y - (cam.position.y * py) + backgroundOffsetY;

            kvp.Value.transform.position = new Vector3(wx, wy, 10f);

            if (!_isUnderground)
                kvp.Value.color = skyTint;
        }
    }

    void ClearAll()
    {
        foreach (var kvp in tiles)
        {
            if (_isUnderground)
                LightingMaterialController.Instance?.UnregisterRenderer(kvp.Value);
            Destroy(kvp.Value.gameObject);
        }
        tiles.Clear();
    }
}