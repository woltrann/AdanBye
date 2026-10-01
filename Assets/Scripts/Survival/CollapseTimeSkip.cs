using System.Globalization;
using AdanBye.Survival;
using UnityEngine;

// Tek iş: oyuncu bayılınca (stamina çökmesi) oyun saatini 1-2 saat ileri atıp bunu zaman-atlaması alıcılarına bildirmek.
// Bileşen Player üzerinde yoksa özellik kapalıdır (eski davranış). Hesap TimeSkipMath/GameTimeConversion'da (saf, testli).
// Karar: atlama Collapsed olayının geldiği anda, yani çökmenin BAŞLANGICINDA uygulanır ("bayılırsın ve saat ilerler").
public class CollapseTimeSkip : MonoBehaviour
{
    [SerializeField, Min(0f)] private float minHours = 1f;
    [SerializeField, Min(0f)] private float maxHours = 2f;

    private IStaminaReadout stamina;

    private void OnEnable()
    {
        stamina = GetComponent<IStaminaReadout>();
        if (stamina == null)
        {
            Debug.LogWarning("[CollapseTimeSkip] IStaminaReadout (PlayerStamina) bulunamadı; çökmede zaman atlanmayacak.", this);
            return;
        }
        stamina.Collapsed += HandleCollapsed;
    }

    private void OnDisable()
    {
        if (stamina == null) return;
        stamina.Collapsed -= HandleCollapsed;
        stamina = null;
    }

    private void HandleCollapsed()
    {
        var clock = GameClockLocator.Find();
        if (clock == null)
        {
            Debug.LogWarning("[CollapseTimeSkip] IGameClock (DayCycle) bulunamadı; zaman atlanmadı.", this);
            return;
        }

        float gameHours = TimeSkipMath.PickHours(minHours, maxHours, Random.value);
        if (gameHours <= 0f) return;

        float realSeconds = GameTimeConversion.GameHoursToRealSeconds(gameHours, clock.GameHoursPerRealSecond);
        clock.AdvanceHours(gameHours);

        // Bileşen listesi sadece çökmede (nadir) alınır; Update'te allocation yok.
        foreach (var receiver in GetComponents<ITimeSkipReceiver>())
            receiver.OnTimeSkipped(gameHours, realSeconds);

        Debug.Log($"[CollapseTimeSkip] {gameHours.ToString("F1", CultureInfo.InvariantCulture)} oyun saati atlandı", this);
    }
}
