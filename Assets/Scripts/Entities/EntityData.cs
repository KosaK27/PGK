using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EntityData", menuName = "Entities/EntityData")]
public class EntityData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Enemy";
    public bool isHostile = true;

    [Header("Stats")]
    public int maxHP = 30;
    public int contactDamage = 10;
    public float moveSpeed = 2f;

    [Header("AI")]
    public float detectionRange = 8f;
    public float attackRange = 0.8f;
    public float patrolChangeInterval = 2f;

    [Header("Knockback")]
    public float knockbackResistance = 0f;

    [Header("Spawn Prefab")]
    public GameObject prefab;

    [Header("Drop")]
    public float xpDrop = 10f;
    public DropTable dropTable;

    [Header("Spawn Conditions")]
    public bool spawnInDay = true;
    public bool spawnInNight = false;
    public bool spawnOnSurface = true;
    public bool spawnInCaves = false;

    [Tooltip("Waga losowania tego moba")]
    public float spawnWeight = 1f;

    [Header("Biomes")]
    public List<BiomeType> allowedBiomes = new List<BiomeType>();
}