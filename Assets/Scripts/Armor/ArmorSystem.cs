using System;
using UnityEngine;

public class ArmorSystem : MonoBehaviour
{
    public static ArmorSystem Instance { get; private set; }
    private ArmorDefinition[] _slots = new ArmorDefinition[3];
    public event Action OnArmorChanged;
    public int TotalDefense
    {
        get
        {
            int d = 0;
            foreach (var s in _slots) if (s != null) d += s.defense;
            return d;
        }
    }
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }
    public ArmorDefinition GetSlot(ArmorSlot slot) => _slots[(int)slot];
    public T GetSlot<T>(ArmorSlot slot) where T : ArmorDefinition => _slots[(int)slot] as T;
    public ArmorDefinition Equip(ArmorSlot slot, ArmorDefinition armor)
    {
        var prev = _slots[(int)slot];
        _slots[(int)slot] = armor;
        OnArmorChanged?.Invoke();
        return prev;
    }
    public ArmorDefinition Unequip(ArmorSlot slot) => Equip(slot, null);
}