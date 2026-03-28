using UnityEngine;

public enum ToolType { None, Pickaxe, Axe, Shovel, Sword }

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Items/ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite sprite;
    public int maxStack = 99;

    [Header("Block")]
    public bool isBlock;
    public BlockType blockType;

    [Header("Tool")]
    public bool isTool;
    public ToolType toolType;
    public float breakingSpeed = 1f;
}