using UnityEngine;
using UnityEngine.Tilemaps;

public class LightingMaterialController : MonoBehaviour
{
    public static LightingMaterialController Instance { get; private set; }

    [Header("Material bazowy z shaderem TilemapLighting")]
    [SerializeField] private Material baseLightingMaterial;

    private MaterialPropertyBlock _mpb;

    void Awake()
    {
        Instance = this;
        _mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        RefreshAllRenderers();
    }

    public void RegisterRenderer(Renderer r)
    {
        if (r == null) return;

        if (r.sharedMaterial != baseLightingMaterial)
        {
            r.sharedMaterial = baseLightingMaterial;
        }

        bool isBg = r.sortingLayerName.ToLower().Contains("back") ||
                    r.gameObject.layer == LayerMask.NameToLayer("Background") ||
                    r.gameObject.CompareTag("Background") ||
                    r.gameObject.name.ToLower().Contains("back");

        r.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_IsBackground", isBg ? 1f : 0f);
        r.SetPropertyBlock(_mpb);
    }

    public void RefreshAllRenderers()
    {
        var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (var r in renderers)
        {
            if (r is TilemapRenderer || r is SpriteRenderer)
            {
                RegisterRenderer(r);
            }
        }
    }
}