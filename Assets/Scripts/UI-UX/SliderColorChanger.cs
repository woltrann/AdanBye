using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderColorChanger : MonoBehaviour
{
    [Header("Character Data")]
    public MainCharacter characterData;

    [Header("Color Settings")]
    public Color lowColor = Color.red;
    public Color midColor = Color.yellow;
    public Color highColor = Color.green;

    [Header("UI References")]
    public Image fillImage;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();

        if (fillImage == null)
            Debug.LogWarning("Fill Image atanmamýþ!");
    }

    void Update()
    {
        if (characterData == null) return;

        // UI güncelle
        slider.maxValue = characterData.maxHealth;
        slider.value = characterData.currentHealth;

        UpdateColor(slider.value);
    }

    void UpdateColor(float value)
    {
        if (fillImage == null) return;

        float normalized = Mathf.InverseLerp(slider.minValue, slider.maxValue, value);

        if (normalized < 0.5f)
        {
            fillImage.color = Color.Lerp(lowColor, midColor, normalized * 2);
        }
        else
        {
            fillImage.color = Color.Lerp(midColor, highColor, (normalized - 0.5f) * 2);
        }
    }
}
