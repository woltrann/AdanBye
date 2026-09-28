using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdanBye.Grass.Spike
{
    /// <summary>
    /// WP-1b SPIKE (kalıcı değil): sabit N instance'ı terrain üzerinde CPU'da (Terrain.SampleHeight) yerleştirir,
    /// StructuredBuffer'a yazar ve her kamera için RenderPipelineManager.beginCameraRendering içinde
    /// Graphics.RenderMeshIndirect ile çizer. Kamera başına frustum cull + sayaç tutar.
    ///
    /// Neden yükseklik CPU'da: 10k sabit instance için tek seferlik iş; Terrain.SampleHeight terrain mesh'iyle
    /// aynı üçgen enterpolasyonunu kullanır. GPU tarafı (WP-3 compute) köşegen-A örneklemesini kullanmak ZORUNDA.
    /// </summary>
    [ExecuteAlways]
    public sealed class GrassSpikeRenderer : MonoBehaviour
    {
        /// <summary>Materyalin GPU instancing durumunu bu bileşenin nasıl ele alacağı.</summary>
        public enum InstancingMode
        {
            /// <summary>Procedural (elle yazılmış HLSL) shader yolu: instancing açık olmalı (varsayılan, spike davranışı).</summary>
            ForceOn,
            /// <summary>Shader Graph yolu (matris identity + SV_InstanceID): INSTANCING_ON varyantı matrisi okunmayan
            /// per-draw cbuffer dizisinden alır, bu yüzden kapalı olmalı.</summary>
            ForceOff,
            /// <summary>Materyale dokunma (yalnızca durumu logla).</summary>
            KeepAsIs,
        }

        [SerializeField] Terrain terrain;
        public Material material;
        [Tooltip("ForceOn: elle yazılmış procedural shader (instancing açık olmalı). ForceOff: Shader Graph testi (Instance ID node + identity matris; instancing kapalı olmalı). " +
                 "Mod yalnızca bileşenin kendi ürettiği materyale uygulanır; atadığınız materyale YAZILMAZ (asset kirlenmesin), uyuşmazlıkta Console'da uyarı çıkar ve materyalde 'Enable GPU Instancing'i elle ayarlamanız gerekir.")]
        public InstancingMode instancingMode = InstancingMode.ForceOn;
        [SerializeField] int instanceCount = 10000;
        [SerializeField] float areaSize = 40f;
        [SerializeField] Vector2 heightRange = new Vector2(0.5f, 0.9f);
        [SerializeField] Vector2 widthRange = new Vector2(0.06f, 0.10f);
        [SerializeField] uint seed = 12345;

        [Header("Blade Mesh (opsiyonel)")]
        [Tooltip("Kendi blade mesh'iniz. Boşsa 'meshSourcePrefab', o da boşsa kodla üretilen varsayılan blade kullanılır (öncelik: customMesh > meshSourcePrefab > varsayılan). " +
                 "Sözleşme: pivot kökte (0,0,0), +Y yukarı, yaklaşık 1 birim yükseklik ve 1 birim taban genişliği. Instance ölçeği (width,height,width) ile uygulanır " +
                 "(varsayılan widthRange 0.06-0.10, heightRange 0.5-0.9 m). Yalnızca submesh 0 çizilir; mesh'in Read/Write açık olması gerekmez; asset'e yazılmaz.")]
        [SerializeField] Mesh customMesh;
        [Tooltip("customMesh boşsa: bu prefab/GameObject içindeki ilk MeshFilter'ın (çocuklar dahil) sharedMesh'i alınır. Yalnızca MESH alınır; materyal prefab'dan ALINMAZ, " +
                 "'material' alanından gelir. Mesh sözleşmesi customMesh ile aynıdır: pivot kökte, +Y yukarı, ~1 birim yükseklik ve taban genişliği " +
                 "(instance ölçeği (width,height,width); varsayılan widthRange 0.06-0.10, heightRange 0.5-0.9 m).")]
        [SerializeField] GameObject meshSourcePrefab;

        [Header("Debug")]
        [Tooltip("Alt yarıyı kırmızımsı, üst yarıyı mavimsi boyar (startInstance testi).")]
        public bool debugTintHalves;
        [Tooltip("Indirect args'ta startInstance; 0 dışı ise SV_InstanceID + baseInstance davranışını test eder.")]
        public int debugStartInstance;

        /// <summary>Kamera başına çizim/cull sayaçları (doğrulama için).</summary>
        public sealed class CameraStats
        {
            public int draws;
            public int culled;
            public int lastDrawFrame = -1;
            public int lastRejectedFrame = -1;
        }

        readonly Dictionary<Camera, CameraStats> _stats = new Dictionary<Camera, CameraStats>();
        readonly Plane[] _planes = new Plane[6];
        IGrassCameraFilter _filter;
        GraphicsBuffer _instances;
        GraphicsBuffer _args;
        Mesh _mesh;
        // Neden ayrı bayrak: kullanıcının mesh asset'i ASLA Destroy edilmemeli; yalnızca kodla ürettiğimiz varsayılan mesh'e sahibiz.
        bool _ownsMesh;
        // OnValidate'in "mesh kaynağı değişti mi" kararı için son kurulumdaki kaynaklar.
        Mesh _builtCustomMesh;
        GameObject _builtMeshSourcePrefab;
        Material _runtimeMaterial;
        // Bileşene ait buffer bağlaması (bkz. Rebuild'deki "neden" yorumu); RenderParams.matProps ile geçilir.
        MaterialPropertyBlock _props;
        static readonly int InstancesId = Shader.PropertyToID("_GrassVisibleInstances");
        Bounds _bounds;
        bool _ready;

        public IReadOnlyDictionary<Camera, CameraStats> Stats => _stats;
        public int RejectedCameraCalls { get; private set; }
        public bool IsReady => _ready;
        public Bounds FieldBounds => _bounds;

        /// <summary>Test/eval için: filtre dışarıdan enjekte edilebilir (DIP).</summary>
        public void SetFilter(IGrassCameraFilter filter) => _filter = filter;

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            Rebuild();
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            ReleaseResources();
        }

        /// <summary>Buffer'ları ve mesh'i baştan üretir (ayar değişince / test için).</summary>
        public void Rebuild()
        {
            ReleaseResources();
            _stats.Clear();
            RejectedCameraCalls = 0;

            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing)
            {
                Debug.LogError("[GrassSpike] Compute/instancing desteklenmiyor; devre dışı.", this);
                return;
            }

            if (terrain == null) terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogError("[GrassSpike] Terrain bulunamadı; devre dışı.", this);
                return;
            }

            if (!ResolveMaterial())
                return;

            instanceCount = Mathf.Max(1, instanceCount);
            debugStartInstance = Mathf.Clamp(debugStartInstance, 0, instanceCount - 1);

            var data = new GrassSpikeInstance[instanceCount];
            FillInstances(data, out _bounds);

            _instances = new GraphicsBuffer(GraphicsBuffer.Target.Structured, instanceCount, GrassSpikeInstance.Stride);
            _instances.SetData(data);

            _mesh = ResolveMesh(out _ownsMesh);
            _builtCustomMesh = customMesh;
            _builtMeshSourcePrefab = meshSourcePrefab;

            _args = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            var args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            args[0].indexCountPerInstance = _mesh.GetIndexCount(0);
            args[0].instanceCount = (uint)(instanceCount - debugStartInstance);
            args[0].startIndex = _mesh.GetIndexStart(0);
            args[0].baseVertexIndex = _mesh.GetBaseVertex(0);
            args[0].startInstance = (uint)debugStartInstance;
            _args.SetData(args);

            // Neden materyale değil MaterialPropertyBlock'a: materyal asset'inin tek bir "_GrassVisibleInstances" yuvası var.
            // Aynı materyal birden fazla bileşende kullanılınca (sahnede birkaç alan) son Rebuild eden bileşen yuvanın
            // üzerine yazıp diğerlerinin alanını boşaltıyordu (ya da serbest bırakılmış buffer'a bakıyordu). Blok bileşene
            // ait olduğundan bileşenler arası çakışma yok; materyale de hiçbir şey yazılmaz (asset kirlenmez).
            // Rebuild'de bir kez doldurulur; frame başına SetBuffer/allocation yok.
            _props ??= new MaterialPropertyBlock();
            _props.SetBuffer(InstancesId, _instances);
            _ready = true;
        }

        /// <summary>Öncelik: customMesh > meshSourcePrefab > kodla üretilen varsayılan. Geçersiz kullanıcı mesh'i varsayılana düşer.</summary>
        Mesh ResolveMesh(out bool owned)
        {
            Mesh source = customMesh;
            string sourceLabel = "customMesh";
            if (source == null && meshSourcePrefab != null)
            {
                sourceLabel = $"meshSourcePrefab '{meshSourcePrefab.name}'";
                MeshFilter filter = meshSourcePrefab.GetComponentInChildren<MeshFilter>(true);
                source = filter != null ? filter.sharedMesh : null;
                if (source == null)
                    Debug.LogWarning($"[GrassSpike] {sourceLabel} içinde mesh'li MeshFilter yok; varsayılan blade kullanılıyor.", this);
            }

            if (source != null && IsCustomMeshAcceptable(source, sourceLabel))
            {
                owned = false;
                return source;
            }

            owned = true;
            return GrassSpikeBladeMesh.Create();
        }

        bool IsCustomMeshAcceptable(Mesh mesh, string sourceLabel)
        {
            if (!GrassSpikeMeshContract.IsUsable(mesh.vertexCount, out string error))
            {
                Debug.LogError($"[GrassSpike] {sourceLabel} '{mesh.name}': {error} Varsayılan blade kullanılıyor.", this);
                return false;
            }

            var warnings = new List<string>(3);
            GrassSpikeMeshContract.CollectWarnings(mesh.subMeshCount, mesh.bounds, warnings);
            // Rebuild başına tek sefer (frame başına değil).
            foreach (string w in warnings)
                Debug.LogWarning($"[GrassSpike] {sourceLabel} '{mesh.name}': {w}", this);
            return true;
        }

