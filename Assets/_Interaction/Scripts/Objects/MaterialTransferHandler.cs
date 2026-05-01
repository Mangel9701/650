using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace InteractionSystem
{
    public class MaterialTransferHandler : MonoBehaviour
    {
        [System.Serializable]
        public struct TargetSettings
        {
            [Tooltip("El renderer que recibirá el material.")]
            public MeshRenderer renderer;
            [Tooltip("El índice en la lista de materiales del destino que queremos reemplazar.")]
            public int materialIndex;
        }

        [Header("Configuración de Origen")]
        [Tooltip("El renderer del que se obtendrá el material. Si se deja vacío, buscará en este objeto o sus hijos.")]
        [SerializeField] private MeshRenderer sourceRenderer;
        [Tooltip("El índice del material en el objeto de origen que queremos transferir.")]
        [SerializeField] private int sourceMaterialIndex = 0;

        [Header("Configuración de Destinos")]
        [Tooltip("Lista de objetos y sus respectivos índices que serán actualizados.")]
        [SerializeField] private List<TargetSettings> targets = new List<TargetSettings>();

        [Header("Eventos")]
        public UnityEvent onTransferComplete;

        private void Awake()
        {
            if (sourceRenderer == null)
                sourceRenderer = GetComponentInChildren<MeshRenderer>();
        }

        /// <summary>
        /// Transfiere el material instanciado del origen a todos los destinos configurados.
        /// </summary>
        public void TransferMaterial()
        {
            if (sourceRenderer == null)
            {
                Debug.LogError($"[MaterialTransferHandler] Falta asignar el renderer de Origen en {gameObject.name}", this);
                return;
            }

            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning($"[MaterialTransferHandler] No hay objetivos (targets) definidos en {gameObject.name}", this);
                return;
            }

            // Obtenemos los materiales instanciados del origen una sola vez
            Material[] sourceMaterials = sourceRenderer.materials;
            if (sourceMaterialIndex < 0 || sourceMaterialIndex >= sourceMaterials.Length)
            {
                Debug.LogError($"[MaterialTransferHandler] Índice de origen {sourceMaterialIndex} fuera de rango en {sourceRenderer.name}.", this);
                return;
            }
            Material materialToTransfer = sourceMaterials[sourceMaterialIndex];

            // Iteramos sobre todos los destinos
            foreach (var targetConfig in targets)
            {
                if (targetConfig.renderer == null) continue;

                PerformTransfer(targetConfig.renderer, targetConfig.materialIndex, materialToTransfer);
            }

            onTransferComplete?.Invoke();
        }

        private void PerformTransfer(MeshRenderer target, int index, Material mat)
        {
            Material[] targetMaterials = target.materials;
            if (index < 0 || index >= targetMaterials.Length)
            {
                Debug.LogError($"[MaterialTransferHandler] Índice {index} fuera de rango en {target.name}.", this);
                return;
            }

            targetMaterials[index] = mat;
            target.materials = targetMaterials;

            if (Application.isPlaying)
            {
                Debug.Log($"[MaterialTransferHandler] Transferido a {target.name}[{index}]", this);
            }
        }

        /// <summary>
        /// Añade un nuevo objetivo a la lista dinámicamente.
        /// </summary>
        public void AddTarget(MeshRenderer renderer, int index)
        {
            targets.Add(new TargetSettings { renderer = renderer, materialIndex = index });
        }

        /// <summary>
        /// Limpia la lista de objetivos.
        /// </summary>
        public void ClearTargets() => targets.Clear();
    }
}
