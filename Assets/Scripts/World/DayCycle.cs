using AdanBye.Survival;
using UnityEngine;

public class DayCycle : MonoBehaviour, IGameClock
{
    public static DayCycle Instance;
    [Header("Sun Settings")]
    public Light sunLight;
    public Gradient sunColorOverTime;
    public AnimationCurve sunIntensityOverTime;

    [Header("Skybox & Ambient")]
    public Material daySkybox;
    public Material nightSkybox;
    public float ambientMultiplier = 1f;

    [Header("Time Settings")]
    public float dayDuration = 120f; // Tam gün süresi (saniye)
    [Range(0f, 1f)] public float startTime = 0.25f; // Baþlangýç: sabah 6 gibi

    private float timeOfDay = 0f;

    private bool isLoaded;

    public float CurrentHour => (timeOfDay % 1f) * 24f;
    public bool IsNight => GetSunIntensity() < 0.2f;

    // 1 gerçek saniyede geçen oyun saati. dayDuration <= 0 ise 0 (saat donuk sayýlýr; tüketiciler güvenle atlar).
    public float GameHoursPerRealSecond => dayDuration > 0f ? 24f / dayDuration : 0f;

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        if (!isLoaded)
        {
            timeOfDay = startTime;
        }
        UpdateLighting();
    }

    private void Update()
    {
        timeOfDay += Time.deltaTime / dayDuration;
        timeOfDay %= 1f;

        UpdateLighting();
    }

    private void UpdateLighting()
    {
        if (sunLight == null) return;

        float cycle = timeOfDay;

        // Güneþ rotasyonu (Yalnýzca x ekseninde dönüþ)
        sunLight.transform.localRotation = Quaternion.Euler(new Vector3(cycle * 360f - 90f, 170f, 0f));

        // Güneþ yoðunluðu ve rengi
        sunLight.intensity = GetSunIntensity();
        sunLight.color = sunColorOverTime.Evaluate(cycle);

        // Ambient ýþýk ayarý
        RenderSettings.ambientIntensity = sunLight.intensity * ambientMultiplier;

        // Skybox deðiþimi
        RenderSettings.skybox = IsNight ? nightSkybox : daySkybox;
    }

    private float GetSunIntensity()
    {
        return sunIntensityOverTime.Evaluate(timeOfDay);
    }
    public string GetFormattedTime()
    {
        int hour = Mathf.FloorToInt(CurrentHour);
        int minute = Mathf.FloorToInt((CurrentHour % 1f) * 60);
        return $"{hour:00}:{minute:00}";
    }
    
    // Zaman atlamasý (çökme/uyku): saat ve ýþýk birlikte ilerler, gün sonunda baþa sarar.
    public void AdvanceHours(float hours)
    {
        if (!(hours > 0f) || float.IsInfinity(hours)) return;
        SetTimeOfDay01(timeOfDay + hours / 24f);
    }

    // Kayýttan yükleme için (SaveManager eskiden private alaný reflection ile yazýyordu). 0..1 dýþý deðer sarýlýr.
    public void SetTimeOfDay01(float value)
    {
        timeOfDay = Mathf.Repeat(value, 1f);
        UpdateLighting();
    }

    public void setIsLoaded(bool value)
    {
        isLoaded = value;
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        GUILayout.Label($" Saat: {Mathf.FloorToInt(CurrentHour)}:{Mathf.FloorToInt((CurrentHour % 1f) * 60):00}");
        GUILayout.Label($" Güneþ Yoðunluðu: {sunLight.intensity:F2}");
        GUILayout.Label($" Gece mi? {(IsNight ? "Evet" : "Hayýr")}");
    }
#endif
}
