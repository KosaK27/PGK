using System.Collections.Generic;
using UnityEngine;

public class LightingMaterialController : MonoBehaviour
{
    public static LightingMaterialController Instance { get; private set; }

    [SerializeField] private Material baseLightingMaterial;

    private MaterialPropertyBlock _mpb;
    private readonly List<Renderer> _registeredRenderers = new();
    private int _backgroundLayer;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _mpb = new MaterialPropertyBlock();
        _backgroundLayer = LayerMask.NameToLayer("Background");
    }

    public void RegisterRenderer(Renderer r, bool forceBackground = false, bool forceForeground = false)
    {
        if (r == null) return;
        if (!_registeredRenderers.Contains(r)) _registeredRenderers.Add(r);
        ApplyToRenderer(r, forceBackground, forceForeground);
    }

    public void UnregisterRenderer(Renderer r) => _registeredRenderers.Remove(r);

    private void ApplyToRenderer(Renderer r, bool forceBackground = false, bool forceForeground = false)
    {
        if (r.sharedMaterial != baseLightingMaterial) r.sharedMaterial = baseLightingMaterial;

        bool isBg = false;
        if (!forceForeground)
        {
            if (forceBackground) isBg = true;
            else
            {
                isBg = r.gameObject.layer == _backgroundLayer || 
                       r.gameObject.CompareTag("Background") || 
                       r.sortingLayerName.IndexOf("back", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                       r.gameObject.name.IndexOf("back", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        r.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_IsBackground", isBg ? 1f : 0f);
        r.SetPropertyBlock(_mpb);
    }

    public void RefreshAllRenderers()
    {
        for (int i = _registeredRenderers.Count - 1; i >= 0; i--)
        {
            var r = _registeredRenderers[i];
            if (r == null) _registeredRenderers.RemoveAt(i);
            else ApplyToRenderer(r);
        }
    }
}