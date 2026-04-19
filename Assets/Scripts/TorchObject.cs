using UnityEngine;

public class TorchObject : MultitileObject
{
    public TorchDefinition TorchDef { get; private set; }

    public void InitializeTorch(TorchDefinition def, Vector2Int origin)
    {
        TorchDef = def;
        Initialize(def, origin);
    }

    public void OnEnable()
    {
        
    }

    public void OnDisable()
    {
        
    }
}