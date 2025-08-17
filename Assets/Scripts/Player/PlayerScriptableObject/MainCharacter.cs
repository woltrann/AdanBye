using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MainCharacter", menuName = "Character/MainCharacter")]
public class MainCharacter : ScriptableObject
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
    public float headHealth = 100;
    public float maxHeadHealth = 100;
    public float torsoHealth = 100;
    public float maxTorsoHealth = 100;
    public float armsHealth = 100;
    public float maxArmsHealth = 100;
    public float legsHealth = 100;
    public float maxLegsHealth = 100;

    public float currentHealth => Mathf.Clamp(headHealth + torsoHealth + armsHealth + legsHealth, 0, maxHealth);
    public float maxHealth => Mathf.Clamp(maxHeadHealth + maxTorsoHealth + maxArmsHealth + maxLegsHealth, 0, 1000);

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

    #region Body Part Functions
    public void HealPart(BodyParts part, float amount)
    {
        switch (part)
        {
            case BodyParts.Head: headHealth = Mathf.Clamp(headHealth + amount, 0, maxHeadHealth); break;
            case BodyParts.Torso: torsoHealth = Mathf.Clamp(torsoHealth + amount, 0, maxTorsoHealth); break;
            case BodyParts.Arms: armsHealth = Mathf.Clamp(armsHealth + amount, 0, maxArmsHealth); break;
            case BodyParts.Legs: legsHealth = Mathf.Clamp(legsHealth + amount, 0, maxLegsHealth); break;
        }
    }

    public void DamagePart(BodyParts part, float amount)
    {
        switch (part)
        {
            case BodyParts.Head: headHealth = Mathf.Clamp(headHealth - amount, 0, maxHeadHealth); break;
            case BodyParts.Torso: torsoHealth = Mathf.Clamp(torsoHealth - amount, 0, maxTorsoHealth); break;
            case BodyParts.Arms: armsHealth = Mathf.Clamp(armsHealth - amount, 0, maxArmsHealth); break;
            case BodyParts.Legs: legsHealth = Mathf.Clamp(legsHealth - amount, 0, maxLegsHealth); break;
        }
    }
    public void UpgradePart(BodyParts part, float amount)
    {
        switch (part)
        {
            case BodyParts.Head: maxHeadHealth += amount; headHealth = Mathf.Clamp(headHealth + amount, 0, maxHeadHealth); break;
            case BodyParts.Torso: maxTorsoHealth += amount; torsoHealth = Mathf.Clamp(torsoHealth + amount, 0, maxTorsoHealth); break;
            case BodyParts.Arms: maxArmsHealth += amount; armsHealth = Mathf.Clamp(armsHealth + amount, 0, maxArmsHealth); break;
            case BodyParts.Legs: maxLegsHealth += amount; legsHealth = Mathf.Clamp(legsHealth + amount, 0, maxLegsHealth); break;
        }
    }
    public void IncreaseMaxHealth(float amount)
    {
        maxHeadHealth = Mathf.Clamp(maxHeadHealth + amount, 0, 250); 
        maxTorsoHealth = Mathf.Clamp(maxTorsoHealth + amount, 0, 250); 
        maxArmsHealth = Mathf.Clamp(maxArmsHealth + amount, 0, 250); 
        maxLegsHealth = Mathf.Clamp(maxLegsHealth + amount, 0, 250);
        headHealth = Mathf.Clamp(headHealth + amount, 0, maxHeadHealth);
        torsoHealth = Mathf.Clamp(torsoHealth + amount, 0, maxTorsoHealth); 
        armsHealth = Mathf.Clamp(armsHealth + amount, 0, maxArmsHealth); 
        legsHealth = Mathf.Clamp(legsHealth + amount, 0, maxLegsHealth); 
    }
    public void Injured(BodyParts part, float amount)
    {
        switch (part)
        {
            case BodyParts.Head:
                maxHeadHealth = Mathf.Clamp(maxHeadHealth - amount, 0, 250);
                headHealth = Mathf.Clamp(headHealth - amount*2, 0, maxHeadHealth); break;
            case BodyParts.Torso:
                maxTorsoHealth = Mathf.Clamp(maxTorsoHealth - amount, 0, 250);
                torsoHealth = Mathf.Clamp(torsoHealth - amount*2, 0, maxTorsoHealth); break;
            case BodyParts.Arms:
                maxArmsHealth = Mathf.Clamp(maxArmsHealth - amount, 0, 250);
                armsHealth = Mathf.Clamp(armsHealth - amount*2, 0, maxArmsHealth); break;
            case BodyParts.Legs:
                maxLegsHealth = Mathf.Clamp(maxLegsHealth - amount, 0, 250);
                legsHealth = Mathf.Clamp(legsHealth - amount*2, 0, maxLegsHealth); break;
        }
    }
    public void OverTimeInjured(BodyParts part, float amount)
    {
        switch (part)
        {
            case BodyParts.Head:
                maxHeadHealth = Mathf.Clamp(maxHeadHealth + amount, 0, 250);
                headHealth = Mathf.Clamp(headHealth + amount, 0, maxHeadHealth); break;
            case BodyParts.Torso:
                maxTorsoHealth = Mathf.Clamp(maxTorsoHealth + amount, 0, 250);
                torsoHealth = Mathf.Clamp(torsoHealth + amount, 0, maxTorsoHealth); break;
            case BodyParts.Arms:
                maxArmsHealth = Mathf.Clamp(maxArmsHealth + amount, 0, 250);
                armsHealth = Mathf.Clamp(armsHealth + amount, 0, maxArmsHealth); break;
            case BodyParts.Legs:
                maxLegsHealth = Mathf.Clamp(maxLegsHealth + amount, 0, 250);
                legsHealth = Mathf.Clamp(legsHealth + amount, 0, maxLegsHealth); break;
        }
    }

    public void HealLowestPart(float amount)
    {
        float[] healths = { headHealth, torsoHealth, armsHealth, legsHealth };
        float[] maxHealths = { maxHeadHealth, maxTorsoHealth, maxArmsHealth, maxLegsHealth };

        float lowestPercent = 1f;
        List<int> lowestIndexes = new List<int>();

        // önce en düþük oraný bul
        for (int i = 0; i < healths.Length; i++)
        {
            float percent = healths[i] / maxHealths[i];
            if (percent < lowestPercent)
            {
                lowestPercent = percent;
                lowestIndexes.Clear();
                lowestIndexes.Add(i);
            }
            else if (Mathf.Approximately(percent, lowestPercent)) // eþitse ekle
            {
                lowestIndexes.Add(i);
            }
        }

        if (lowestIndexes.Count > 0)
        {
            int chosen = lowestIndexes[Random.Range(0, lowestIndexes.Count)];
            HealPart((BodyParts)chosen, amount);
            Debug.Log($"[{(BodyParts)chosen}] {amount} iyileþtirildi!");
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