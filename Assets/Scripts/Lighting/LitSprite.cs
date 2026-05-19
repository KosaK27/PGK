using UnityEngine;

public class LitSprite : MonoBehaviour
{
    private SpriteRenderer[] _renderers;

    void Start()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in _renderers)
            LightingMaterialController.Instance?.RegisterRenderer(sr);
    }

    void OnDestroy()
    {
        if (LightingMaterialController.Instance == null) return;
        foreach (var sr in _renderers)
            LightingMaterialController.Instance.UnregisterRenderer(sr);
    }
}