using UnityEngine;

[CreateAssetMenu(fileName = "ChestArmor", menuName = "Items/Armor/Chest")]
public class ChestArmorDefinition : ArmorDefinition
{
    public override ArmorSlot Slot => ArmorSlot.Chest;
    public Sprite torsoSprite;
    public Sprite armBIdle;
    public Sprite[] armBWalk = new Sprite[4];
    public Sprite armBJump;
    public Sprite armFIdle;
    public Sprite[] armFWalk = new Sprite[4];
    public Sprite armFJump;
    public Sprite armFToolUp;
    public Sprite armFToolForward;
    public Sprite armFToolDown;
}