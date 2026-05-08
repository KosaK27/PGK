using UnityEngine;

public enum DoorOpenDirection { Left, Right }

[CreateAssetMenu(fileName = "DoorDefinition", menuName = "World/DoorDefinition")]
public class DoorDefinition : MultitileObjectDefinition
{
    public Vector2Int closedSize = new(1, 3);
    public Vector2Int openSize = new(2, 3);
    public Sprite closedSprite;
    public Sprite openSprite;
    public DoorOpenDirection openDirection = DoorOpenDirection.Right;
    [HideInInspector] public string sourceName;
}