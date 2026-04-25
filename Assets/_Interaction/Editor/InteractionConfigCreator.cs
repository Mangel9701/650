using UnityEditor;
using UnityEngine;

public static class InteractionConfigCreator
{
    [MenuItem("Tools/Interactions/Create Interaction Config")]
    public static void CreateConfig()
    {
        var asset = ScriptableObject.CreateInstance<InteractionConfigSO>();
        asset.mediaBaseUrl = "https://tuenlace.com/MediaResources/";

        string path = EditorUtility.SaveFilePanelInProject(
            "Crear Interaction Config",
            "InteractionConfig",
            "asset",
            "Selecciona dónde guardar el asset de configuración."
        );

        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        Debug.Log($"InteractionConfig creado en: {path}");
    }
}