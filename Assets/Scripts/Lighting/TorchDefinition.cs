using UnityEngine;

[CreateAssetMenu(fileName = "TorchDefinition", menuName = "World/TorchDefinition")]
public class TorchDefinition : MultitileObjectDefinition
{
    public Color lightColor = Color.white;
    [Min(0f)] public float lightStrength = 1.5f;
}