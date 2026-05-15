#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(DayNightSystem))]
public class DayNightSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var sys = (DayNightSystem)target;
        DrawDefaultInspector();
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Podglad pory dnia", EditorStyles.boldLabel);
        Color skyColor = sys.GetSkyColor();
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true)), skyColor);
        EditorGUILayout.Space(4);
        float t = sys.timeOfDay;
        string phaseLabel;
        if (t < 0.22f) phaseLabel = "Noc";
        else if (t < 0.30f) phaseLabel = "Swit";
        else if (t < 0.70f) phaseLabel = "Dzien";
        else if (t < 0.78f) phaseLabel = "Zmierzch";
        else phaseLabel = "Noc";
        int totalMinutes = Mathf.RoundToInt(t * 24 * 60);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        EditorGUILayout.LabelField($"Pora: {phaseLabel}  Zegar: {hours:D2}:{minutes:D2}", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Polnoc\n00:00")) { sys.timeOfDay = 0.00f; MarkDirty(); }
        if (GUILayout.Button("Swit\n06:00")) { sys.timeOfDay = 0.25f; MarkDirty(); }
        if (GUILayout.Button("Poludnie\n12:00")) { sys.timeOfDay = 0.50f; MarkDirty(); }
        if (GUILayout.Button("Zmierzch\n18:00")) { sys.timeOfDay = 0.75f; MarkDirty(); }
        EditorGUILayout.EndHorizontal();
        if (Application.isPlaying)
            Repaint();
    }
    private void MarkDirty()
    {
        EditorUtility.SetDirty(target);
    }
}
#endif