#if UNITY_EDITOR
        // Play dışında Inspector'da mesh kaynağı değişince çim yeni mesh'le güncellensin. Rebuild GraphicsBuffer/materyal
        // yok edip yarattığından OnValidate içinde değil delayCall ile çağrılır; yalnızca mesh alanları değişince tetiklenir
        // (diğer alanların mevcut davranışı değişmesin).
        void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            if (customMesh == _builtCustomMesh && meshSourcePrefab == _builtMeshSourcePrefab) return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Rebuild();
            };
        }
#endif

        bool ResolveMaterial()
        {
            if (material == null)
            {
                Shader shader = Shader.Find("AdanBye/Spike/GrassSpikeLit");
                if (shader == null)
                {
                    Debug.LogError("[GrassSpike] Shader bulunamadı: AdanBye/Spike/GrassSpikeLit", this);
                    return false;
                }
                _runtimeMaterial = new Material(shader) { name = "GRASS_SPIKE_Mat", hideFlags = HideFlags.DontSave };
                material = _runtimeMaterial;
            }

            // Neden kullanıcı materyaline yazmıyoruz: enableInstancing asset'e serileşir; bileşen kullanıcının
            // (Shader Graph) materyalini sessizce değiştirip asset'i kirletmesin. Yalnızca kendi ürettiğimiz
            // runtime materyalde uygularız; kullanıcının materyali uyuşmuyorsa tek seferlik uyarı veririz.
            // (ResolveMaterial yalnızca Rebuild'de çağrılır, yani uyarı frame başına değil.)
            bool wantInstancing = instancingMode == InstancingMode.ForceOn;
            if (instancingMode != InstancingMode.KeepAsIs)
            {
                if (material == _runtimeMaterial)
                    material.enableInstancing = wantInstancing; // ForceOn: procedural varyant için instancing açık şart
                else if (material.enableInstancing != wantInstancing)
                    Debug.LogWarning($"[GrassSpike] Materyal '{material.name}' için mod {instancingMode} ama " +
                                     $"enableInstancing={material.enableInstancing}. Materyaldeki 'Enable GPU Instancing'i " +
                                     $"{(wantInstancing ? "aç" : "kapat")} (bileşen kullanıcı materyaline yazmaz).", material);
            }

            Debug.Log($"[GrassSpike] Materyal '{material.name}' shader='{material.shader.name}' " +
                      $"enableInstancing={material.enableInstancing} mode={instancingMode}", this);
            return true;
        }

        // Bileşen Inspector'dan ilk eklendiğinde çağrılır. Yeni eklenen bileşen kullanıcının kendi (Shader Graph)
        // materyalini test etmek için eklendiğinden varsayılanı ForceOff yapıyoruz; sahnede halihazırda kayıtlı
        // spike bileşeni Reset'ten geçmez, ForceOn olarak kalır ve davranışı değişmez.
        void Reset() => instancingMode = InstancingMode.ForceOff;

        void FillInstances(GrassSpikeInstance[] data, out Bounds bounds)
        {
            var rng = new Unity.Mathematics.Random(seed == 0 ? 1u : seed);
            Vector3 center = transform.position;
            float originY = terrain.GetPosition().y; // SampleHeight terrain'e göreli döner
            int side = Mathf.CeilToInt(Mathf.Sqrt(data.Length));
            float cell = areaSize / side;
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            int half = data.Length / 2;

            for (int i = 0; i < data.Length; i++)
            {
                float x = center.x - areaSize * 0.5f + ((i % side) + rng.NextFloat()) * cell;
                float z = center.z - areaSize * 0.5f + ((i / side) + rng.NextFloat()) * cell;
                float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + originY;

                float height = rng.NextFloat(heightRange.x, heightRange.y);
                float width = rng.NextFloat(widthRange.x, widthRange.y);
                float yaw = rng.NextFloat(0f, 2f * Mathf.PI);
                Color32 tint = new Color32(255, 255, 255, 255);
                if (debugTintHalves)
                    tint = i < half ? new Color32(255, 110, 110, 255) : new Color32(110, 110, 255, 255);

                var p = new Vector3(x, y, z);
                data[i] = GrassSpikeInstance.Create(p, yaw, height, width, tint, rng.NextUInt());
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p + Vector3.up * heightRange.y);
            }

            bounds = new Bounds((min + max) * 0.5f, max - min);
            bounds.Expand(new Vector3(0.5f, 0f, 0.5f)); // blade genişliği/rüzgar payı
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (!_ready) return;

            _filter ??= new DefaultGrassCameraFilter(gameObject.layer);
            if (!_filter.ShouldRender(cam))
            {
                RejectedCameraCalls++;
                return;
            }

            if (!_stats.TryGetValue(cam, out CameraStats stats))
                _stats[cam] = stats = new CameraStats();

            // Kamera başına ayrı cull: Game ve SceneView aynı frame'de farklı sonuç verebilir.
            GeometryUtility.CalculateFrustumPlanes(cam, _planes);
            if (!GeometryUtility.TestPlanesAABB(_planes, _bounds))
            {
                stats.culled++;
                return;
            }

            var rp = new RenderParams(material)
            {
                camera = cam,
                layer = gameObject.layer,
                worldBounds = _bounds,
                // Çim gölge ATMAZ (ShadowCaster pass'i de yok), yalnızca gölge alır.
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true,
                matProps = _props,
            };
            Graphics.RenderMeshIndirect(rp, _mesh, _args);
            stats.draws++;
            stats.lastDrawFrame = Time.frameCount;
        }

        void ReleaseResources()
        {
            _ready = false;
            // Blok serbest bırakılan buffer'a referans tutmasın; nesnenin kendisi Rebuild'de yeniden kullanılır.
            _props?.Clear();
            _instances?.Release();
            _instances = null;
            _args?.Release();
            _args = null;
            if (_ownsMesh) DestroyObject(_mesh);
            _mesh = null;
            _ownsMesh = false;
            if (_runtimeMaterial != null)
            {
                if (material == _runtimeMaterial) material = null;
                DestroyObject(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }

        static void DestroyObject(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
