using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractionItemSO))]
public class InteractionItemSOEditor : Editor
{


    public override void OnInspectorGUI()
    {
        InteractionItemSO item = (InteractionItemSO)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vista previa media", EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "Nombre sanitizado",
            string.IsNullOrEmpty(item.GetSanitizedMediaBaseName()) ? "-" : item.GetSanitizedMediaBaseName()
        );

        EditorGUILayout.LabelField(
            "Archivo final",
            string.IsNullOrEmpty(item.GetFinalMediaFileName()) ? "-" : item.GetFinalMediaFileName()
        );

        EditorGUILayout.LabelField("Tipo me dia", item.mediaType.ToString());

        if (GUILayout.Button("Normalizar media"))
        {
            Undo.RecordObject(item, "Normalizar media interacción");
            item.NormalizeMediaFields();
            EditorUtility.SetDirty(item);
        }
    }
}