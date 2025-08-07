using UnityEngine;

[CreateAssetMenu(fileName = "MainCharacter", menuName = "Character/MainCharacter")]
public class MainCharacter : ScriptableObject
{
    [Header("Level")]
    public int level;

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

    #region Body Part Functions
    public void HealPart(string part, float amount)
    {
        switch (part.ToLower())
        {
            case "head": headHealth = Mathf.Clamp(headHealth + amount, 0, maxHeadHealth); break;
            case "torso": torsoHealth = Mathf.Clamp(torsoHealth + amount, 0, maxTorsoHealth); break;
            case "arms": armsHealth = Mathf.Clamp(armsHealth + amount, 0, maxArmsHealth); break;
            case "legs": legsHealth = Mathf.Clamp(legsHealth + amount, 0, maxLegsHealth); break;
        }
    }

    public void DamagePart(string part, float amount)
    {
        switch (part.ToLower())
        {
            case "head": headHealth = Mathf.Clamp(headHealth - amount, 0, maxHeadHealth); break;
            case "torso": torsoHealth = Mathf.Clamp(torsoHealth - amount, 0, maxTorsoHealth); break;
            case "arms": armsHealth = Mathf.Clamp(armsHealth - amount, 0, maxArmsHealth); break;
            case "legs": legsHealth = Mathf.Clamp(legsHealth - amount, 0, maxLegsHealth); break;
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
    public void Injured(string part, float amount)
    {
        switch (part.ToLower())
        {
            case "head":
                maxHeadHealth = Mathf.Clamp(maxHeadHealth - amount, 0, 250);
                headHealth = Mathf.Clamp(headHealth - amount*2, 0, maxHeadHealth); break;
            case "torso":
                maxTorsoHealth = Mathf.Clamp(maxTorsoHealth - amount, 0, 250);
                torsoHealth = Mathf.Clamp(torsoHealth - amount*2, 0, maxTorsoHealth); break;
            case "arms":
                maxArmsHealth = Mathf.Clamp(maxArmsHealth - amount, 0, 250);
                armsHealth = Mathf.Clamp(armsHealth - amount*2, 0, maxArmsHealth); break;
            case "legs":
                maxLegsHealth = Mathf.Clamp(maxLegsHealth - amount, 0, 250);
                legsHealth = Mathf.Clamp(legsHealth - amount*2, 0, maxLegsHealth); break;
        }
    }
    public void OverTimeInjured(string part, float amount)
    {
        switch (part.ToLower())
        {
            case "head":
                maxHeadHealth = Mathf.Clamp(maxHeadHealth + amount, 0, 250);
                headHealth = Mathf.Clamp(headHealth + amount, 0, maxHeadHealth); break;
            case "torso":
                maxTorsoHealth = Mathf.Clamp(maxTorsoHealth + amount, 0, 250);
                torsoHealth = Mathf.Clamp(torsoHealth + amount, 0, maxTorsoHealth); break;
            case "arms":
                maxArmsHealth = Mathf.Clamp(maxArmsHealth + amount, 0, 250);
                armsHealth = Mathf.Clamp(armsHealth + amount, 0, maxArmsHealth); break;
            case "legs":
                maxLegsHealth = Mathf.Clamp(maxLegsHealth + amount, 0, 250);
                legsHealth = Mathf.Clamp(legsHealth + amount, 0, maxLegsHealth); break;
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
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
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
        currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
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
        currentWeight = Mathf.Clamp(currentWeight, 0, maxWeight);
    }
    #endregion
}
