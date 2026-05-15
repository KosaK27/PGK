using UnityEngine;
using UnityEngine.Tilemaps;

public class LightingMaterialController : MonoBehaviour
{
    public static LightingMaterialController Instance { get; private set; }

    [Header("Material bazowy z shaderem TilemapLighting")]
    [SerializeField] private Material baseLightingMaterial;
    [SerializeField] private float refreshInterval = 0.5f;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private float _timer;

    void Awake()
    {
        Instance = this;
        _mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        RefreshRenderers();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= refreshInterval)
        {
            _timer = 0f;
            RefreshRenderers();
        }
    }

    public void RefreshRenderers()
    {
        _renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (var r in _renderers)
        {
            if (r is TilemapRenderer || r is SpriteRenderer)
            {
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
        }
    }
}