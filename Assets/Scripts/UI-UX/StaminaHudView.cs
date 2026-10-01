using System.Collections;
using AdanBye.Survival;
using UnityEngine;
using UnityEngine.UI;

// Sadece görünüm: stamina doluluğu, yorgunluk sınırı ve çökme geri bildirimi. Durum IStaminaReadout'tan okunur.
// Event yok (değer her karede değişebilir); okuma alloc'suz, değişmeyen değer için UI'ye yazılmaz.
// Image'lara yalnızca fillAmount/color verilir; fillMethod'dan bağımsızdır (yarım daire de yatay bar da olur).
public class StaminaHudView : MonoBehaviour
{
    private const int BindMaxFrames = 120;

    [Tooltip("Image Type = Filled. Radial180 (saat yarım dairesi) veya Horizontal olabilir.")]
    [SerializeField] private Image staminaFill;
    [Tooltip("Opsiyonel: staminaFill'in ARKASINDA, soluk renkli yorgunluk sınırı Image'ı (Filled, aynı Fill Method/Origin).")]
    [SerializeField] private Image ceilingFill;
    [Header("Renkler (saat göstergeleriyle aynı dil)")]
    [SerializeField] private Color lowColor = Color.red;
    [SerializeField] private Color midColor = Color.yellow;
    [SerializeField] private Color highColor = Color.green;
    [Tooltip("Çökmüş durumda gösterge rengi (kırmızıya yakın).")]
    [SerializeField] private Color collapsedColor = new Color(0.8f, 0.1f, 0.1f, 1f);
    [Header("Çökme geri bildirimi")]
    [Tooltip("Opsiyonel: çökmede alfa titreşimi uygulanacak grup.")]
    [SerializeField] private CanvasGroup collapseFeedback;
    [SerializeField] private float collapsePulseSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float collapseMinAlpha = 0.35f;

    private IStaminaReadout stamina;
    private float shownFill = -1f, shownCeiling = -1f;
    private bool shownCollapsed;
    private bool colorShown;

    private IEnumerator Start()
    {
        if (staminaFill == null)
            Debug.LogWarning("[StaminaHudView] staminaFill atanmamış; doluluk gösterilmeyecek.", this);

        // Player bu UI'dan sonra doğabilir; bulunana kadar kare kare dener (UXobjects ile aynı kalıp).
        for (int i = 0; stamina == null && i < BindMaxFrames; i++)
        {
            if (PlayerManager.Instance != null) stamina = PlayerManager.Instance.GetComponent<IStaminaReadout>();
            if (stamina == null) yield return null;
        }

        if (stamina == null)
            Debug.LogWarning("[StaminaHudView] IStaminaReadout (PlayerStamina) bulunamadı; HUD güncellenmeyecek.", this);
    }

    private void Update()
    {
        if (stamina == null) return;

        float max = stamina.Max;
        if (!(max > 0f)) return;

        float fill = StaminaFillMath.Ratio(stamina.Current, max);
        bool collapsed = stamina.IsCollapsed;
        bool fillChanged = !Mathf.Approximately(fill, shownFill);

        if (staminaFill != null)
        {
            if (fillChanged) staminaFill.fillAmount = fill;
            // Renk yalnızca oran ya da çökme durumu değişince yazılır (Image.color set'i grafiği kirletir).
            if (fillChanged || collapsed != shownCollapsed || !colorShown)
            {
                staminaFill.color = collapsed
                    ? collapsedColor
                    : StatColorGradient.Evaluate(lowColor, midColor, highColor, fill);
                colorShown = true;
            }
        }
        shownFill = fill;
        shownCollapsed = collapsed;

        float ceiling = StaminaFillMath.Ratio(stamina.Ceiling, max);
        if (ceilingFill != null && !Mathf.Approximately(ceiling, shownCeiling))
        {
            ceilingFill.fillAmount = ceiling;
            shownCeiling = ceiling;
        }

        if (collapseFeedback != null)
        {
            collapseFeedback.alpha = collapsed
                ? Mathf.Lerp(collapseMinAlpha, 1f, Mathf.PingPong(Time.unscaledTime * collapsePulseSpeed, 1f))
                : 1f;
        }
    }
}
