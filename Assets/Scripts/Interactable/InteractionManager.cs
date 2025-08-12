using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    private void Awake()
    {
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
        Debug.Log($"[Collect] {itemData.itemName} (ID: {itemData.itemID}) toplandý!");
        if (itemData.itemName.Contains("Çip"))
        {
            if (itemData.itemID == 4)
                Debug.Log("4 numaralý çip toplandý  Gizli kapý açýldý!");
        }
    }

    private void HandleExamine(ItemData itemData)
    {
        Debug.Log($"[Examine] {itemData.itemName}  {itemData.description}");
    }

    private void HandleUse(ItemData itemData)
    {
        Debug.Log($"[Use] {itemData.itemName} kullanýldý!");
    }
}
