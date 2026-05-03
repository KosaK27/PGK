using UnityEngine;

public class LightSource : MonoBehaviour
{
    public Color LightColor = Color.white;
    [Range(0f, 1f)] public float Strength = 1f;
    public Vector2 WorldPosition => transform.position;

    private bool _registered = false;

    void OnEnable()
    {
        TryRegister();
    }

    void Start()
    {
        if (!_registered)
            TryRegister();
    }

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
        if (LightingSystem.Instance != null)
        {
            LightingSystem.Instance.RegisterSource(this);
            LightingSystem.Instance.RebuildLightMap();
            _registered = true;
        }
    }
}