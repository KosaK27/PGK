using UnityEngine;

[CreateAssetMenu(fileName = "HeadArmor", menuName = "Items/Armor/Head")]
public class HeadArmorDefinition : ArmorDefinition
{
    public override ArmorSlot Slot => ArmorSlot.Head;
    public Sprite headSprite;
}