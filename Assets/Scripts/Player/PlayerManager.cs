using UnityEngine;
using UnityEngine.InputSystem;

// Tek iş: input action asset'e ve paylaşılan referanslara (PlayerAudio, MainCharacter)
// erişim noktası olmak. Diyalog seçme mantığı (eskiden burada duran PlayDialogue/switch)
// PlayerAudio'ya taşındı - hangi koşulda hangi diyaloğun çalacağını bilmek Audio'nun işi,
// "manager"ın değil.
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    public InputActionAsset InputActions;

    public PlayerAudio PlayerAudio;

    public MainCharacter mainCharacter;

    // Aynı GameObject'teki şarj yöneticisi; sahne ataması gerektirmesin diye GetComponent ile bulunur.
    // Eksikse null kalır - çağıranlar null-güvenli davranır.
    public DeviceChargeController DeviceCharge { get; private set; }

    private void Awake()
    {
        Instance = this;

        DeviceCharge = GetComponent<DeviceChargeController>();
        if (DeviceCharge == null)
        {
            Debug.LogWarning("[PlayerManager] Player üzerinde DeviceChargeController yok; şarj/fener/gaz filtresi çalışmayacak.", this);
        }
    }

    // Tüm Awake'ler bittikten sonra: kayıttan gelen oyuncu durumu (stamina/şarj) bu noktada uygulanabilir.
    private void Start()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.ApplyPendingPlayerState();
    }

    public void OnEnable()
    {
        InputActions.FindActionMap("PlayerController").Enable();
    }

    public void OnDisable()
    {
        InputActions.FindActionMap("PlayerController").Disable();
    }
}
