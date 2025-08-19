using NUnit.Framework.Interfaces;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    public GameObject[] chips;

    private InventoryData inventoryData;

    public ItemData CleanWater;
    public ItemData EmptyBottle;
    public ItemData RiverWater;
    public ItemData WellWater;

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
            case InteractionType.Consumable:
                HandleCollect(itemData); // önce envantere ekle
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
        if (itemData.itemID == 50)//50 campfire, save point
        {
            SaveManager.Instance.SaveGame();
            Debug.Log("Oyun kaydedildi!");
        }

        if (itemData.itemID == 51) //Kuyu objesi
        {
            FillBottleWithWater(EmptyBottle, WellWater);
            return;
        }

        //  Eðer obje Nehir ise
        if (itemData.itemID == 52) //Nehir objesi
        {
            FillBottleWithWater(EmptyBottle, RiverWater);
            return;
        }
    }

    private void HandleConsumable(ItemData itemData)
    {
        var character = GetComponent<PlayerManager>().mainCharacter;

        switch (itemData.consumableType)
        {
            case ConsumableType.Food:
                character.EatFood(itemData.consumableValue);
                Debug.Log($"Yemek yendi: {itemData.itemName}, Hunger +{itemData.consumableValue}");
                break;

            case ConsumableType.Water:
                character.DrinkWater(itemData.consumableValue);
                Debug.Log($"Su içildi: {itemData.itemName}, Thirst +{itemData.consumableValue}");
                break;

            case ConsumableType.Medkit:
                character.HealPart(BodyParts.Torso, itemData.consumableValue);
                Debug.Log($"Medkit kullanýldý: {itemData.itemName}, Health +{itemData.consumableValue}");
                break;

            case ConsumableType.Antidote:
                character.DecreasePoison(itemData.consumableValue);
                Debug.Log($"Panzehir kullanýldý: {itemData.itemName}, Poison -{itemData.consumableValue}");
                break;
        }

        // Envanterden çýkar
        inventoryData.RemoveItem(itemData);
        InventoryUI ui = FindObjectOfType<InventoryUI>();
        ui.RefreshUI();
    }
    public void UseConsumable(ItemData itemData)
    {
        var character = GetComponent<PlayerManager>().mainCharacter;

        switch (itemData.consumableType)
        {
            case ConsumableType.Food:
                character.EatFood(itemData.consumableValue);
                break;
            case ConsumableType.ToksinFood:
                character.EatFood(itemData.consumableValue);
                character.IncreasePoison(itemData.consumableValue/4);
                break;
            case ConsumableType.Water:
                character.DrinkWater(itemData.consumableValue);
                break;
            case ConsumableType.ToksinRiverWater:
                character.DrinkWater(itemData.consumableValue);
                character.IncreasePoison(itemData.consumableValue/2);
                break;
            case ConsumableType.ToksinPoolWater:
                character.DrinkWater(itemData.consumableValue);
                character.IncreasePoison(itemData.consumableValue / 4);
                break;
            case ConsumableType.Medkit:
                character.HealPart(BodyParts.Head, itemData.consumableValue);
                character.HealPart(BodyParts.Torso, itemData.consumableValue);
                character.HealPart(BodyParts.Arms, itemData.consumableValue);
                character.HealPart(BodyParts.Legs, itemData.consumableValue);
                break;
            case ConsumableType.Bandage:
                character.HealLowestPart(itemData.consumableValue);
                break;
            case ConsumableType.Antidote:
                character.DecreasePoison(itemData.consumableValue);
                break;
            case ConsumableType.PoweredHead:
                character.UpgradePart(BodyParts.Head, itemData.consumableValue);
                break;
            case ConsumableType.PoweredTorso:
                character.UpgradePart(BodyParts.Torso, itemData.consumableValue);
                break;
            case ConsumableType.PoweredArm:
                character.UpgradePart(BodyParts.Arms, itemData.consumableValue);
                break;
            case ConsumableType.PoweredLeg:
                character.UpgradePart(BodyParts.Legs, itemData.consumableValue);
                break;
            case ConsumableType.UpgradeWeight:
                inventoryData.MaxWeight += itemData.consumableValue;
                break;
            case ConsumableType.UpgradeSpeed:
                PlayerMovement.Instance.moveSpeed *= 1.5f;
                PlayerMovement.Instance.runSpeed *= 1.5f;
                break;
            case ConsumableType.ToksinMask:
                PlayerMovement.Instance.isOutSide = false;
                break;
            case ConsumableType.WaterCleaner:
                FillBottleWithWater(WellWater, CleanWater);
                FillBottleWithWater(RiverWater, CleanWater);
                break;
            case ConsumableType.Recharger:
                UXobjects.Instance.isRecharge = true;
                break;
        }
        if (itemData.itemAfterUse != null)
        {
            inventoryData.AddItem(itemData.itemAfterUse);
            Debug.Log($"{itemData.itemName} kullanýldý, {itemData.itemAfterUse.itemName} envantere eklendi!");
        }

        // Envanterden sil
        inventoryData.RemoveItem(itemData);
        FindObjectOfType<InventoryUI>().RefreshUIonly();
    }


    private void FillBottleWithWater(ItemData emptyBottle, ItemData filledWater)
    {
        if (inventoryData.items.Contains(emptyBottle))
        {
            // Boþ þiþeyi sil
            inventoryData.RemoveItem(emptyBottle);
            // Dolu þiþeyi ekle
            inventoryData.AddItem(filledWater);

            Debug.Log($"Boþ þiþe {filledWater.itemName} ile dolduruldu!");
            FindObjectOfType<InventoryUI>().RefreshUIonly();
        }
    }

}
