using System;
using AdanBye.Survival;
using UnityEngine;

// Tek iş: telefon/saat/fener/gaz filtresi/droid şarjını zaman içinde yönetmek.
// Eskiden UXobjects coroutine'leri + public alanlarıydı; artık UI sadece Changed event'ini
// dinleyip değerleri okur. Hesap ChargeableDevice/GasFilter/IntervalTicker'da (saf, testli).
public class DeviceChargeController : MonoBehaviour
{
    [SerializeField] private MainCharacter mainCharacter;

    // Rakamlar "1 birim kaç saniyede" biçiminde: eski coroutine bekleme süreleriyle birebir.
    [Header("Telefon")]
    [SerializeField] private float phoneDrainSeconds = 10f;
    [SerializeField] private float phoneChargeSeconds = 10f;

    [Header("Saat")]
    [SerializeField] private float watchDrainSeconds = 15f;
    [SerializeField] private float watchChargeSeconds = 15f;

    [Header("Fener (sadece açıkken azalır, şarj olmaz)")]
    [SerializeField] private float flashDrainSeconds = 7f;

    [Header("Droid")]
    [SerializeField] private float droidIntervalSeconds = 15f;
    [SerializeField] private float droidAmountPerTick = 1f;

    private ChargeableDevice phone;
    private ChargeableDevice watch;
    private ChargeableDevice flash;
    private GasFilter gasFilter;
    private IntervalTicker droidTicker;

    private bool solarCharging;
    private bool droidCharging;

    // UI'nin gösterdiği yuvarlanmış değerler; sadece yüzde değişince event atılır (her karede değil).
    private int shownPhone = -1, shownWatch = -1, shownFlash = -1, shownGas = -1;

    // Herhangi bir görünür değer (yuvarlanmış %) değişti. UI değerleri property'lerden okur.
    public event Action Changed;

    public bool IsFlashOn { get; private set; }
    public float PhoneCharge => phone.Value;
    public float WatchCharge => watch.Value;
    public float FlashCharge => flash.Value;
    public float GasFilterValue => gasFilter.Value;
    public bool IsGasFilterEmpty => gasFilter.IsEmpty;

    private void Awake()
    {
        if (mainCharacter == null)
        {
            var manager = GetComponent<PlayerManager>();
            if (manager != null) mainCharacter = manager.mainCharacter;
        }
        if (mainCharacter == null)
        {
            Debug.LogWarning("[DeviceChargeController] MainCharacter bulunamadı; droid şarjı güncellenmeyecek.", this);
        }

        // Property'ler Awake'ten önce okunabilsin diye (UI Start'ta bağlanır) burada kuruluyor.
        phone = new ChargeableDevice(PerSecond(phoneDrainSeconds), PerSecond(phoneChargeSeconds));
        watch = new ChargeableDevice(PerSecond(watchDrainSeconds), PerSecond(watchChargeSeconds));
        flash = new ChargeableDevice(PerSecond(flashDrainSeconds), 0f);
        gasFilter = new GasFilter();
        droidTicker = new IntervalTicker(Mathf.Max(0.01f, droidIntervalSeconds));
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Eski davranış: güneş şarjı açıksa telefon/saat artar, değilse azalır.
        phone.Tick(dt, true, solarCharging);
        watch.Tick(dt, true, solarCharging);
        flash.Tick(dt, IsFlashOn, false);

        TickDroid(dt);
        NotifyIfChanged();
    }

    private void TickDroid(float dt)
    {
        if (mainCharacter == null) return;

        int ticks = droidTicker.Advance(dt);
        if (ticks == 0) return;

        float amount = droidAmountPerTick * ticks;
        if (droidCharging) mainCharacter.IncreaseDroidCharge(amount);
        else mainCharacter.DecreaseDroidCharge(amount);
    }

    public void SetSolarCharging(bool value) => solarCharging = value;

    public void SetDroidCharging(bool value) => droidCharging = value;

    public void ToggleFlash() => IsFlashOn = !IsFlashOn;

    // Şarj istasyonu: telefon/saat/fener dolar. Droid şarjı çağıran tarafta (MainCharacter) kalır.
    public void RechargeDevices()
    {
        phone.Fill();
        watch.Fill();
        flash.Fill();
        NotifyIfChanged();
    }

    public void RefillGasFilter()
    {
        gasFilter.Refill();
        NotifyIfChanged();
    }

    // Dışarıda kalma süresini ölçen sistem (WP-8 AtmosphereSensor) bu kapıdan filtreyi boşaltır.
    // Şimdilik kimse çağırmıyor -> filtre boşalmıyor (eskiden de hiçbir kod boşaltmıyordu).
    public void DrainGasFilter(float amount)
    {
        gasFilter.Drain(amount);
        NotifyIfChanged();
    }

    private void NotifyIfChanged()
    {
        int p = Mathf.RoundToInt(phone.Value);
        int w = Mathf.RoundToInt(watch.Value);
        int f = Mathf.RoundToInt(flash.Value);
        int g = Mathf.RoundToInt(gasFilter.Value);
        if (p == shownPhone && w == shownWatch && f == shownFlash && g == shownGas) return;

        shownPhone = p; shownWatch = w; shownFlash = f; shownGas = g;
        Changed?.Invoke();
    }

    private static float PerSecond(float secondsPerUnit) => 1f / Mathf.Max(0.01f, secondsPerUnit);
}
