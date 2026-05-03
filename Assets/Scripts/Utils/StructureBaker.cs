#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class StructureBaker : EditorWindow
{
    private Tilemap blockTilemap;
    private Tilemap wallTilemap;
    private Tilemap objectTilemap;
    private StructureTemplate target;
    private BlockRegistry blockRegistry;
    private WallRegistry wallRegistry;
    private MultitileObjectRegistry objectRegistry;

    [MenuItem("Tools/Structure Baker")]
    public static void Open() => GetWindow<StructureBaker>("Structure Baker");

    void OnGUI()
    {
        GUILayout.Label("Bake Structure from Tilemap", EditorStyles.boldLabel);

        blockTilemap  = (Tilemap)EditorGUILayout.ObjectField("Block Tilemap", blockTilemap, typeof(Tilemap), true);
        blockRegistry = (BlockRegistry)EditorGUILayout.ObjectField("Block Registry", blockRegistry, typeof(BlockRegistry), false);

        EditorGUILayout.Space();
        wallTilemap  = (Tilemap)EditorGUILayout.ObjectField("Wall Tilemap (optional)", wallTilemap, typeof(Tilemap), true);
        wallRegistry = (WallRegistry)EditorGUILayout.ObjectField("Wall Registry (optional)", wallRegistry, typeof(WallRegistry), false);

        EditorGUILayout.Space();
        objectTilemap  = (Tilemap)EditorGUILayout.ObjectField("Object Tilemap (optional)", objectTilemap, typeof(Tilemap), true);
        objectRegistry = (MultitileObjectRegistry)EditorGUILayout.ObjectField("Object Registry (optional)", objectRegistry, typeof(MultitileObjectRegistry), false);

        EditorGUILayout.Space();
        target = (StructureTemplate)EditorGUILayout.ObjectField("Structure Template", target, typeof(StructureTemplate), false);

        if (GUILayout.Button("Bake"))
        {
            if (blockTilemap == null || target == null || blockRegistry == null)
            {
                EditorUtility.DisplayDialog("Error", "Block Tilemap, Block Registry and Structure Template are required.", "OK");
                return;
            }

            blockRegistry.Initialize();
            blockTilemap.CompressBounds();
            var bounds = blockTilemap.cellBounds;
            target.BakeFromTilemap(blockTilemap, blockRegistry, bounds);

            if (wallTilemap != null && wallRegistry != null)
            {
                wallRegistry.Initialize();
                target.BakeWallsFromTilemap(wallTilemap, wallRegistry, bounds);
            }

            if (objectTilemap != null && objectRegistry != null)
            {
                objectRegistry.Initialize();
                target.BakeObjectsFromTilemap(objectTilemap, objectRegistry, bounds);
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Done",
                $"Baked {target.structureName} ({target.size.x}x{target.size.y}) — {target.objects.Count} object(s).", "OK");
        }

        if (target != null)
        {
            EditorGUILayout.Space();
            GUILayout.Label($"Structure size: {target.size.x} x {target.size.y}", EditorStyles.helpBox);
            if (GUILayout.Button("Select StructureTemplate Asset"))
                Selection.activeObject = target;
        }
    }
}
#endif