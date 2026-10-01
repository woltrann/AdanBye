using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sadece görünüm: saat metni, kalp atışı animasyonu, bildirim paneli ve şarj yüzdeleri.
// Oyun durumu (şarj/açlık/susuzluk tick'leri) DeviceChargeController ve PlayerVitalsTicker'da;
// burası değerleri yalnızca okuyup gösterir.
public class UXobjects : MonoBehaviour
{
    public static UXobjects Instance;

    // Player'daki DeviceChargeController'a kadar beklenecek en fazla kare (Player geç doğabilir).
    private const int BindMaxFrames = 120;

    [Header("Character Data")]
    public MainCharacter characterData;

    [Header("Time")]
    public TextMeshProUGUI timeText;
    public DayCycle dayCycle;

    [Header("HeartBeats")]
    public Image image1;
    public Image image11;
    public Image image2;
    public float duration = 1f;

    [Header("Other UX")]
    public GameObject NotificationPanel;

    [Header("Charge (yüzde metinleri)")]
    public TextMeshProUGUI phoneChargePercent;
    public TextMeshProUGUI watchChargePercent;
    public TextMeshProUGUI flashChargePercent;
    public TextMeshProUGUI gassFilterPercent;

    // Opsiyonel: boş bırakılırsa PlayerManager.Instance.DeviceCharge üzerinden bulunur.
    [SerializeField] private DeviceChargeController deviceCharge;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (characterData == null && PlayerManager.Instance != null)
            characterData = PlayerManager.Instance.mainCharacter;

        if (characterData != null && image1 != null && image11 != null && image2 != null)
            StartCoroutine(FillLoop());
        else
            Debug.LogWarning("[UXobjects] Kalp atışı için characterData/image1/image11/image2 eksik; animasyon çalışmayacak.", this);

        StartCoroutine(BindDeviceCharge());
    }

    void OnDestroy()
    {
        if (deviceCharge != null) deviceCharge.Changed -= RefreshCharge;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (dayCycle != null && timeText != null)
        {
            timeText.text = dayCycle.GetFormattedTime();
        }
    }

    // Player UXobjects'ten sonra doğabilir; bu yüzden bulunana kadar kare kare dener.
    private IEnumerator BindDeviceCharge()
    {
        for (int i = 0; deviceCharge == null && i < BindMaxFrames; i++)
        {
            if (PlayerManager.Instance != null) deviceCharge = PlayerManager.Instance.DeviceCharge;
            if (deviceCharge == null) yield return null;
        }

        if (deviceCharge == null)
        {
            Debug.LogWarning("[UXobjects] DeviceChargeController bulunamadı; şarj yüzdeleri güncellenmeyecek.", this);
            yield break;
        }

        deviceCharge.Changed += RefreshCharge;
        RefreshCharge();
    }

    private void RefreshCharge()
    {
        SetPercent(phoneChargePercent, deviceCharge.PhoneCharge);
        SetPercent(watchChargePercent, deviceCharge.WatchCharge);
        SetPercent(flashChargePercent, deviceCharge.FlashCharge);
        SetPercent(gassFilterPercent, deviceCharge.GasFilterValue);
    }

    private static void SetPercent(TextMeshProUGUI text, float value)
    {
        if (text != null) text.text = Mathf.RoundToInt(value) + "%";
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
