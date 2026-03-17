// Assets/Scripts/Items/ItemDefinition.cs
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Items/ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;

    public Sprite sprite;
    public int maxStack = 99;
    public bool isBlock;
    public BlockType blockType;
}