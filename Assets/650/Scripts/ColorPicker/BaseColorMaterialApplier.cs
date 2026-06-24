using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;
using InteractionSystem;
using Studio650.Budget;

namespace Studio650.ColorField
{
    [Preserve]
    [DisallowMultipleComponent]
    public class BaseColorMaterialApplier : MonoBehaviour
    {
        [Header("Objetivo de color")]
        [SerializeField] private MaterialTransferHandler targetTransferHandler;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private int materialIndex;

        [Header("Propiedad del shader")]
        [SerializeField] private string colorPropertyName = "_BaseColor";
        [SerializeField] private bool fallbackToColorProperty = true;

        [Header("Eventos")]
        public UnityEvent onApplied = new UnityEvent();

        private Material runtimeMaterial;

        public MaterialTransferHandler TargetTransferHandler
        {
            get => targetTransferHandler;
            set
            {
                targetTransferHandler = value;
                runtimeMaterial = null;
            }
        }

        public Renderer TargetRenderer
        {
            get => targetRenderer;
            set
            {
                targetRenderer = value;
                runtimeMaterial = null;
            }
        }

        public int MaterialIndex
        {
            get => materialIndex;
            set
            {
                materialIndex = value;
                runtimeMaterial = null;
            }
        }

        public string ColorPropertyName
        {
            get => colorPropertyName;
            set => colorPropertyName = string.IsNullOrWhiteSpace(value) ? "_BaseColor" : value;
        }

        public void ApplyRgb255(Vector3 rgb255)
        {
            Color color = new Color(
                Mathf.Clamp01(rgb255.x / 255f),
                Mathf.Clamp01(rgb255.y / 255f),
                Mathf.Clamp01(rgb255.z / 255f),
                1f);

            ApplyColor(color);
        }

        public void ApplyColor(Color color)
        {
            Material material = GetRuntimeMaterial();
            if (material == null)
                return;

            string propertyName = ResolveColorProperty(material);
            if (string.IsNullOrEmpty(propertyName))
            {
                Debug.LogWarning($"[BaseColorMaterialApplier] El material '{material.name}' no tiene la propiedad '{colorPropertyName}'.", this);
                return;
            }

            Color previousColor = material.GetColor(propertyName);
            color.a = previousColor.a;
            material.SetColor(propertyName, color);

            if (targetTransferHandler != null)
                BudgetMaterialTransferBridge.NotifyTransfer(targetTransferHandler);

            onApplied.Invoke();
        }

        public void SetTarget(Renderer renderer)
        {
            targetTransferHandler = null;
            TargetRenderer = renderer;
        }

        public void SetTarget(Renderer renderer, int index)
        {
            targetTransferHandler = null;
            targetRenderer = renderer;
            materialIndex = index;
            runtimeMaterial = null;
        }

        public void SetTarget(MaterialTransferHandler transferHandler)
        {
            targetTransferHandler = transferHandler;
            runtimeMaterial = null;
        }

        private Material GetRuntimeMaterial()
        {
            if (targetTransferHandler != null)
            {
                runtimeMaterial = targetTransferHandler.CurrentSourceMaterial;
                if (runtimeMaterial != null)
                    return runtimeMaterial;
            }

            if (targetRenderer == null)
                return null;

            Material[] materials = targetRenderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                Debug.LogWarning($"[BaseColorMaterialApplier] Indice de material {materialIndex} fuera de rango en '{targetRenderer.name}'.", this);
                return null;
            }

            runtimeMaterial = materials[materialIndex];
            return runtimeMaterial;
        }

        private string ResolveColorProperty(Material material)
        {
            if (!string.IsNullOrWhiteSpace(colorPropertyName) && material.HasColor(colorPropertyName))
                return colorPropertyName;

            if (fallbackToColorProperty && material.HasColor("_Color"))
                return "_Color";

            return null;
        }
    }
}
