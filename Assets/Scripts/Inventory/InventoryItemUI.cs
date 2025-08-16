using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI weightText;

    public Button useButton;
    private ItemData currentItem;


    public void Setup(ItemData data)
    {
        currentItem = data;

        icon.sprite = data.icon;
        itemNameText.text = data.itemName;
        weightText.text = $"{data.weight} kg";

        if (data.interactionType == InteractionType.Consumable)
        {
            useButton.gameObject.SetActive(true);
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() =>
            {
                InteractionManager.Instance.UseConsumable(currentItem);
            });
        }
        else
        {
            useButton.gameObject.SetActive(false);
        }
    }
}
