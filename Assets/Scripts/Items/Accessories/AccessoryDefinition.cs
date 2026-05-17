using UnityEngine;

public enum AccessoryEffect { None, LeatherBoots, LightningBoots, BatWings }

[CreateAssetMenu(fileName = "AccessoryDefinition", menuName = "Items/AccessoryDefinition")]
public class AccessoryDefinition : ScriptableObject
{
    public string accessoryId;
    public string displayName;
    public Sprite sprite;
    public AccessoryEffect effect;
}