using AdanBye.Grass;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Sahnede herhangi bir objeye ekle. Play'de tuslarla A/B testi yapar; sonuc ekranda ve [L] ile Console'da.
// G = GrassRenderer ac-kapa (cimin toplam maliyeti = kapali FPS - acik FPS)
// S = golge mesafesi ac-kapa
// R = Render Scale 1 <-> 0.5 (FPS cok artarsa maliyet piksel/raster tarafinda)
// L = anlik durumu Console'a yaz (etiketle birlikte, kopyalayip paylasmak icin)
// Not: Input.GetKeyDown icin Player Settings > Active Input Handling "Both" veya "Input Manager" olmali.
// Not: UniversalRenderPipeline.asset bir proje asset'idir; Editor'de yaptigimiz degisiklik kalici olur,
// bu yuzden OnDisable'da orijinal degerler geri yazilir.
public class GrassPerfTest : MonoBehaviour
{
    // Olcum penceresi: tek kare degerleri gurultulu, ~1 sn ortalama okunabilir sayi verir.
    const float WindowSeconds = 1f;

    [SerializeField] GrassRenderer grass;

    float _origShadowDistance;
    float _origRenderScale;
    int _origVSync;
    int _origTargetFps;
    bool _restoreValid;

    bool _grassOn = true, _shadowOn = true, _scaleHalf;

    float _windowTime, _windowMaxDt;
    int _windowFrames;
    float _avgMs, _maxMs;

    readonly FrameTiming[] _timing = new FrameTiming[1];
    double _gpuMs;

    void OnEnable()
    {
        if (grass == null) grass = FindFirstObjectByType<GrassRenderer>();

        // FPS sinirlari olcumu gizler (cim acik/kapali fark etmez). Test suresince kaldirilir, OnDisable'da geri yazilir.
        _origVSync = QualitySettings.vSyncCount;
        _origTargetFps = Application.targetFrameRate;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        UniversalRenderPipelineAsset asset = UniversalRenderPipeline.asset;
        if (asset == null) return;
        _origShadowDistance = asset.shadowDistance;
        _origRenderScale = asset.renderScale;
        _restoreValid = true;
    }

    void OnDisable()
    {
        QualitySettings.vSyncCount = _origVSync;
        Application.targetFrameRate = _origTargetFps;

        UniversalRenderPipelineAsset asset = UniversalRenderPipeline.asset;
        if (asset != null && _restoreValid)
        {
            asset.shadowDistance = _origShadowDistance;
            asset.renderScale = _origRenderScale;
        }
        if (grass != null) grass.enabled = true;
    }

    void Update()
    {
        AccumulateFrame();
        HandleKeys();
    }

    void HandleKeys()
    {
        UniversalRenderPipelineAsset asset = UniversalRenderPipeline.asset;

        if (Input.GetKeyDown(KeyCode.G) && grass != null)
        {
            _grassOn = !_grassOn;
            grass.enabled = _grassOn;
            ResetWindow(); // Rebuild takilmasi yeni olcumu bozmasin
        }
        if (Input.GetKeyDown(KeyCode.S) && asset != null)
        {
            _shadowOn = !_shadowOn;
            asset.shadowDistance = _shadowOn ? _origShadowDistance : 0f;
            ResetWindow();
        }
        if (Input.GetKeyDown(KeyCode.R) && asset != null)
        {
            _scaleHalf = !_scaleHalf;
            asset.renderScale = _scaleHalf ? 0.5f : _origRenderScale;
            ResetWindow();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            // Pencere bir tusa basinca sifirlanir; dolmadan yazarsak avg=0 ve GPU ms ESKI degerden kalir (yaniltici).
            if (_avgMs <= 0f) Debug.LogWarning("[GrassPerfTest] Olcum penceresi dolmadi: tusa bastiktan sonra ~1-2 sn bekleyip tekrar L'ye bas.");
            else Debug.Log("[GrassPerfTest] " + BuildReport());
        }
    }

    void AccumulateFrame()
    {
        float dt = Time.unscaledDeltaTime;
        _windowTime += dt;
        _windowFrames++;
        if (dt > _windowMaxDt) _windowMaxDt = dt;

        if (_windowTime < WindowSeconds) return;

        _avgMs = _windowTime / _windowFrames * 1000f;
        _maxMs = _windowMaxDt * 1000f;
        _gpuMs = ReadGpuMs();
        ResetWindow(keepDisplayed: true);
    }

    void ResetWindow(bool keepDisplayed = false)
    {
        _windowTime = 0f;
        _windowFrames = 0;
        _windowMaxDt = 0f;
        if (!keepDisplayed) { _avgMs = 0f; _maxMs = 0f; }
    }

    // GPU suresi her platformda/ayarda yok (Editor + bazi API'lerde 0 doner); 0 ise 'n/a' gosterilir.
    double ReadGpuMs()
    {
        FrameTimingManager.CaptureFrameTimings();
        uint got = FrameTimingManager.GetLatestTimings(1, _timing);
        return got > 0 ? _timing[0].gpuFrameTime : 0.0;
    }

    string BuildReport()
    {
        string fps = _avgMs > 0f ? (1000f / _avgMs).ToString("F0") : "-";
        string gpu = _gpuMs > 0.0 ? _gpuMs.ToString("F1") + " ms" : "n/a";
        string chunks = grass != null && grass.IsRunning ? $"{grass.LastSelectedChunks}/{grass.MaxChunks}" : "-";
        return $"grass={(_grassOn ? "ON" : "OFF")} shadow={(_shadowOn ? "ON" : "OFF")} " +
               $"renderScale={(_scaleHalf ? "0.5" : _origRenderScale.ToString("F1"))} | " +
               $"avg={_avgMs:F1} ms ({fps} FPS) max={_maxMs:F1} ms gpu={gpu} chunks={chunks} | " +
               $"vsync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} refresh={Screen.currentResolution.refreshRateRatio.value:F0}Hz";
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        GUI.Label(new Rect(10, 10, 1100, 32), BuildReport(), style);
        GUI.Label(new Rect(10, 40, 1100, 32),
            "[G] Cim  [S] Golge  [R] Render Scale 0.5  [L] Console'a yaz   (deger ~1 sn ortalama)", style);
    }
}
