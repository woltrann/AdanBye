using System.Collections.Generic;
using UnityEngine;

// Tek bir vücut parçasının can durumu ve davranışları.
[System.Serializable]
public class BodyPartHealth
{
    public float current = 100f;
    public float max = 100f;

    public float Percent => max > 0f ? current / max : 0f;

    public void Heal(float amount) => current = Mathf.Clamp(current + amount, 0, max);

    public void Damage(float amount) => current = Mathf.Clamp(current - amount, 0, max);

    public void UpgradeMax(float amount)
    {
        max += amount;
        current = Mathf.Clamp(current + amount, 0, max);
    }

    public void Injure(float amount)
    {
        max = Mathf.Clamp(max - amount, 0, 250);
        current = Mathf.Clamp(current - amount * 2, 0, max);
    }

    public void OverTimeInjure(float amount)
    {
        max = Mathf.Clamp(max + amount, 0, 250);
        current = Mathf.Clamp(current + amount, 0, max);
    }
}

[CreateAssetMenu(fileName = "MainCharacter", menuName = "Character/MainCharacter")]
public class MainCharacter : ScriptableObject, ISaveable
{
    [Header("Level")]
    public int level;

    [Header("Inventory")]
    public InventoryData InventoryData;

    [Header("Dialogues")]
    public List<DialogueData> RandomDialogues;
    public List<DialogueData> LowHealthDialogues;
    public List<DialogueData> LowStaminaDialogues;
    public List<DialogueData> HighHungerDialogues;
    public List<DialogueData> HighThirstyDialogues;
    public List<DialogueData> HighToxicityDialogues;
    public List<DialogueData> ScenerioDialogues;
    public List<DialogueData> ExamineDialogues;

    [Header("Body Part Health")]
    public BodyPartHealth head = new BodyPartHealth();
    public BodyPartHealth torso = new BodyPartHealth();
    public BodyPartHealth arms = new BodyPartHealth();
    public BodyPartHealth legs = new BodyPartHealth();

    public float currentHealth => Mathf.Clamp(head.current + torso.current + arms.current + legs.current, 0, maxHealth);
    public float maxHealth => Mathf.Clamp(head.max + torso.max + arms.max + legs.max, 0, 1000);

    [Header("Hunger Stats")]
    public float currentHunger = 100;
    public float maxHunger = 100;

    [Header("Thirst Stats")]
    public float currentThirst = 100;
    public float maxThirst = 100;

    [Header("Poison Stats")]
    public float currentPoison = 0;
    public float maxPoison = 100;

    [Header("Weight Stats")]
    public float currentWeight = 100;
    public float maxWeight = 100;

    [Header("Droid Stats")]
    public float droidCharge = 100;
    public float maxDroidCharge = 100;

    #region Save / Load (ISaveable)

    // Kendi durumunu SaveData'ya yazar. SaveManager bu metodun içeriğini bilmez,
    // sadece çağırır. Buradaki alanlardan biri değişirse SADECE burası güncellenir.
    public void CaptureState(SaveData data)
    {
        data.level = level;

        data.headHealth = head.current;
        data.torsoHealth = torso.current;
        data.armsHealth = arms.current;
        data.legsHealth = legs.current;
        data.maxHeadHealth = head.max;
        data.maxTorsoHealth = torso.max;
        data.maxArmsHealth = arms.max;
        data.maxLegsHealth = legs.max;

        data.currentHunger = currentHunger;
        data.currentThirst = currentThirst;
        data.currentPoison = currentPoison;
        data.maxHunger = maxHunger;
        data.maxThirst = maxThirst;
        data.maxPoison = maxPoison;
        data.currentWeight = currentWeight;
        data.maxWeight = maxWeight;
        data.droidCharge = droidCharge;
        data.maxDroidCharge = maxDroidCharge;

        data.inventoryItemIDs = new List<int>();
        if (InventoryData != null && InventoryData.items != null)
        {
            foreach (var item in InventoryData.items)
            {
                if (item != null) data.inventoryItemIDs.Add(item.itemID);
            }
        }
    }

