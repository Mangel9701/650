using UnityEngine;
using UnityEngine.Scripting;

namespace Studio650.Budget
{
    [Preserve]
    public class BudgetCalculatorPanelBinder : MonoBehaviour
    {
        private const float ScanInterval = 0.5f;
        private float nextScanTime;

        private void Start()
        {
            BindAvailablePanels();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanTime)
                return;

            nextScanTime = Time.unscaledTime + ScanInterval;
            BindAvailablePanels();
        }

        private void BindAvailablePanels()
        {
            var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (!IsCalculatorPanel(candidate))
                    continue;

                if (candidate.GetComponent<BudgetCalculatorUI>() == null)
                    candidate.gameObject.AddComponent<BudgetCalculatorUI>();
            }
        }

        private static bool IsCalculatorPanel(Transform candidate)
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid())
                return false;

            string objectName = candidate.name;
            if (objectName.StartsWith("Panel_Calculadora") || objectName == "Panel Calculadora")
                return true;

            return FindChildByName(candidate, "Panel_ContenedorScroll") != null &&
                   FindChildByName(candidate, "Footer") != null;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName)
                    return child;

                Transform found = FindChildByName(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
