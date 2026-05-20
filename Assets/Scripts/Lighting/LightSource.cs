using UnityEngine;

public class LightSource : MonoBehaviour
{
    public Color LightColor = Color.white;
    [Min(0f)] public float Strength = 1f;
    public Vector2 WorldPosition => transform.position;
    private bool _registered = false;
    private Vector2 _lastPosition;

    void OnEnable() => TryRegister();

    void OnDisable()
    {
        if (LightingSystem.Instance != null && _registered)
        {
            LightingSystem.Instance.UnregisterSource(this);
            _registered = false;
        }
    }

    void LateUpdate()
    {
        if (!_registered) return;
        Vector2 pos = transform.position;
        if (pos != _lastPosition)
        {
            LightingSystem.Instance?.MarkSourceDirty(this);
            _lastPosition = pos;
        }
    }

    private void TryRegister()
    {
        if (_registered || LightingSystem.Instance == null) return;
        LightingSystem.Instance.RegisterSource(this);
        _lastPosition = transform.position;
        _registered = true;
    }
}