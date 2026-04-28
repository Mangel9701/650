using UnityEngine;

public class RuntimeItemBinder : MonoBehaviour
{
    [SerializeField] private RuntimeInteractionConfigLoader loader;
    [SerializeField] private ItemDisplay itemDisplay;
    private InteractObject interactObject;

    [Header("Seleccionar interacción")]
    [SerializeField] private string interactionId;

    private void Awake()
    {
        if (loader == null)
            loader = FindObjectOfType<RuntimeInteractionConfigLoader>();
    }

    private void Reset()
    {
        itemDisplay = GetComponent<ItemDisplay>();
        interactObject = GetComponent<InteractObject>();
    }

    private void OnEnable()
    {
        if (loader != null)
            loader.OnConfigLoaded += ApplyConfig;
    }

    private void OnDisable()
    {
        if (loader != null)
            loader.OnConfigLoaded -= ApplyConfig;
    }

    private void Start()
    {
        itemDisplay = GetComponent<ItemDisplay>();
        interactObject = GetComponent<InteractObject>();
        if (loader != null && loader.CurrentConfig != null)
            ApplyConfig(loader.CurrentConfig);
    }

    private void ApplyConfig(RuntimeInteractionConfig config)
    {
        if (config == null || itemDisplay == null)
            return;

        RuntimeInteractionItem selected = config.interactions.Find(x => x.id == interactionId);

        if (selected == null)
        {
            Debug.LogWarning($"No se encontró interacción con id: {interactionId}");
            return;
        }

        if (interactObject != null)
            interactObject.SetEyeOffset(selected.interactivePointPosition);

        itemDisplay.SetRuntimeItem(selected);
    }
}