using Studio650.ColorField;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

namespace Studio650.ColorField.Editor
{
    public static class BaseColorFieldInteractionPrefabBuilder
    {
        private const string GenericInteractionPath = "Assets/_Interaction/_Prefabs/_COMMON/GenericInteraction.prefab";
        private const string PanelPrefabPath = "Assets/650/Prefabs/Panel_BaseColorField.prefab";
        private const string InteractionPrefabPath = "Assets/650/Prefabs/GenericInteraction_BaseColorField.prefab";

        [MenuItem("650/UI/Build Base Color Field Interaction Prefab")]
        public static void BuildPrefab()
        {
            GameObject genericPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GenericInteractionPath);
            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);

            if (genericPrefab == null)
            {
                Debug.LogError($"[BaseColorFieldInteractionPrefabBuilder] No se encontro {GenericInteractionPath}");
                return;
            }

            if (panelPrefab == null)
            {
                Debug.LogError($"[BaseColorFieldInteractionPrefabBuilder] No se encontro {PanelPrefabPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(genericPrefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[BaseColorFieldInteractionPrefabBuilder] No se pudo instanciar GenericInteraction.");
                return;
            }

            instance.name = "GenericInteraction_BaseColorField";

            InteractObject interactObject = instance.GetComponent<InteractObject>();
            if (interactObject == null)
                interactObject = instance.AddComponent<InteractObject>();

            interactObject.stopPlayerMovementOnInteract = true;

            BaseColorFieldPanelOpener opener = instance.GetComponent<BaseColorFieldPanelOpener>();
            if (opener == null)
                opener = instance.AddComponent<BaseColorFieldPanelOpener>();

            var serializedOpener = new SerializedObject(opener);
            serializedOpener.FindProperty("panelPrefab").objectReferenceValue = panelPrefab;
            serializedOpener.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(interactObject.onInteract, opener.OpenPanel);

            AssetDatabase.DeleteAsset(InteractionPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(instance, InteractionPrefabPath, out bool success);
            Object.DestroyImmediate(instance);

            if (success)
                Debug.Log($"[BaseColorFieldInteractionPrefabBuilder] Prefab creado en {InteractionPrefabPath}");
            else
                Debug.LogError($"[BaseColorFieldInteractionPrefabBuilder] No se pudo crear el prefab en {InteractionPrefabPath}");
        }
    }
}
