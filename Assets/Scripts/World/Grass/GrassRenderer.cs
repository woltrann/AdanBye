using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdanBye.Grass
{
    /// <summary>
    /// Çim çiziminin sahne bileşeni (ince composition root): Inspector referanslarını <see cref="GrassDrawSystem"/>'e
    /// bağlar, yaşam döngüsünü (kur / yeniden kur / bırak) ve beginCameraRendering aboneliğini yönetir.
    /// Çizim/üretim mantığı burada DEĞİL, GrassDrawSystem'dedir. Kullanıcının materyaline yazmaz.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    // Feed, etkileşimcilerin konumunu shader'a taşır; renderer tek başına eklendiğinde
    // unutulup "çim tepki vermiyor" durumuna düşülmesin diye birlikte gelir.
    [RequireComponent(typeof(GrassInteractionFeed))]
    public sealed class GrassRenderer : MonoBehaviour
    {
        [SerializeField] GrassSettings settings;
        [Tooltip("Shader Graph çim materyali (RenderMeshIndirect ile çizilir; _GrassVisibleInstances okumalı). GPU Instancing'in kapalı olması önerilir (zorunlu değil).")]
        [SerializeField] Material material;
        [Tooltip("Açıkken çim URP'nin DepthNormals ön geçişine çizilmez: SSAO çimeni 'görmez' (çimene AO uygulanmaz) ve çim bir kez daha çizilmekten kurtulur. Yan etki: çim depth texture'da da yer almaz.")]
        [SerializeField] bool skipDepthNormalsPass = true;
        [Tooltip("Boşsa Terrain.activeTerrain kullanılır.")]
        [SerializeField] Terrain terrain;
        // Build'de AssetDatabase yok: compute shader referansı sahneye SERİLEŞTİRİLMİŞ olmalı (Editor'de otomatik atanır;
        // yalnızca serileştirilmiş referans olan shader build'e girer, Resources/Find tabanlı yükleme buna güvenmez).
        [Tooltip("GrassGenerate.compute. Editor'de boşsa otomatik atanır; build için sahnede serileştirilmiş olmalı.")]
        [SerializeField] ComputeShader generateCompute;

        [Tooltip("Opsiyonel. Bake edilmiş exclusion mask (ağaç/mesh altında çim yok). Boşsa exclusion uygulanmaz.")]
        [SerializeField] GrassExclusionMask exclusionMask;

        GrassDrawSystem _system;
        IGrassCameraFilter _filter;
        GrassSettings _subscribedSettings;
        string _status = "Kurulmadı.";
        string _lastLoggedStatus;

        /// <summary>Inspector için durum metni: kurulum başarısı, hatalar ve uyarılar.</summary>
        public string Status => _status;
        public bool IsRunning => _system != null && !_system.IsDisposed;
        public int ActiveCameraCount => _system != null ? _system.CameraCount : 0;
        /// <summary>Son karede seçilen chunk sayısı ve seçici kapasitesi; sistem yoksa 0.</summary>
        public int LastSelectedChunks => IsRunning ? _system.LastSelectedChunks : 0;
        public int MaxChunks => IsRunning ? _system.MaxChunks : 0;
        /// <summary>Kamera başına yaklaşık GPU belleği (bayt, tahmin); sistem yoksa 0.</summary>
        public long EstimatedBytesPerCamera => IsRunning ? _system.EstimatedBytesPerCamera : 0;

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseSystem;
            AutoAssignCompute();
#endif
            SubscribeSettings();
            Rebuild();
        }

        // Start, tüm OnEnable'lardan sonra çalışır: Terrain.activeTerrain OnEnable sırasına bağlı olduğundan
        // OnEnable'da terrain bulunamadıysa burada bir kez daha denenir.
        void Start()
        {
            if (_system == null && terrain == null) Rebuild();
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= ReleaseSystem;
#endif
            UnsubscribeSettings();
            ReleaseSystem();
            // Materyal bir asset: pass durumu bileşen kapanınca eski haline döner (başka kullanımı etkilemesin).
            SetDepthNormalsPassEnabled(true);
        }

        void SetDepthNormalsPassEnabled(bool enabled)
        {
            if (material != null) material.SetShaderPassEnabled("DepthNormals", enabled);
        }

        /// <summary>Sistemi baştan kurar. Ayar/referans değişince çağrılır; idempotent ve hata durumunda çizimi kapatır.</summary>
        public void Rebuild()
        {
            ReleaseSystem();
            if (!isActiveAndEnabled) return;

            // Filtre hot path'te değil burada üretilir (beginCameraRendering'de lazy ??= yok).
            SetDepthNormalsPassEnabled(!skipDepthNormalsPass);
            _filter = new DefaultGrassCameraFilter(gameObject.layer);
            Terrain target = terrain != null ? terrain : Terrain.activeTerrain;

            bool ok = GrassDrawSystem.TryCreate(settings, material, target, generateCompute, gameObject.layer,
                                                exclusionMask,
                                                out GrassDrawSystem system, out ValidationReport report);
            _system = system;
            _status = BuildStatus(ok, report);
            LogStatusOnce(ok, report);
        }

        static string BuildStatus(bool ok, ValidationReport report)
        {
            var sb = new StringBuilder(ok ? "Çalışıyor." : "Çalışmıyor.");
            if (report.Errors.Count + report.Warnings.Count > 0) sb.Append('\n').Append(report);
            return sb.ToString();
        }

        // Aynı durum metni tekrar üretilirse (ör. her OnValidate) konsol tekrar dolmasın.
        void LogStatusOnce(bool ok, ValidationReport report)
        {
            if (report.Errors.Count + report.Warnings.Count == 0) { _lastLoggedStatus = null; return; }
            if (_status == _lastLoggedStatus) return;
            _lastLoggedStatus = _status;
            if (ok) Debug.LogWarning("[GrassRenderer] " + _status, this);
            else Debug.LogError("[GrassRenderer] " + _status, this);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            _system?.Render(cam, _filter);
        }

        void ReleaseSystem()
        {
            _system?.Dispose();
            _system = null;
        }

        void SubscribeSettings()
        {
            if (_subscribedSettings == settings) return;
            UnsubscribeSettings();
            if (settings == null) return;
            _subscribedSettings = settings;
            _subscribedSettings.Changed += OnSettingsChanged;
        }

        void UnsubscribeSettings()
        {
            if (_subscribedSettings != null) _subscribedSettings.Changed -= OnSettingsChanged;
            _subscribedSettings = null;
        }

        // GrassSettings.Changed yalnızca Editor'de (OnValidate) tetiklenir.
        void OnSettingsChanged()
        {
#if UNITY_EDITOR
            QueueRebuild();
#endif
        }

#if UNITY_EDITOR
        bool _rebuildQueued;

        void Reset() => AutoAssignCompute();

        // OnValidate içinde GPU kaynaklarını yok edip yaratmak güvensiz (asset yükleme/serileştirme sırasında da çağrılır)
        // ve slider sürüklerken her değişimde yeniden kurmak pahalı: delayCall ile editör tick'i başına en fazla bir kez.
        void OnValidate()
        {
            SubscribeSettings();
            QueueRebuild();
        }

        void QueueRebuild()
        {
            if (_rebuildQueued) return;
            _rebuildQueued = true;
            EditorApplication.delayCall += () =>
            {
                _rebuildQueued = false;
                if (this == null) return; // bileşen bu arada yok edildi
                AutoAssignCompute();
                Rebuild();
            };
        }

        // Yalnızca alan boşken atar (kullanıcının seçimini ezmez). İsim tam eşleşmesi: benzer adlı başka compute'lara kayma olmasın.
        void AutoAssignCompute()
        {
            if (generateCompute != null) return;
            foreach (string guid in AssetDatabase.FindAssets("GrassGenerate t:ComputeShader"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != "GrassGenerate") continue;
                generateCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
                EditorUtility.SetDirty(this);
                return;
            }
        }
#endif
    }
}
