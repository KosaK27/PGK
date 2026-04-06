using UnityEngine;

public class CraftingStationObject : MultitileObject
{
    public CraftingStationDefinition StationDefinition { get; private set; }

    public void InitializeStation(CraftingStationDefinition def, Vector2Int origin)
    {
        StationDefinition = def;
        Initialize(def, origin);
    }

    public override void Interact()
    {
        CraftingUIManager.Instance.OpenStation(StationDefinition, this);
    }
}