using UnityEngine;

public class CraftManager : MonoBehaviour
{
    public static CraftManager Instance;
    private InventoryData inventoryData;

    private void Awake()
    {
        Instance = this;
        inventoryData = GameObject.FindWithTag("Player")
            .GetComponent<PlayerManager>().mainCharacter.InventoryData;
    }

    public bool CanCraft(CraftRecipe recipe)
    {
        foreach (var req in recipe.requirements)
        {
            int count = 0;
            foreach (var item in inventoryData.items)
            {
                if (item == req.item)
                    count++;
            }
            if (count < req.amount)
                return false;
        }
        return true;
    }

    public bool Craft(CraftRecipe recipe)
    {
        if (!CanCraft(recipe))
        {
            Debug.LogWarning("Malzemeler eksik!");
            return false;
        }

        // Malzemeleri eksilt
        foreach (var req in recipe.requirements)
        {
            int toRemove = req.amount;
            for (int i = inventoryData.items.Count - 1; i >= 0 && toRemove > 0; i--)
            {
                if (inventoryData.items[i] == req.item)
                {
                    inventoryData.RemoveItem(inventoryData.items[i]);
                    toRemove--;
                }
            }
        }

        // Ürünü ekle
        for (int i = 0; i < recipe.resultAmount; i++)
            inventoryData.AddItem(recipe.resultItem);

        Debug.Log($"{recipe.recipeName} baþarýyla craft edildi!");
        return true;
    }
}
