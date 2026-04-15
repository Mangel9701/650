using TMPro;
using UnityEngine;

public class DoorNameDisplay : MonoBehaviour
{
    public TextMeshProUGUI doorNameText;
    private string currentDoorName;

    private void OnEnable()
    {
        RefreshDoorName();
    }

    public void UpdateDoorName(string newDoorName)
    {
        currentDoorName = newDoorName;
        RefreshDoorName();
    }

    private void RefreshDoorName()
    {
        if (doorNameText != null)
        {
            doorNameText.text = currentDoorName;
        }
    }
}