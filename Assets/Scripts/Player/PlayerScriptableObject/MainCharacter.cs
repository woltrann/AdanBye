using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "MainCharacter", menuName = "Character/MainCharacter")]
public class MainCharacter : ScriptableObject
{
    [Header("Health Stats")]
    public int level;
    public float currentHealth = 100;
    public float maxHealth = 100;

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

    #region Health Functions
    public void UpgradeHealth(float amount)
    {
        maxHealth = Mathf.Min(maxHealth + amount, 500);
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }
    public void Injured(float amount)
    {
        maxHealth -= amount;
        maxHealth = Mathf.Clamp(maxHealth, 0, 500);
    }
    public void InjuredOverTime(float amount)
    {
        maxHealth += amount;
        maxHealth = Mathf.Clamp(maxHealth, 0, 500);
    }
    #endregion

    #region Hunger Functions
    public void DecreaseHunger(float amount)
    {
        currentHunger -= amount;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
    }

    public void EatFood(float amount)
    {
        currentHunger += amount;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
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
        currentThirst -= amount;
        currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
    }

    public void DrinkWater(float amount)
    {
        currentThirst += amount;
        currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
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
        currentPoison += amount;
        currentPoison = Mathf.Clamp(currentPoison, 0, maxPoison);
    }

    public void DecreasePoison(float amount)
    {
        currentPoison -= amount;
        currentPoison = Mathf.Clamp(currentPoison, 0, maxPoison);
    }

    public void CurePoison()
    {
        currentPoison = 0;
    }
    #endregion

    #region Weight Functions
    public void IncreaseWeight(float amount)
    {
        currentWeight += amount;
        currentWeight = Mathf.Clamp(currentWeight, 0, maxWeight);
    }

    public void DecreaseWeight(float amount)
    {
        currentWeight -= amount;
        currentWeight = Mathf.Clamp(currentWeight, 0, maxWeight);
    }
    public void IncreaseMaxWeight(float amount)
    {
        maxWeight += amount;
        currentWeight = Mathf.Clamp(currentWeight, 0, maxWeight);
    }
    #endregion
}
