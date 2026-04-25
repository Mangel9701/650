using System.IO;
using UnityEditor;
using UnityEngine;

public static class InteractionConfigJsonExporter
{
    [MenuItem("Tools/Interactions/Export Config To JSON")]
    public static void ExportSelectedConfigToJson()
    {
        InteractionConfigSO config = Selection.activeObject as InteractionConfigSO;

        if (config == null)
        {
            EditorUtility.DisplayDialog(
                "Exportar JSON",
                "Selecciona un InteractionConfigSO en el Project antes de exportar.",
                "OK"
            );
            return;
        }

        ExportConfigToJson(config);
    }

    public static void ExportConfigToJson(InteractionConfigSO config)
    {
        if (config == null)
        {
            EditorUtility.DisplayDialog(
                "Exportar JSON",
                "No hay InteractionConfigSO asignado.",
                "OK"
            );
            return;
        }

        InteractionConfigDto dto = config.ToDto();
        string json = JsonUtility.ToJson(dto, true);

        string path = EditorUtility.SaveFilePanel(
            "Guardar JSON",
            Application.dataPath,
            "interaction-config.json",
            "json"
        );

        if (string.IsNullOrEmpty(path))
            return;

        File.WriteAllText(path, json);
        AssetDatabase.Refresh();

        Debug.Log($"JSON exportado en: {path}");
    }
}