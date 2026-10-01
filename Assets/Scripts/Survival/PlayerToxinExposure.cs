using AdanBye.Survival;
using UnityEngine;

// Tek iş: sensörden gelen gaz yoğunluğunu filtre/zehire çevirmek.
// Eski PlayerPoisonStatus'un yerini alır (o, isOutSide'ı hiçbir kod atamadığı için filtreyi hiç boşaltmıyordu).
// Hesap ToxinExposureModel'de (saf, testli); filtre sahipliği DeviceChargeController'da kalır.
public class PlayerToxinExposure : MonoBehaviour, ITimeSkipReceiver
{
    [SerializeField] private MainCharacter mainCharacter;
    [Tooltip("Birimler OYUN SAATİ başınadır. Filtre süresi (oyun saati) = 100 / (FilterDrainPerGameHour x yoğunluk); " +
             "taban yoğunluk 0.2 ve 100 ile 5 oyun saati. Zehir: filtre boşken PoisonPerGameHour x yoğunluk.")]
    [SerializeField] private ToxinExposureConfig config = new ToxinExposureConfig();

    private ToxinExposureModel model;
    private IAtmosphereProvider atmosphere;
    private DeviceChargeController deviceCharge;
    private IGameClock clock;
    private bool warnedNoClock;

    private void Awake()
    {
        atmosphere = GetComponent<IAtmosphereProvider>();
        if (atmosphere == null)
        {
            Debug.LogWarning("[PlayerToxinExposure] IAtmosphereProvider (AtmosphereSensor) bulunamadı; zehir maruziyeti kapalı.", this);
            enabled = false;
            return;
        }

        if (mainCharacter == null)
        {
            var manager = GetComponent<PlayerManager>();
            if (manager != null) mainCharacter = manager.mainCharacter;
        }
        if (mainCharacter == null)
        {
            Debug.LogWarning("[PlayerToxinExposure] MainCharacter bulunamadı (PlayerManager.mainCharacter atanmamış); zehir maruziyeti kapalı.", this);
            enabled = false;
            return;
        }

        model = new ToxinExposureModel(config);
    }

    // PlayerManager.Awake DeviceCharge'ı kurduktan sonra bağlanmak için Start.
    private void Start()
    {
        if (!enabled) return;
        var manager = GetComponent<PlayerManager>();
        deviceCharge = manager != null ? manager.DeviceCharge : null;
        if (deviceCharge == null)
        {
            Debug.LogWarning("[PlayerToxinExposure] DeviceChargeController yok; zehir maruziyeti kapalı.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        // Saat sahne yüklenirken geç gelebilir (DayCycle.Awake sırası); bulunana kadar her karede ucuzca denenir.
        if (clock == null) clock = GameClockLocator.Find();
        if (clock == null)
        {
            // Saat yokken oranı bilemeyiz: yanlış doz uygulamaktansa maruziyet beklemede kalır (log tek sefer).
            if (!warnedNoClock)
            {
                warnedNoClock = true;
                Debug.LogWarning("[PlayerToxinExposure] IGameClock (DayCycle) bulunamadı; saat gelene kadar gaz maruziyeti işlemiyor.", this);
            }
            return;
        }

        float gameHours = GameTimeConversion.RealSecondsToGameHours(Time.deltaTime, clock.GameHoursPerRealSecond);
        Apply(gameHours);
    }

    // Zaman atlaması: yoğunluk atlama anındaki sensör değerinde sabit varsayılır (filtre bitince kalan süre zehire döner).
    public void OnTimeSkipped(float gameHours, float realSecondsEquivalent)
    {
        if (model == null || deviceCharge == null) return; // Awake/Start'ta kapatılmış
        Apply(gameHours);
    }

    private void Apply(float gameHours)
    {
        float poison = deviceCharge.ExposeToGas(model, gameHours, atmosphere.GasDensity);
        if (poison > 0f) mainCharacter.IncreasePoison(poison);
    }
}
