using System;
using UnityEngine;

public class AccessorySystem : MonoBehaviour
{
    public static AccessorySystem Instance { get; private set; }

    [SerializeField] private int slotCount = 5;

    private AccessoryDefinition[] _slots;

    public int SlotCount => slotCount;
    public event Action OnAccessoriesChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _slots = new AccessoryDefinition[slotCount];
    }

    public AccessoryDefinition GetSlot(int index) => index >= 0 && index < slotCount ? _slots[index] : null;

    public AccessoryDefinition Equip(int index, AccessoryDefinition accessory)
    {
        if (index < 0 || index >= slotCount) return null;
        var previous = _slots[index];
        _slots[index] = accessory;
        OnAccessoriesChanged?.Invoke();
        return previous;
    }

    public AccessoryDefinition Unequip(int index) => Equip(index, null);

    public bool HasEffect(AccessoryEffect effect)
    {
        foreach (var slot in _slots)
            if (slot != null && slot.effect == effect) return true;
        return false;
    }
}