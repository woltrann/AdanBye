using AdanBye.Survival;
using UnityEngine;

// Tek iş: sensörden gelen gaz yoğunluğunu filtre/zehire çevirmek.
// Eski PlayerPoisonStatus'un yerini alır (o, isOutSide'ı hiçbir kod atamadığı için filtreyi hiç boşaltmıyordu).
// Hesap ToxinExposureModel'de (saf, testli); filtre sahipliği DeviceChargeController'da kalır.
public class PlayerToxinExposure : MonoBehaviour
{
    [SerializeField] private MainCharacter mainCharacter;
    [SerializeField] private ToxinExposureConfig config = new ToxinExposureConfig();

    private ToxinExposureModel model;
    private IAtmosphereProvider atmosphere;
    private DeviceChargeController deviceCharge;

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
        float poison = deviceCharge.ExposeToGas(model, Time.deltaTime, atmosphere.GasDensity);
        if (poison > 0f) mainCharacter.IncreasePoison(poison);
    }
}
