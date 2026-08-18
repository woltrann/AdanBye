using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    public GameObject[] chips;

    private InventoryData inventoryData;
    public MainCharacter characterData;

    public ItemData CleanWater;
    public ItemData EmptyBottle;
    public ItemData RiverWater;
    public ItemData WellWater;
    public ItemData Apple;
    public ItemData Battery;


    public bool isHaveGES = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        inventoryData = GetComponent<PlayerManager>().mainCharacter.InventoryData;
    }
    private void Update()
    {
        if (isHaveGES)
        {
            if (!DayCycle.Instance.IsNight)
            {
                UXobjects.Instance.isRecharge = true;
            }
            else
            {
                UXobjects.Instance.isRecharge = false;
            }
        }
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
                    UXobjects.Instance.droidRecharge = true;
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
        if (itemData.itemID == 52) //Nehir objesi
        {
            FillBottleWithWater(EmptyBottle, RiverWater);
            return;
        }
        if (itemData.itemID == 53) //Agac
        {
            inventoryData.AddItem(Apple);
        }
        if (itemData.itemID == 54) //sarj istasyonu
        {
            UXobjects.Instance.phoneCharge = 100;
            UXobjects.Instance.phoneChargePercent.text = UXobjects.Instance.phoneCharge.ToString();
            UXobjects.Instance.watchCharge = 100;
            UXobjects.Instance.watchChargePercent.text = UXobjects.Instance.watchCharge.ToString();
            UXobjects.Instance.flashCharge = 100;
            UXobjects.Instance.flashChargePercent.text = UXobjects.Instance.flashCharge.ToString();
            characterData.IncreaseDroidCharge(100f);
        }
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
                character.IncreasePoison(itemData.consumableValue / 4);
                break;
            case ConsumableType.Water:
                character.DrinkWater(itemData.consumableValue);
                break;
            case ConsumableType.ToksinRiverWater:
                character.DrinkWater(itemData.consumableValue);
                character.IncreasePoison(itemData.consumableValue / 2);
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
                PlayerManager.Instance.GetComponent<PlayerMotor>().MultiplySpeed(1.5f);
                break;
            case ConsumableType.ToksinMask:
                UXobjects.Instance.gassFilter = 100;
                UXobjects.Instance.gassFilterPercent.text = UXobjects.Instance.gassFilter.ToString() + "%";
                break;
            case ConsumableType.WaterCleaner:
                FillBottleWithWater(WellWater, CleanWater);
                FillBottleWithWater(RiverWater, CleanWater);
                break;
            case ConsumableType.Recharger:
                isHaveGES = true;
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
    public void ChangeDroidBattery()
    {
        if (inventoryData.items.Contains(Battery))
        {
            characterData.IncreaseDroidCharge(100f);
            inventoryData.RemoveItem(Battery);
        }
    }
}