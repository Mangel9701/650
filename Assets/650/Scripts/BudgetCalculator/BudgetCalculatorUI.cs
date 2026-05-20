using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Studio650.Budget
{
    public class BudgetCalculatorUI : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private BudgetMaterialRowUI rowTemplate;
        [SerializeField] private TMP_Text totalText;

        [Header("Formato")]
        [SerializeField] private string zeroPriceText = "Incluido";

        private readonly List<BudgetMaterialRowUI> rows = new List<BudgetMaterialRowUI>();
        private BudgetCalculatorManager manager;

        private void Awake()
        {
            AutoWireMissingReferences();
            manager = BudgetCalculatorManager.GetOrCreate();
        }

        private void OnEnable()
        {
            if (manager == null)
                manager = BudgetCalculatorManager.GetOrCreate();

            manager.SelectionChanged += Refresh;
            Refresh(manager.GetCurrentSelections(), manager.GetTotalEuros());
        }

        private void OnDisable()
        {
            if (manager != null)
                manager.SelectionChanged -= Refresh;
        }

        private void Refresh(IReadOnlyList<BudgetMaterialSelection> selections, int totalEuros)
        {
            if (rowTemplate == null || rowsRoot == null)
                return;

            EnsureRows(selections.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                bool active = i < selections.Count;
                rows[i].gameObject.SetActive(active);

                if (!active)
                    continue;

                BudgetMaterialSelection selection = selections[i];
                BudgetMaterialOptionSO option = selection.Option;
                rows[i].SetData(
                    option.CategoryDisplayName,
                    option.DisplayName,
                    option.Description,
                    FormatPrice(option.PriceEuros),
                    selection.RuntimeMaterial != null ? selection.RuntimeMaterial : option.Material);
            }

            if (totalText != null)
                totalText.text = FormatTotal(totalEuros);
        }

        private void EnsureRows(int count)
        {
            if (rows.Count == 0)
            {
                foreach (Transform child in rowsRoot)
                {
                    if (!child.name.StartsWith("PanelPlantillaMaterial"))
                        continue;

                    var row = child.GetComponent<BudgetMaterialRowUI>();
                    if (row == null)
                        row = child.gameObject.AddComponent<BudgetMaterialRowUI>();

                    rows.Add(row);
                }
            }

            if (rowTemplate != null && !rows.Contains(rowTemplate))
                rows.Insert(0, rowTemplate);

            while (rows.Count < count)
            {
                var newRow = Instantiate(rowTemplate, rowsRoot);
                newRow.name = rowTemplate.name;
                rows.Add(newRow);
            }
        }

        private void AutoWireMissingReferences()
        {
            if (rowsRoot == null)
            {
                Transform content = FindChildByName(transform, "Panel_ContenedorScroll");
                if (content != null)
                    rowsRoot = content as RectTransform;
            }

            if (rowsRoot != null)
            {
                var layout = rowsRoot.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 8f;
                    layout.childControlHeight = false;
                    layout.childForceExpandHeight = false;
                    layout.childControlWidth = true;
                    layout.childForceExpandWidth = true;
                }
            }

            if (rowTemplate == null && rowsRoot != null)
            {
                rowTemplate = rowsRoot.GetComponentInChildren<BudgetMaterialRowUI>(true);
                if (rowTemplate == null)
                {
                    foreach (Transform child in rowsRoot)
                    {
                        if (!child.name.StartsWith("PanelPlantillaMaterial"))
                            continue;

                        rowTemplate = child.gameObject.AddComponent<BudgetMaterialRowUI>();
                        break;
                    }
                }
            }

            if (totalText == null)
            {
                Transform footer = FindChildByName(transform, "Footer");
                if (footer != null)
                {
                    Transform totalTransform = FindChildByName(footer, "Text (TMP) (2)");
                    if (totalTransform != null)
                        totalText = totalTransform.GetComponent<TMP_Text>();

                    if (totalText == null)
                    {
                        foreach (var text in footer.GetComponentsInChildren<TMP_Text>(true))
                        {
                            if (text.text == "00.00")
                            {
                                totalText = text;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private string FormatPrice(int value)
        {
            if (value <= 0)
                return zeroPriceText;

            return $"+{value.ToString("N0", CultureInfo.InvariantCulture)} \u20AC";
        }

        private string FormatTotal(int value)
        {
            return $"{value.ToString("N0", CultureInfo.InvariantCulture)} \u20AC";
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
