using NUnit.Framework.Interfaces;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    public GameObject[] chips;

    private InventoryData inventoryData;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        inventoryData = GetComponent<PlayerManager>().mainCharacter.InventoryData;
    }

    public void Interact(ItemData itemData)
    {
        switch (itemData.interactionType)
        {
            case InteractionType.Collect:
                HandleCollect(itemData);
                break;
            case InteractionType.Examine:
                HandleExamine(itemData);
                break;
            case InteractionType.Use:
                HandleUse(itemData);
                break;
            case InteractionType.Chip:
                HandleChipCollect(itemData);
                break;

        }
    }
    private void HandleChipCollect(ItemData itemData)
    {
        if (itemData.interactionType == InteractionType.Chip)
        {
            switch (itemData.itemID)
            {
                case 0:
                    chips[0].gameObject.SetActive(true);
                    UXobjects.Instance.NotificationPanelOpen();
                    break;
                case 1:
                    chips[1].gameObject.SetActive(true);
                    UXobjects.Instance.NotificationPanelOpen();
                    break;
                case 2:
                    chips[2].gameObject.SetActive(true);
                    UXobjects.Instance.NotificationPanelOpen();
                    break;
                case 3:
                    chips[3].gameObject.SetActive(true);
                    UXobjects.Instance.NotificationPanelOpen();
                    break;
                case 4:
                    chips[4].gameObject.SetActive(true);
                    UXobjects.Instance.NotificationPanelOpen();
                    break;
                case 5:
                    chips[5].gameObject.SetActive(true);
                    UXobjects.Instance.NotificationPanelOpen();
                    break;
            }
            return;
        }
    }
    private void HandleCollect(ItemData itemData)
    {
       

        if (inventoryData.AddItem(itemData))
        {
            Debug.Log($"[Collect] {itemData.itemName} (ID: {itemData.itemID}) toplandý!");
        }
        else
        {
            Debug.LogWarning($"[Collect] {itemData.itemName} (ID: {itemData.itemID}) toplanamadý, envanter dolu!");
            return;
        }
       
    }

    private void HandleExamine(ItemData itemData)
    {
        Debug.Log($"[Examine] {itemData.itemName}  {itemData.description}");
        //Examine Diyaloðu oynatýlacak
    }

    private void HandleUse(ItemData itemData)
    {
        Debug.Log($"[Use] {itemData.itemName} kullanýldý!");
    }
}
