using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI weightText;

    public void Setup(ItemData data)
    {
        icon.sprite = data.sprite;
        itemNameText.text = data.itemName;
        weightText.text = $"{data.weight} kg";
    }
}
