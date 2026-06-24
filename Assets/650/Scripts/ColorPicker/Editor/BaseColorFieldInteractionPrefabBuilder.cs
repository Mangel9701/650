using Studio650.ColorField;
using InteractionSystem;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

namespace Studio650.ColorField.Editor
{
    public static class BaseColorFieldInteractionPrefabBuilder
    {
        private const string GenericInteractionPath = "Assets/_Interaction/_Prefabs/_COMMON/GenericInteraction.prefab";
        private const string PanelPrefabPath = "Assets/650/Prefabs/Panel_BaseColorField.prefab";
        private const string CustomMaterialPath = "Assets/650/Resources/Budget/Materials/M_Sofa_MaterialPersonalizado.mat";
        private const string InteractionPrefabPath = "Assets/650/Prefabs/GenericInteraction_BaseColorField.prefab";

        [MenuItem("650/UI/Build Base Color Field Interaction Prefab")]
        public static void BuildPrefab()
        {
            GameObject genericPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GenericInteractionPath);
            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            Material customMaterial = AssetDatabase.LoadAssetAtPath<Material>(CustomMaterialPath);

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
            MeshRenderer sourceRenderer = instance.GetComponentInChildren<MeshRenderer>();
            if (sourceRenderer != null && customMaterial != null)
                sourceRenderer.sharedMaterial = customMaterial;

            InteractObject interactObject = instance.GetComponent<InteractObject>();
            if (interactObject == null)
                interactObject = instance.AddComponent<InteractObject>();

            interactObject.stopPlayerMovementOnInteract = true;

            MaterialTransferHandler transferHandler = instance.GetComponent<MaterialTransferHandler>();
            if (transferHandler == null)
                transferHandler = instance.AddComponent<MaterialTransferHandler>();

            var serializedTransfer = new SerializedObject(transferHandler);
            serializedTransfer.FindProperty("sourceRenderer").objectReferenceValue = sourceRenderer;
            serializedTransfer.FindProperty("sourceMaterialIndex").intValue = 0;
            SerializedProperty targets = serializedTransfer.FindProperty("targets");
            targets.arraySize = Mathf.Max(1, targets.arraySize);
            serializedTransfer.ApplyModifiedPropertiesWithoutUndo();

            BaseColorFieldPanelOpener opener = instance.GetComponent<BaseColorFieldPanelOpener>();
            if (opener == null)
                opener = instance.AddComponent<BaseColorFieldPanelOpener>();

            var serializedOpener = new SerializedObject(opener);
            serializedOpener.FindProperty("panelPrefab").objectReferenceValue = panelPrefab;
            serializedOpener.FindProperty("targetTransferHandler").objectReferenceValue = transferHandler;
            serializedOpener.ApplyModifiedPropertiesWithoutUndo();

            for (int i = interactObject.onInteract.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(interactObject.onInteract, i);

            UnityEventTools.AddPersistentListener(interactObject.onInteract, transferHandler.TransferMaterial);
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
