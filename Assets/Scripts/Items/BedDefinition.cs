using UnityEngine;

public enum NPCType { Merchant, Warrior, Farmer }

[CreateAssetMenu(fileName = "BedDefinition", menuName = "World/BedDefinition")]
public class BedDefinition : MultitileObjectDefinition
{
    [Header("NPC Bed Settings")]
    public NPCType npcType;
}