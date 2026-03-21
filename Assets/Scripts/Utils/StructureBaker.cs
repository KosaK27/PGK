// Assets/Scripts/Editor/StructureBaker.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class StructureBaker : EditorWindow
{
    private Tilemap tilemap;
    private StructureTemplate target;
    private BlockRegistry registry;

    [MenuItem("Tools/Structure Baker")]
    public static void Open() => GetWindow<StructureBaker>("Structure Baker");

    void OnGUI()
    {
        GUILayout.Label("Zapisz strukturę z Tilemapa", EditorStyles.boldLabel);

        tilemap  = (Tilemap)EditorGUILayout.ObjectField("Tilemap", tilemap, typeof(Tilemap), true);
        registry = (BlockRegistry)EditorGUILayout.ObjectField("Block Registry", registry, typeof(BlockRegistry), false);
        target   = (StructureTemplate)EditorGUILayout.ObjectField("Structure Template", target, typeof(StructureTemplate), false);

        if (GUILayout.Button("Bake zaznaczony obszar"))
        {
            if (tilemap == null || target == null || registry == null)
            {
                EditorUtility.DisplayDialog("Błąd", "Uzupełnij wszystkie pola.", "OK");
                return;
            }

            registry.Initialize();
            tilemap.CompressBounds();
            var bounds = tilemap.cellBounds;
            target.BakeFromTilemap(tilemap, registry, bounds);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Sukces", $"Zapisano strukturę {target.structureName} ({target.size.x}x{target.size.y})", "OK");
        }
    }
}
#endif