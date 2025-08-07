using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CharacterStatType
{
    Health,
    Hunger,
    Thirst,
    Poison,
    Weight,
    Head,
    Torso,
    Arms,
    Legs
}

[RequireComponent(typeof(Slider))]
public class SliderColorChanger : MonoBehaviour
{
    [Header("Character Data")]
    public MainCharacter characterData;

    [Header("Which Stat To Track")]
    public CharacterStatType statType;

    [Header("Color Settings")]
    public Color lowColor = Color.red;
    public Color midColor = Color.yellow;
    public Color highColor = Color.green;
    public Color backgroundColor = Color.gray;

    [Header("UI References")]
    public Image fillImage;
    public Image backgroundImage;
    public TextMeshProUGUI valueText;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();

        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;
    }

    void Update()
    {
        if (characterData == null) return;

        float current = 0;
        float max = 100;

        switch (statType)
        {
            case CharacterStatType.Health:
                current = characterData.currentHealth;
                max = characterData.maxHealth;
                break;
            case CharacterStatType.Hunger:
                current = characterData.currentHunger;
                max = characterData.maxHunger;
                break;
            case CharacterStatType.Thirst:
                current = characterData.currentThirst;
                max = characterData.maxThirst;
                break;
            case CharacterStatType.Poison:
                current = characterData.currentPoison;
                max = characterData.maxPoison;
                break;
            case CharacterStatType.Weight:
                current = characterData.currentWeight;
                max = characterData.maxWeight;
                break;

            case CharacterStatType.Head:
                current = characterData.headHealth;
                max = characterData.maxHeadHealth;
                break;
            case CharacterStatType.Torso:
                current = characterData.torsoHealth;
                max = characterData.maxTorsoHealth;
                break;
            case CharacterStatType.Arms:
                current = characterData.armsHealth;
                max = characterData.maxArmsHealth;
                break;
            case CharacterStatType.Legs:
                current = characterData.legsHealth;
                max = characterData.maxLegsHealth;
                break;
        }

        slider.maxValue = max;
        slider.value = current;

        UpdateColor(current, max);
        UpdateText(current, max);
    }

    void UpdateColor(float value, float max)
    {
        if (fillImage == null) return;

        float normalized = Mathf.InverseLerp(0, max, value);

        if (normalized < 0.5f)
            fillImage.color = Color.Lerp(lowColor, midColor, normalized * 2f);
        else
            fillImage.color = Color.Lerp(midColor, highColor, (normalized - 0.5f) * 2f);
    }

    void UpdateText(float current, float max)
    {
        if (valueText == null) return;

        valueText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
    }
}
