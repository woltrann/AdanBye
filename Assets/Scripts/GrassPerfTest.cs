using UnityEngine;
using UnityEngine.Rendering.Universal;

// Sahnede herhangi bir objeye ekle. Development build'de tuþlarla A/B testi yapar.
// G = çimen/detail aç-kapa, S = gölge aç-kapa, R = Render Scale 1 <-> 0.5
// Not: Proje sadece yeni Input System kullanýyorsa Input.GetKeyDown hata verir;
// o zaman Player Settings > Active Input Handling'i "Both" yap.
public class GrassPerfTest : MonoBehaviour
{
    Terrain terrain;
    float origDetailDistance;
    float origShadowDistance;
    float origRenderScale;
    bool grassOn = true, shadowOn = true, scaleHalf = false;
    float smoothedDt;

    void Start()
    {
        terrain = Terrain.activeTerrain;
        origDetailDistance = terrain.detailObjectDistance;

        var asset = UniversalRenderPipeline.asset;
        origShadowDistance = asset.shadowDistance;
        origRenderScale = asset.renderScale;
    }

    void Update()
    {
        smoothedDt = Mathf.Lerp(smoothedDt, Time.unscaledDeltaTime, 0.05f);
        var asset = UniversalRenderPipeline.asset;

        if (Input.GetKeyDown(KeyCode.G))
        {
            grassOn = !grassOn;
            terrain.detailObjectDistance = grassOn ? origDetailDistance : 0f;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            shadowOn = !shadowOn;
            asset.shadowDistance = shadowOn ? origShadowDistance : 0f;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            scaleHalf = !scaleHalf;
            asset.renderScale = scaleHalf ? 0.5f : origRenderScale;
        }
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        GUI.Label(new Rect(10, 10, 700, 40),
            $"{smoothedDt * 1000f:F1} ms  ({1f / smoothedDt:F0} FPS)", style);
        GUI.Label(new Rect(10, 40, 700, 40),
            $"[G] Çimen: {(grassOn ? "AÇIK" : "KAPALI")}   " +
            $"[S] Gölge: {(shadowOn ? "AÇIK" : "KAPALI")}   " +
            $"[R] Render Scale: {(scaleHalf ? "0.5" : origRenderScale.ToString("F1"))}", style);
    }
}