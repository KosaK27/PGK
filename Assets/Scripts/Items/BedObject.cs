using UnityEngine;

public class BedObject : MultitileObject
{
    public BedDefinition BedDef { get; private set; }

    public void InitializeBed(BedDefinition def, Vector2Int origin)
    {
        BedDef = def;
        Initialize(def, origin);

        Transform spriteChild = transform.Find("Sprite");
        if (spriteChild != null)
        {
            spriteChild.localPosition = Vector3.zero;
        }
    }
}