    // SaveData'dan kendi durumunu geri yükler.
    public void RestoreState(SaveData data)
    {
        level = data.level;

        head.current = data.headHealth;
        torso.current = data.torsoHealth;
        arms.current = data.armsHealth;
        legs.current = data.legsHealth;
        head.max = data.maxHeadHealth;
        torso.max = data.maxTorsoHealth;
        arms.max = data.maxArmsHealth;
        legs.max = data.maxLegsHealth;

        currentHunger = data.currentHunger;
        currentThirst = data.currentThirst;
        currentPoison = data.currentPoison;
        maxHunger = data.maxHunger;
        maxThirst = data.maxThirst;
        maxPoison = data.maxPoison;
        currentWeight = data.currentWeight;
        maxWeight = data.maxWeight;
        droidCharge = data.droidCharge;
        maxDroidCharge = data.maxDroidCharge;

        if (InventoryData != null)
        {
            InventoryData.items.Clear();

            if (data.inventoryItemIDs != null)
            {
                foreach (var id in data.inventoryItemIDs)
                {
                    ItemData item = ItemDatabase.GetItemById(id);
                    if (item != null)
                    {
                        InventoryData.items.Add(item);
                        Debug.Log($"Loaded item: {item.name} (ID: {id})");
                    }
                    else
                    {
                        Debug.LogWarning($"Item with ID {id} not found in database!");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("InventoryData is null!");
        }
    }

    #endregion

    #region Body Part Functions

    private BodyPartHealth GetPart(BodyParts part)
    {
        switch (part)
        {
            case BodyParts.Head: return head;
            case BodyParts.Torso: return torso;
            case BodyParts.Arms: return arms;
            case BodyParts.Legs: return legs;
            default: return null;
        }
    }

    public void HealPart(BodyParts part, float amount) => GetPart(part)?.Heal(amount);
    public void DamagePart(BodyParts part, float amount) => GetPart(part)?.Damage(amount);
    public void UpgradePart(BodyParts part, float amount) => GetPart(part)?.UpgradeMax(amount);
    public void Injured(BodyParts part, float amount) => GetPart(part)?.Injure(amount);
    public void OverTimeInjured(BodyParts part, float amount) => GetPart(part)?.OverTimeInjure(amount);

    public void IncreaseMaxHealth(float amount)
    {
        head.UpgradeMax(amount);
        torso.UpgradeMax(amount);
        arms.UpgradeMax(amount);
        legs.UpgradeMax(amount);
    }

    public void HealLowestPart(float amount)
    {
        var parts = new[] { head, torso, arms, legs };
        var partNames = new[] { BodyParts.Head, BodyParts.Torso, BodyParts.Arms, BodyParts.Legs };

        float lowestPercent = 1f;
        List<int> lowestIndexes = new List<int>();

        for (int i = 0; i < parts.Length; i++)
        {
            float percent = parts[i].Percent;
            if (percent < lowestPercent)
            {
                lowestPercent = percent;
                lowestIndexes.Clear();
                lowestIndexes.Add(i);
            }
            else if (Mathf.Approximately(percent, lowestPercent))
            {
                lowestIndexes.Add(i);
            }
        }

        if (lowestIndexes.Count > 0)
        {
            int chosen = lowestIndexes[Random.Range(0, lowestIndexes.Count)];
            parts[chosen].Heal(amount);
            Debug.Log($"[{partNames[chosen]}] {amount} iyileştirildi!");
        }
    }

    #endregion

    #region Hunger Functions
    public void DecreaseHunger(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger - amount, 0, maxHunger);
    }

    public void EatFood(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);
    }

    public void IncreaseMaxHunger(float amount)
    {
        maxHunger += amount;
        maxHunger = Mathf.Clamp(maxHunger, 0, 200);
        currentHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);
    }
    #endregion

    #region Thirst Functions
    public void DecreaseThirst(float amount)
    {
        currentThirst = Mathf.Clamp(currentThirst - amount, 0, maxThirst);
    }

    public void DrinkWater(float amount)
    {
        currentThirst = Mathf.Clamp(currentThirst + amount, 0, maxThirst);
    }

    public void IncreaseMaxThirst(float amount)
    {
        maxThirst += amount;
        maxThirst = Mathf.Clamp(maxThirst, 0, 200);
        currentThirst = Mathf.Clamp(currentThirst + amount, 0, maxThirst);
    }
    #endregion

    #region Poison Functions
    public void IncreasePoison(float amount)
    {
        currentPoison = Mathf.Clamp(currentPoison + amount, 0, maxPoison);
    }
    public void DecreasePoison(float amount)
    {
        currentPoison = Mathf.Clamp(currentPoison - amount, 0, maxPoison);
    }

    public void CurePoison()
    {
        currentPoison = 0;
    }
    #endregion

    #region Weight Functions
    public void IncreaseWeight(float amount)
    {
        currentWeight = Mathf.Clamp(currentWeight + amount, 0, maxWeight);
    }

    public void DecreaseWeight(float amount)
    {
        currentWeight = Mathf.Clamp(currentWeight - amount, 0, maxWeight);
    }

    public void IncreaseMaxWeight(float amount)
    {
        maxWeight += amount;
        maxWeight = Mathf.Clamp(maxWeight, 0, 200);
        currentWeight = Mathf.Clamp(currentWeight + amount, 0, maxWeight);
    }
    #endregion

    #region Droid Charge Functions
    public void IncreaseDroidCharge(float amount)
    {
        droidCharge = Mathf.Clamp(droidCharge + amount, 0, maxDroidCharge);
    }

    public void DecreaseDroidCharge(float amount)
    {
        droidCharge = Mathf.Clamp(droidCharge - amount, 0, maxDroidCharge);
    }
    public void IncreaseMaxDroidCharge(float amount)
    {
        maxDroidCharge += amount;
        maxDroidCharge = Mathf.Clamp(maxDroidCharge, 0, 200);
        droidCharge = Mathf.Clamp(droidCharge + amount, 0, maxDroidCharge);
    }
    #endregion
}

public enum BodyParts
{
    Head,
    Torso,
    Arms,
    Legs
}
