using UnityEngine;

[CreateAssetMenu(fileName = "MultitileObjectDefinition", menuName = "World/MultitileObjectDefinition")]
public class MultitileObjectDefinition : ScriptableObject
{
    public string displayName;
    public Vector2Int size = Vector2Int.one;
    public Sprite sprite;
    public float hardness = 3f;
    public ToolType requiredTool = ToolType.None;
    public ItemDefinition dropItem;
    public int dropAmount = 1;
    public int sortingOrder = 0;
    public bool hasCollision = true;
}