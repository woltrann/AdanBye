using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UXobjects : MonoBehaviour
{
    public static UXobjects Instance;
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

    [Header("Other UX")]
    public GameObject NotificationPanel;

    [Header ("Charge")]
    public TextMeshProUGUI phoneChargePercent;
    public float phoneCharge = 100f;
    public TextMeshProUGUI watchChargePercent;
    public float watchCharge = 100f;
    public bool isRecharge=false;


    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        StartCoroutine(FillLoop());
        phoneChargePercent.text = phoneCharge.ToString() + "%";
        watchChargePercent.text = watchCharge.ToString() + "%";
        StartCoroutine(PhoneDecharge());
        StartCoroutine(WatchDecharge());
    }
    void Update()
    {
        if (dayCycle != null && timeText != null)
        {
            timeText.text = dayCycle.GetFormattedTime();
        }
    }

    public void NotificationPanelOpen()
    {
        NotificationPanel.SetActive(true);
        StartCoroutine(NotificationPanelClose());
    }
    IEnumerator NotificationPanelClose()
    {
        yield return new WaitForSeconds(1.5f);
        NotificationPanel.SetActive(false);    
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
    IEnumerator PhoneDecharge()
    {
        while (!isRecharge)
        {
            phoneCharge = Mathf.Clamp(watchCharge - 1, 0, 100);
            phoneChargePercent.text = phoneCharge.ToString() + "%";
            yield return new WaitForSeconds(10);
        }
        while (isRecharge)
        {
            phoneCharge = Mathf.Clamp(watchCharge + 1, 0, 100);
            phoneChargePercent.text = phoneCharge.ToString() + "%";
            yield return new WaitForSeconds(10);
        }
    }

    IEnumerator WatchDecharge()
    {
        while (!isRecharge)
        {
            watchCharge = Mathf.Clamp(watchCharge - 1, 0, 100);
            watchChargePercent.text = watchCharge.ToString() + "%";
            yield return new WaitForSeconds(15);
        }
        
        while (isRecharge)
        {
            watchCharge = Mathf.Clamp(watchCharge + 1, 0, 100);
            watchChargePercent.text = watchCharge.ToString() + "%";
            yield return new WaitForSeconds(15);
        }  

    }
}
