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
        // Limit kontrolü
        if (recipe.craftOnce && PlayerPrefs.GetInt($"Crafted_{recipe.recipeName}", 0) == 1)
            return false;

        if (recipe.maxCraftCount > 0)
        {
            int craftedCount = PlayerPrefs.GetInt($"CraftedCount_{recipe.recipeName}", 0);
            if (craftedCount >= recipe.maxCraftCount)
                return false;
        }

        // Malzeme kontrolü
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
        // Eðer sýnýrlý craft ise daha önce yapýldý mý kontrol et
        if (recipe.craftOnce && PlayerPrefs.GetInt($"Crafted_{recipe.recipeName}", 0) == 1)
        {
            Debug.LogWarning($"{recipe.recipeName} sadece bir kere craftlanabilir!");
            return false;
        }

        if (recipe.maxCraftCount > 0)
        {
            int craftedCount = PlayerPrefs.GetInt($"CraftedCount_{recipe.recipeName}", 0);
            if (craftedCount >= recipe.maxCraftCount)
            {
                Debug.LogWarning($"{recipe.recipeName} maksimum {recipe.maxCraftCount} kere craftlanabilir!");
                return false;
            }
        }

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

        // Limit bilgisi kaydet
        if (recipe.craftOnce)
            PlayerPrefs.SetInt($"Crafted_{recipe.recipeName}", 1);

        if (recipe.maxCraftCount > 0)
        {
            int craftedCount = PlayerPrefs.GetInt($"CraftedCount_{recipe.recipeName}", 0);
            PlayerPrefs.SetInt($"CraftedCount_{recipe.recipeName}", craftedCount + 1);
        }

        return true;
    }

}
