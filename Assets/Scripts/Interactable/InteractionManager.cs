using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    private InventoryData inventoryData;

    private void Awake()
    {
        inventoryData = GetComponent<PlayerManager>().mainCharacter.InventoryData;
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
        if (itemData.itemName.Contains("Çip"))
        {
            if (itemData.itemID == 4)
                Debug.Log("4 numaralý çip toplandý  Gizli kapý açýldý!");
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
