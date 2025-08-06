using UnityEngine;

public class DayCycle : MonoBehaviour
{
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

    public float CurrentHour => (timeOfDay % 1f) * 24f;
    public bool IsNight => GetSunIntensity() < 0.2f;

    private void Start()
    {
        timeOfDay = startTime;
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

#if UNITY_EDITOR
    private void OnGUI()
    {
        GUILayout.Label($" Saat: {Mathf.FloorToInt(CurrentHour)}:{Mathf.FloorToInt((CurrentHour % 1f) * 60):00}");
        GUILayout.Label($" Güneþ Yoðunluðu: {sunLight.intensity:F2}");
        GUILayout.Label($" Gece mi? {(IsNight ? "Evet" : "Hayýr")}");
    }
#endif
}
