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
    public float dayDuration = 120f; // Tam g�n s�resi (saniye)
    [Range(0f, 1f)] public float startTime = 0.25f; // Ba�lang��: sabah 6 gibi

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

        // G�ne� rotasyonu (Yaln�zca x ekseninde d�n��)
        sunLight.transform.localRotation = Quaternion.Euler(new Vector3(cycle * 360f - 90f, 170f, 0f));

        // G�ne� yo�unlu�u ve rengi
        sunLight.intensity = GetSunIntensity();
        sunLight.color = sunColorOverTime.Evaluate(cycle);

        // Ambient ���k ayar�
        RenderSettings.ambientIntensity = sunLight.intensity * ambientMultiplier;

        // Skybox de�i�imi
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
        GUILayout.Label($" G�ne� Yo�unlu�u: {sunLight.intensity:F2}");
        GUILayout.Label($" Gece mi? {(IsNight ? "Evet" : "Hay�r")}");
    }
#endif
}
