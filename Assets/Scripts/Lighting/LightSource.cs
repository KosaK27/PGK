using UnityEngine;

public class LightSource : MonoBehaviour
{
    public Color LightColor = Color.white;
    [Min(0f)] public float Strength = 1f;
    public bool isDynamic = false;
    public Vector2 WorldPosition => transform.position;
    private bool _registered = false;

    void OnEnable() => TryRegister();

    void OnDisable()
    {
        if (LightingSystem.Instance != null && _registered)
        {
            LightingSystem.Instance.UnregisterSource(this);
            LightingSystem.Instance.RebuildLightMap();
            _registered = false;
        }
    }

    private void TryRegister()
    {
        if (_registered || LightingSystem.Instance == null) return;
        LightingSystem.Instance.RegisterSource(this);
        if (!isDynamic) LightingSystem.Instance.RebuildLightMap();
        _registered = true;
    }
}