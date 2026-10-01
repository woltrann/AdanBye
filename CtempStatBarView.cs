using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Saat (WatchPanel) göstergelerinin tek bileşeni: açlık, susuzluk ve stamina. Hangi stat olduğu Kind ile seçilir,
// veri IStatBarSource'tan gelir. Event yok (değer her karede değişebilir); okuma alloc'suz,
// değişmeyen değer için UI'ye yazılmaz. Image'lara yalnızca fillAmount/color verilir; fillMethod'dan bağımsızdır
// (yarım daire de yatay bar da olur).
public class StatBarView : MonoBehaviour
{
    private const int BindMaxFrames = 120;

    [SerializeField] private StatKind kind;
    [Tooltip("Opsiyonel: Açlık/Susuzluk için normalde PlayerManager.mainCharacter kullanılır; farklı bir asset gerekirse ata.")]
    [SerializeField] private MainCharacter characterOverride;
    [Tooltip("Image Type = Filled. Radial180 (saat yarım dairesi) veya Horizontal olabilir.")]
    [SerializeField] private Image fill;
    [Tooltip("Opsiyonel (yalnız stamina): fill'in ARKASINDA, soluk renkli yorgunluk sınırı Image'ı (Filled, aynı Fill Method/Origin).")]
    [SerializeField] private Image limitFill;
    [Header("Renkler")]
    [SerializeField] private Color lowColor = Color.red;
    [SerializeField] private Color midColor = Color.yellow;
    [SerializeField] private Color highColor = Color.green;
    [Header("Uyarı geri bildirimi (stamina çökmesi)")]
    [Tooltip("Uyarı durumunda gösterge rengi (kırmızıya yakın).")]
    [SerializeField] private Color alertColor = new Color(0.8f, 0.1f, 0.1f, 1f);
    [Tooltip("Opsiyonel: uyarıda alfa titreşimi uygulanacak grup.")]
    [SerializeField] private CanvasGroup alertFeedback;
    [SerializeField] private float alertPulseSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float alertMinAlpha = 0.35f;

    private IStatBarSource source;
    private float shownFill = -1f, shownLimit = -1f;
    private bool shownAlert, colorShown;

    private IEnumerator Start()
    {
        if (fill == null)
            Debug.LogWarning($"[StatBarView:{kind}] fill atanmamış; doluluk gösterilmeyecek.", this);

        // Player/PlayerManager bu UI'dan sonra doğabilir; bulunana kadar kare kare dener (UXobjects ile aynı kalıp).
        for (int i = 0; source == null && i < BindMaxFrames; i++)
        {
            source = StatBarSourceFactory.TryCreate(kind, PlayerManager.Instance, characterOverride);
            if (source == null) yield return null;
        }

        if (source == null)
            Debug.LogWarning($"[StatBarView:{kind}] Veri kaynağı bulunamadı (PlayerManager/MainCharacter/PlayerStamina); gösterge güncellenmeyecek.", this);
    }

    private void Update()
    {
        if (source == null) return;

        float ratio = source.Ratio;
        bool alert = source.IsAlert;
        bool ratioChanged = !Mathf.Approximately(ratio, shownFill);

        if (fill != null)
        {
            if (ratioChanged) fill.fillAmount = ratio;
            // Renk yalnızca oran ya da uyarı durumu değişince yazılır (Image.color set'i grafiği kirletir).
            if (ratioChanged || alert != shownAlert || !colorShown)
            {
                fill.color = alert ? alertColor : StatColorGradient.Evaluate(lowColor, midColor, highColor, ratio);
                colorShown = true;
            }
        }
        shownFill = ratio;
        shownAlert = alert;

        if (limitFill != null)
        {
            float limit = source.LimitRatio;
            if (!Mathf.Approximately(limit, shownLimit))
            {
                limitFill.fillAmount = limit;
                shownLimit = limit;
            }
        }

        if (alertFeedback != null)
        {
            alertFeedback.alpha = alert
                ? Mathf.Lerp(alertMinAlpha, 1f, Mathf.PingPong(Time.unscaledTime * alertPulseSpeed, 1f))
                : 1f;
        }
    }
}
