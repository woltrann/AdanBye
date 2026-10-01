using AdanBye.Survival;
using UnityEngine;

// Tek iş: açlık/susuzluğu zamanla düşürmek. Eskiden UXobjects içindeki HungerDecrase/
// ThirstDecrase coroutine'leriydi - bir UI sınıfının oyun durumunu değiştirmesi yanlıştı.
// Saat mantığı IntervalTicker'da (saf, testli); burası sadece Unity'ye bağlayan ince kabuk.
public class PlayerVitalsTicker : MonoBehaviour
{
    [SerializeField] private MainCharacter mainCharacter;

    [Header("Açlık")]
    [SerializeField] private float hungerIntervalSeconds = 7f;
    [SerializeField] private float hungerPerTick = 1f;

    [Header("Susuzluk")]
    [SerializeField] private float thirstIntervalSeconds = 5f;
    [SerializeField] private float thirstPerTick = 1f;

    private IntervalTicker hungerTicker;
    private IntervalTicker thirstTicker;

    private void Awake()
    {
        // PlayerToxinExposure ile aynı yol: mainCharacter'ın tek doğruluk kaynağı PlayerManager.
        if (mainCharacter == null)
        {
            var manager = GetComponent<PlayerManager>();
            if (manager != null) mainCharacter = manager.mainCharacter;
        }

        if (mainCharacter == null)
        {
            Debug.LogWarning("[PlayerVitalsTicker] MainCharacter bulunamadı (PlayerManager.mainCharacter atanmamış). Açlık/susuzluk düşmeyecek.", this);
            enabled = false;
            return;
        }

        // Inspector'da 0/negatif girilirse IntervalTicker exception atmasın.
        hungerTicker = new IntervalTicker(Mathf.Max(0.01f, hungerIntervalSeconds));
        thirstTicker = new IntervalTicker(Mathf.Max(0.01f, thirstIntervalSeconds));
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        int hungerTicks = hungerTicker.Advance(dt);
        if (hungerTicks > 0) mainCharacter.DecreaseHunger(hungerPerTick * hungerTicks);

        int thirstTicks = thirstTicker.Advance(dt);
        if (thirstTicks > 0) mainCharacter.DecreaseThirst(thirstPerTick * thirstTicks);
    }
}
