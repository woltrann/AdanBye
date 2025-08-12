using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FilledChanger : MonoBehaviour
{
    [Header("Character Data")]
    public MainCharacter characterData;

    [Header("Stat Ayarý")]
    public bool isHunger; // true  açlýk, false  susuzluk

    [Header("UI")]
    public Image fillImage;

    [Header("Renk Ayarlarý")]
    public Color lowColor = Color.red;
    public Color midColor = Color.yellow;
    public Color highColor = Color.green;

    void Update()
    {
        if (characterData == null || fillImage == null) return;

        float current, max;

        if (isHunger)
        {
            current = characterData.currentHunger;
            max = characterData.maxHunger;
        }
        else
        {
            current = characterData.currentThirst;
            max = characterData.maxThirst;
        }

        // FillAmount ayarý
        fillImage.fillAmount = current / max;

        // Renk geçiþi
        float normalized = Mathf.InverseLerp(0, max, current);
        if (normalized < 0.5f)
            fillImage.color = Color.Lerp(lowColor, midColor, normalized * 2f);
        else
            fillImage.color = Color.Lerp(midColor, highColor, (normalized - 0.5f) * 2f);

    }
}
