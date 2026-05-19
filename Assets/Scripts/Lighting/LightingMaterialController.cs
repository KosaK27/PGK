using System.Collections.Generic;
using UnityEngine;

public class LightingMaterialController : MonoBehaviour
{
    public static LightingMaterialController Instance { get; private set; }

    [SerializeField] private Material baseLightingMaterial;

    private MaterialPropertyBlock _mpb;
    private List<Renderer> _registeredRenderers = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _mpb = new MaterialPropertyBlock();
    }

    public void RegisterRenderer(Renderer r, bool forceBackground = false, bool forceForeground = false)
    {
        if (r == null) return;
        if (!_registeredRenderers.Contains(r))
            _registeredRenderers.Add(r);
        ApplyToRenderer(r, forceBackground, forceForeground);
    }

    public void UnregisterRenderer(Renderer r)
    {
        _registeredRenderers.Remove(r);
    }

    private void ApplyToRenderer(Renderer r, bool forceBackground = false, bool forceForeground = false)
    {
        if (r.sharedMaterial != baseLightingMaterial)
            r.sharedMaterial = baseLightingMaterial;

        bool isBg;
        if (forceForeground)
            isBg = false;
        else if (forceBackground)
            isBg = true;
        else
            isBg = r.sortingLayerName.ToLower().Contains("back") ||
                   r.gameObject.layer == LayerMask.NameToLayer("Background") ||
                   r.gameObject.CompareTag("Background") ||
                   r.gameObject.name.ToLower().Contains("back");

        r.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_IsBackground", isBg ? 1f : 0f);
        r.SetPropertyBlock(_mpb);
    }

    public void RefreshAllRenderers()
    {
        for (int i = _registeredRenderers.Count - 1; i >= 0; i--)
        {
            if (_registeredRenderers[i] == null)
                _registeredRenderers.RemoveAt(i);
            else
                ApplyToRenderer(_registeredRenderers[i]);
        }
    }
}