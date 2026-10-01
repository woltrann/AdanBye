using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Sadece görünüm: stamina doluluğu, tavan işareti ve çökme titreşimi. Durum IStaminaReadout'tan okunur.
// Event yok (değer her karede değişebilir); okuma alloc'suz, değişmeyen değer için UI'ye yazılmaz.
public class StaminaHudView : MonoBehaviour
{
    private const int BindMaxFrames = 120;

    [Tooltip("Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left.")]
    [SerializeField] private Image staminaFill;
    [Tooltip("Barın (fill ile aynı rect) çocuğu; X konumu Ceiling/Max'e göre anchor ile kayar.")]
    [SerializeField] private RectTransform ceilingMarker;
    [Tooltip("Opsiyonel: çökmede alfa titreşimi uygulanacak grup.")]
    [SerializeField] private CanvasGroup collapseFeedback;
    [SerializeField] private float collapsePulseSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float collapseMinAlpha = 0.35f;

    private IStaminaReadout stamina;
    private float shownFill = -1f, shownCeiling = -1f;

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

        float fill = Mathf.Clamp01(stamina.Current / max);
        if (staminaFill != null && !Mathf.Approximately(fill, shownFill))
        {
            staminaFill.fillAmount = fill;
            shownFill = fill;
        }

        float ceiling = Mathf.Clamp01(stamina.Ceiling / max);
        if (ceilingMarker != null && !Mathf.Approximately(ceiling, shownCeiling))
        {
            // Anchor'ı kaydırmak parent genişliğinden bağımsız doğru konumu verir.
            var anchorMin = ceilingMarker.anchorMin;
            var anchorMax = ceilingMarker.anchorMax;
            ceilingMarker.anchorMin = new Vector2(ceiling, anchorMin.y);
            ceilingMarker.anchorMax = new Vector2(ceiling, anchorMax.y);
            var pos = ceilingMarker.anchoredPosition;
            pos.x = 0f;
            ceilingMarker.anchoredPosition = pos;
            shownCeiling = ceiling;
        }

        if (collapseFeedback != null)
        {
            collapseFeedback.alpha = stamina.IsCollapsed
                ? Mathf.Lerp(collapseMinAlpha, 1f, Mathf.PingPong(Time.unscaledTime * collapsePulseSpeed, 1f))
                : 1f;
        }
    }
}
