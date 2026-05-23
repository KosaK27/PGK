using UnityEngine;

[CreateAssetMenu(fileName = "LegsArmor", menuName = "Items/Armor/Legs")]
public class LegsArmorDefinition : ArmorDefinition
{
    public override ArmorSlot Slot => ArmorSlot.Legs;
    public Sprite legsIdle;
    public Sprite[] legsWalk = new Sprite[4];
    public Sprite legsJump;
}