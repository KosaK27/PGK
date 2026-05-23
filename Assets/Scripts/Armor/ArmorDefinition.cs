using UnityEngine;

public enum ArmorSlot { Head, Chest, Legs }

public abstract class ArmorDefinition : ScriptableObject
{
    public string armorId;
    public string displayName;
    public Sprite sprite;
    public int defense;
    public abstract ArmorSlot Slot { get; }
}