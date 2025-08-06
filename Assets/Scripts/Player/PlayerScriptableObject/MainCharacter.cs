using UnityEngine;

[CreateAssetMenu(fileName = "MainCharacter", menuName = "Character/MainCharacter")]
public class MainCharacter : ScriptableObject
{
    [Header("Health Stats")]
    [Range (0, 100)]public float currentHealth;
    [Range (100, 500)]public float maxHealth;

    public void UpgradeHealth(float amount)
    {
        maxHealth = Mathf.Min(maxHealth + amount, 500);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }


}
