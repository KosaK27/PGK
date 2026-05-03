using UnityEngine;
using UnityEngine.Tilemaps;

public class LightingMaterialController : MonoBehaviour
{
    [Header("Materia³y z shaderem TilemapLighting")]
    [SerializeField] private Material lightingMaterial;

    private TilemapRenderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    void Start()
    {
        _mpb = new MaterialPropertyBlock();
        RefreshRenderers();
    }

    public void RefreshRenderers()
    {
        _renderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
        foreach (var r in _renderers)
            r.sharedMaterial = lightingMaterial;
    }

    void LateUpdate()
    {
        var lightMap = LightingSystem.Instance?.GetLightMap();
        if (lightMap == null || _renderers == null) return;

        var wm = WorldManager.Instance;
        if (wm == null) return;

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetTexture("_LightMap", lightMap);
            _mpb.SetFloat("_WorldMinX", wm.OffsetX);
            _mpb.SetFloat("_WorldMinY", wm.OffsetY);
            _mpb.SetFloat("_WorldWidth", wm.Data.Width);
            _mpb.SetFloat("_WorldHeight", wm.Data.Height);
            r.SetPropertyBlock(_mpb);
        }
    }
}