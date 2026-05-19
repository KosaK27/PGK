using UnityEngine;

public class TorchObject : MultitileObject
{
    public TorchDefinition TorchDef { get; private set; }
    private LightSource _lightSource;

    public void InitializeTorch(TorchDefinition def, Vector2Int origin)
    {
        TorchDef = def;
        Initialize(def, origin);

        _lightSource = gameObject.AddComponent<LightSource>();
        _lightSource.LightColor = def.lightColor;
        _lightSource.Strength = def.lightStrength;
    }

    void OnDestroy()
    {
        if (LightingSystem.Instance != null && _lightSource != null)
        {
            LightingSystem.Instance.UnregisterSource(_lightSource);
            LightingSystem.Instance.RebuildLightMapAt(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
        }
    }
}