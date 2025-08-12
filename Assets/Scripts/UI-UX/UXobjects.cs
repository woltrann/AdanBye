using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UXobjects : MonoBehaviour
{
    [Header("Character Data")]
    public MainCharacter characterData;

    [Header("Time")]
    public TextMeshProUGUI timeText;          // Inspector'dan ata
    public DayCycle dayCycle;      // Inspector'dan DayCycle objesini ata

    [Header("HeartBeats")]
    public Image image1;
    public Image image11;
    public Image image2;
    public float duration = 1f; // 0-1 arasý geçiþ süresi

    void Start()
    {
        StartCoroutine(FillLoop());
    }
    void Update()
    {
        if (dayCycle != null && timeText != null)
        {
            timeText.text = dayCycle.GetFormattedTime();
        }
    }


    IEnumerator FillLoop()
    {
        while (true)
        {
            // Saðlýk durumuna göre ayarlarý belirle
            if (characterData.currentHealth == 0f)
            {
                duration = 0.8f;
                image1.gameObject.SetActive(false);
                image11.gameObject.SetActive(true);
            }
            else if (characterData.currentHealth <= characterData.maxHealth/4)
            {
                duration = 0.4f;
                image1.gameObject.SetActive(true);
                image11.gameObject.SetActive(false);
            }
            else
            {
                duration = 0.8f;
                image1.gameObject.SetActive(true);
                image11.gameObject.SetActive(false);
            }

            // Animasyon 1
            Image activeImage = (characterData.currentHealth == 0f) ? image11 : image1;
            activeImage.fillAmount = 0;
            image2.fillAmount = 1;
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / duration;
                activeImage.fillAmount = Mathf.Lerp(0, 1, t);
                yield return null;
            }

            // Animasyon 2
            activeImage.fillAmount = 1;
            t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / duration;
                image2.fillAmount = Mathf.Lerp(1, 0, t);
                yield return null;
            }
        }
    }

}
