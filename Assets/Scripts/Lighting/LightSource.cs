using UnityEngine;

public class LightSource : MonoBehaviour
{
    public Color LightColor = Color.white;
    [Min(0f)] public float Strength = 1f;
    
    public Vector2 WorldPosition => transform.position;
    
    private bool _registered;
    private Vector2Int _lastTilePos;

    void OnEnable() => TryRegister();

    void OnDisable()
    {
        if (_registered && LightingSystem.Instance != null)
        {
            LightingSystem.Instance.UnregisterSource(this);
            _registered = false;
        }
    }

    void LateUpdate()
    {
        if (!_registered) return;

        Vector3 pos = transform.position;
        Vector2Int currentTilePos = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));

        if (currentTilePos != _lastTilePos)
        {
            LightingSystem.Instance?.MarkSourceDirty(this);
            _lastTilePos = currentTilePos;
        }
    }

    private void TryRegister()
    {
        if (_registered || LightingSystem.Instance == null) return;
        
        LightingSystem.Instance.RegisterSource(this);
        Vector3 pos = transform.position;
        _lastTilePos = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        _registered = true;
    }
}