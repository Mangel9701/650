using Unity.VisualScripting;
using UnityEngine;

namespace InteractionSystem.VisualScripting
{
    [UnitCategory("Interaction")]
    [UnitTitle("Transfer Material")]
    [UnitSubtitle("Transfer instantiated material between renderers")]
    public class TransferMaterialUnit : Unit
    {
        [DoNotSerialize]
        public ControlInput inputTrigger;

        [DoNotSerialize]
        public ControlOutput outputTrigger;

        [DoNotSerialize]
        public ValueInput sourceRenderer;

        [DoNotSerialize]
        public ValueInput sourceIndex;

        [DoNotSerialize]
        public ValueInput targetRenderer;

        [DoNotSerialize]
        public ValueInput targetIndex;

        protected override void Definition()
        {
            inputTrigger = ControlInput("Enter", (flow) =>
            {
                MeshRenderer source = flow.GetValue<MeshRenderer>(sourceRenderer);
                int sIndex = flow.GetValue<int>(sourceIndex);
                MeshRenderer target = flow.GetValue<MeshRenderer>(targetRenderer);
                int tIndex = flow.GetValue<int>(targetIndex);

                if (source != null && target != null)
                {
                    Material[] sourceMats = source.materials;
                    Material[] targetMats = target.materials;

                    if (sIndex >= 0 && sIndex < sourceMats.Length && tIndex >= 0 && tIndex < targetMats.Length)
                    {
                        targetMats[tIndex] = sourceMats[sIndex];
                        target.materials = targetMats;
                    }
                    else
                    {
                        Debug.LogWarning($"[TransferMaterialUnit] Index out of bounds. Source: {sIndex}/{sourceMats.Length}, Target: {tIndex}/{targetMats.Length}");
                    }
                }

                return outputTrigger;
            });

            outputTrigger = ControlOutput("Exit");

            sourceRenderer = ValueInput<MeshRenderer>("Source Renderer", null);
            sourceIndex = ValueInput<int>("Source Index", 0);
            targetRenderer = ValueInput<MeshRenderer>("Target Renderer", null);
            targetIndex = ValueInput<int>("Target Index", 0);
        }
    }
}
