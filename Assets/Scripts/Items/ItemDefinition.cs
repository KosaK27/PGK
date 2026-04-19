using UnityEngine;

public enum ToolType { None, Pickaxe, Axe, Shovel }
public enum WeaponType { None, Sword, Bow }

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

    [Header("Wall")]
    public bool isWall;
    public WallType wallType;

    [Header("Multitile Object")]
    public bool isMultitileObject;
    public MultitileObjectDefinition multitileObjectDefinition;
    
    [Header("Tool")]
    public bool isTool;
    public ToolType toolType;
    public float breakingSpeed = 1f;

    [Header("Weapon")]
    public bool isWeapon;
    public WeaponType weaponType;
    public int damage = 10;
    public Vector2 hitboxSize = new Vector2(2.5f, 0.6f);
    public float hitboxDistance = 1.2f;

    [Header("Bow")]
    public float projectileSpeed = 14f;
    public float shootCooldown = 0.8f;

    [Header("Hold Positions")]
    public Vector2 holdPositionUp;
    public Vector2 holdPositionForward;
    public Vector2 holdPositionDown;

    public bool IsHandheld => isTool || isWeapon;
}