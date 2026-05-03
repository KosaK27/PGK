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

    void OnEnable() { }
    void OnDisable() { }
}