using UnityEngine;

[CreateAssetMenu(fileName = "TorchDefinition", menuName = "World/TorchDefinition")]
public class TorchDefinition : MultitileObjectDefinition
{
    [ColorUsage(false, true)]
    public Color lightColor = new Color(1.0f, 1.0f, 1.0f);
    [Range(0f, 1f)]
    public float lightStrength = 1.0f;
}