using UnityEngine;

public class ChestObject : MultitileObject
{
    public ContainerData Container { get; private set; }

    private ChestDefinition _chestDef;

    public void InitializeChest(ChestDefinition def, Vector2Int origin)
    {
        _chestDef = def;
        Container = new ContainerData(def.slotCount);
        Initialize(def, origin);
    }

    public override void Interact()
    {
        ContainerUIManager.Instance.OpenContainer(Container, this);
    }
}