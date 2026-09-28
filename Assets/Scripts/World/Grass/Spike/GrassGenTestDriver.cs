using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace AdanBye.Grass.Spike
{
    /// <summary>
    /// WP-3b-2 TEST SÜRÜCÜSÜ (geçici, kalıcı GrassRenderer DEĞİL; WP-8'de silinir). Kalıcı Gpu/ + Core/ sınıflarını
    /// bir arada sürer: kamera çevresinde chunk seç -> compute ile üret + LOD seç + blade frustum cull ->
    /// LOD başına ayrı buffer/mesh/RenderMeshIndirect ile çiz (GrassSGContractTest shader'ı).
    /// Her LOD kendi MaterialPropertyBlock'unu kullanır (material.SetBuffer YOK: paylaşılan materyal çakışması bulunmuştu).
    /// Readback/ölçüm yardımcıları yalnızca doğrulama içindir; üretim yolunda GPU'dan okuma yapılmaz.
    /// </summary>
    [ExecuteAlways]
    public sealed class GrassGenTestDriver : MonoBehaviour
    {
        static readonly int DebugTintId = Shader.PropertyToID("_DebugTint");

        public ComputeShader computeShader;
        public Material material;
        public Terrain terrain;
        /// <summary>Boşsa Camera.main. Bu kamera render edilirken üretim yenilenir; diğer kameralar aynı sonucu çizer.</summary>
        public Camera anchorCamera;

        public float chunkSize = 16f;
        public float maxDensityPerM2 = 16f;
        public uint seed = 12345u;
        public int maxChunks = 400;
        /// <summary>CPU: chunk düzeyinde frustum kesimi.</summary>
        public bool useFrustum = true;
        /// <summary>GPU: blade başına frustum kesimi.</summary>
        public bool useBladeFrustum = true;
        /// <summary>Blade küresine eklenen pay (m); WP-4 rüzgar/ezme eğilmesi için.</summary>
        public float cullPadding = 0.3f;

        // LOD listesi (LodLevelSpec'e dönüşür; SO dönüşümü 3b-3'te). Dizi uzunlukları aynı olmalı.
        public float[] lodMaxDistance = { 25f, 60f, 120f };
        public float[] lodKeepRatio = { 1f, 0.35f, 0.1f };
        public float[] lodWidthCompCap = { 1f, 1.7f, 3f };
        public float[] lodBand = { 5f, 10f, 15f };
        /// <summary>LOD başına görünür instance bütçesi (= buffer kapasitesi).</summary>
        public int[] lodBudget = { 400000, 600000, 800000 };

        public bool debugLodColors = true;
        public Color[] lodDebugColors = { new Color(0.2f, 1f, 0.2f), new Color(1f, 0.9f, 0.1f), new Color(1f, 0.25f, 0.2f) };

        /// <summary>Alphamap sırası: Grass_A, Grass_Dry, Snow, Muddy.</summary>
        public Vector4 layerDensity = new Vector4(1f, 0.6f, 0f, 0.15f);
        public float slopeMinDeg = 30f;
        public float slopeMaxDeg = 55f;
        public Vector2 heightRange = new Vector2(0.4f, 0.8f);
        public Vector2 widthRange = new Vector2(0.06f, 0.10f);

        GrassGpuResources _gpu;
        GrassComputeDispatcher _dispatcher;
        VisibleChunkSelector _selector;
        LayerDensityMapper _mapper;
        LodDistanceTable _lods;
        GrassTerrainSamplingInfo _terrainInfo;
        Terrain _terrain;
        Mesh[] _meshes;
        MaterialPropertyBlock[] _props;
        PlanesFrustum _frustum;
        readonly Vector4[] _planeVectors = new Vector4[GrassViewParams.PlaneCount];
        IGrassCameraFilter _filter;
        Bounds _bounds;
        GrassGenerateSettings _settings;
        int _chunkCount;
        bool _hasGenerated;
        string _lastError;

        public bool IsReady => _gpu != null && !_gpu.IsDisposed && _dispatcher != null;
        public GrassGpuResources Gpu => _gpu;
        public GrassGenerateSettings Settings => _settings;
        public LodDistanceTable Lods => _lods;
        public int VisibleChunkCount => _chunkCount;
        public VisibleChunkSelector Selector => _selector;
        public string LastError => _lastError;

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            if (computeShader != null && material != null) Rebuild();
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Release();
        }

        /// <summary>Alanlar değişince (yoğunluk, bütçe, LOD listesi, layer çarpanı, chunk boyutu) çağrılır: her şeyi baştan kurar.</summary>
        public bool Rebuild()
        {
            Release();

            _terrain = terrain != null ? terrain : Terrain.activeTerrain;
            if (_terrain == null) return Fail("Terrain yok.");
            if (computeShader == null || material == null) return Fail("computeShader/material atanmamış.");

            if (!BuildLodTable(out string error)) return Fail(error);
            if (lodBudget == null || lodBudget.Length != _lods.Count) return Fail("lodBudget uzunluğu LOD sayısına eşit olmalı.");
            if (!GrassTerrainSamplingInfo.TryCreate(_terrain, out _terrainInfo, out error)) return Fail(error);

            TerrainData data = _terrain.terrainData;
            int res = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, res, res);
            if (!ChunkGrid.TryCreate(_terrainInfo.Origin.x, _terrainInfo.Origin.z, _terrainInfo.Size.x, _terrainInfo.Size.z,
                                     chunkSize, out ChunkGrid grid, out error)) return Fail(error);
            if (!ChunkBoundsTable.TryCreate(grid, heights, _terrainInfo.Origin.y, _terrainInfo.Size.y,
                                            out ChunkBoundsTable table, out error)) return Fail(error);

            // Hücre seviyesi kullanılmıyor (LOD seyreltmeyle): eşikler çizim mesafesinin ötesinde, seçici 1/2 atamasın.
            float draw = _lods.DrawDistance;
            if (!VisibleChunkSelector.TryCreate(table, maxChunks, draw * 2f, draw * 3f, 2f, out _selector, out error)) return Fail(error);

            if (!BuildLayerMapper(data, out error)) return Fail(error);
            if (!GrassComputeDispatcher.TryCreate(computeShader, out _dispatcher, out error)) return Fail(error);
            if (!GrassGpuResources.TryCreate(lodBudget, maxChunks, out _gpu, out error)) return Fail(error);

            _meshes = new[] { GrassSpikeBladeMesh.Create(6), GrassSpikeBladeMesh.Create(3), GrassLodMeshFactory.CreateTriangle() };
            _props = new MaterialPropertyBlock[_lods.Count];
            for (int lod = 0; lod < _lods.Count; lod++)
            {
                if (!_gpu.ConfigureDrawArgs(lod, _meshes[lod], 0, out error)) return Fail(error);
                _props[lod] = new MaterialPropertyBlock();
                _props[lod].SetBuffer("_GrassVisibleInstances", _gpu.Instances(lod));
                _props[lod].SetColor(DebugTintId, debugLodColors ? lodDebugColors[lod] : Color.white);
            }

            _frustum = new PlanesFrustum();
            _hasGenerated = false;
            _lastError = null;
            return true;
        }

        bool BuildLodTable(out string error)
        {
            int n = lodMaxDistance != null ? lodMaxDistance.Length : 0;
            if (n == 0 || lodKeepRatio.Length != n || lodWidthCompCap.Length != n || lodBand.Length != n)
            {
                error = "LOD dizilerinin uzunlukları aynı ve > 0 olmalı.";
                return false;
            }
            var specs = new List<LodLevelSpec>(n);
            for (int i = 0; i < n; i++) specs.Add(new LodLevelSpec(lodMaxDistance[i], lodKeepRatio[i], lodWidthCompCap[i], lodBand[i]));
            if (!LodDistanceTable.TryCreate(specs, out _lods, out ValidationReport report))
            {
                error = "LodDistanceTable: " + report;
                return false;
            }
            error = null;
            return true;
        }

        bool BuildLayerMapper(TerrainData data, out string error)
        {
            TerrainLayer[] layers = data.terrainLayers;
            var names = new List<string>();
            var rules = new List<LayerDensityRule>();
            for (int i = 0; i < Mathf.Min(layers.Length, LayerDensityMapper.MaxLayers); i++)
            {
                names.Add(layers[i] != null ? layers[i].name : "(null)");
                rules.Add(new LayerDensityRule(i, null, layerDensity[i], Color.white, Color.white, 1f));
            }
            if (!LayerDensityMapper.TryCreate(names, rules, out _mapper, out ValidationReport report))
            {
                error = "LayerDensityMapper: " + report;
                return false;
            }
            error = null;
            return true;
        }

        /// <summary>Seçim + yükleme + dispatch. cam verilirse frustum kullanılır (useFrustum/useBladeFrustum açıksa).</summary>
        public bool Regenerate(Vector3 cameraPosition, Camera frustumCamera)
        {
            if (!IsReady) return false;

            IFrustum frustum = null;
            bool haveFrustum = frustumCamera != null && (useFrustum || useBladeFrustum);
            if (haveFrustum) _frustum.Update(frustumCamera.projectionMatrix * frustumCamera.worldToCameraMatrix);
            if (haveFrustum && useFrustum) frustum = _frustum;

            float draw = _lods.DrawDistance;
            _selector.Select(cameraPosition, draw, frustum);
            _chunkCount = _gpu.UploadChunks(_selector);

            Vector4[] planes = null;
            if (haveFrustum && useBladeFrustum)
            {
                _frustum.CopyPlanes(_planeVectors);
                planes = _planeVectors;
            }

            float y0 = _terrainInfo.Origin.y;
            _bounds = new Bounds(new Vector3(cameraPosition.x, y0 + _terrainInfo.Size.y * 0.5f, cameraPosition.z),
                                 new Vector3(draw * 2f + 2f, _terrainInfo.Size.y + 2f, draw * 2f + 2f));
            return DispatchOnly(new GrassViewParams(cameraPosition, planes, cullPadding));
        }

        /// <summary>Mevcut chunk listesiyle yeniden üretir (ölçüm döngüsü için: seçim/yükleme maliyeti dışarıda).</summary>
        public bool DispatchOnly(in GrassViewParams view)
        {
            if (!IsReady) return false;
            if (!GrassGenerateSettings.TryCreate(chunkSize, maxDensityPerM2, seed, _mapper.DensityMultipliers,
                                                 slopeMinDeg, slopeMaxDeg, heightRange, widthRange,
                                                 out _settings, out string error)) return Fail(error);

            TerrainData data = _terrain.terrainData;
            if (!_dispatcher.Dispatch(_gpu, _terrainInfo, data.heightmapTexture, data.GetAlphamapTexture(0), _settings,
                                      _lods, view, _chunkCount, out error)) return Fail(error);

            _hasGenerated = true;
            return true;
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (!IsReady) return;
            _filter ??= new DefaultGrassCameraFilter(gameObject.layer);
            if (!_filter.ShouldRender(cam)) return;

            Camera anchor = anchorCamera != null ? anchorCamera : Camera.main;
            if (cam == anchor) Regenerate(cam.transform.position, cam);
            if (!_hasGenerated) return;

            for (int lod = 0; lod < _lods.Count; lod++)
            {
                var rp = new RenderParams(material)
                {
                    camera = cam,
                    layer = gameObject.layer,
                    worldBounds = _bounds,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = true,
                    matProps = _props[lod],
                };
                // LOD başına ayrı mesh + ayrı args komutu (startCommand = lod) + ayrı instance buffer (matProps).
                Graphics.RenderMeshIndirect(rp, _meshes[lod], _gpu.Args, 1, lod);
            }
        }

        // --- Yalnızca doğrulama için (GPU senkronize eder, yavaş) ---

        /// <summary>LOD başına üretilen sayaç (bütçeyi aşabilir) ve taşma bayrağı.</summary>
        public void ReadbackState(out uint[] counts, out bool[] overflow)
        {
            var s = new uint[GrassGpuResources.StateCount];
            _gpu.State.GetData(s);
            counts = new uint[_lods.Count];
            overflow = new bool[_lods.Count];
            for (int i = 0; i < _lods.Count; i++)
            {
                counts[i] = s[i * GrassGpuResources.StatePerLod];
                overflow[i] = s[i * GrassGpuResources.StatePerLod + 1] != 0u;
            }
        }

        /// <summary>Bir LOD'un bütçeye kırpılmış geçerli instance'ları (yalnız yazılmış olanlar).</summary>
        public GrassInstance[] ReadbackInstances(int lod)
        {
            ReadbackState(out uint[] counts, out _);
            int valid = (int)System.Math.Min(counts[lod], (uint)_gpu.InstanceCapacity(lod));
            var all = new GrassInstance[valid];
            if (valid > 0) _gpu.Instances(lod).GetData(all, 0, 0, valid);
            return all;
        }

        public uint[] ReadbackArgs()
        {
            var a = new uint[GrassGpuResources.ArgsCount];
            _gpu.Args.GetData(a);
            return a;
        }

        /// <summary>N kez dispatch + tek senkron readback; ms/dispatch (CPU gönderim + GPU yürütme, kaba wall-clock).</summary>
        public float MeasureGenerateMs(in GrassViewParams view, int iterations)
        {
            DispatchOnly(view);
            ReadbackState(out _, out _); // ısınma + senkron
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++) DispatchOnly(view);
            ReadbackState(out _, out _);
            sw.Stop();
            return (float)sw.Elapsed.TotalMilliseconds / iterations;
        }

        /// <summary>Doğrulama için: mevcut frustum/planes olmadan bir kamera konumu için görünüm parametresi.</summary>
        public GrassViewParams MakeView(Vector3 cameraPosition, bool frustum, Camera cam)
        {
            Vector4[] planes = null;
            if (frustum && cam != null)
            {
                _frustum.Update(cam.projectionMatrix * cam.worldToCameraMatrix);
                _frustum.CopyPlanes(_planeVectors);
                planes = _planeVectors;
            }
            return new GrassViewParams(cameraPosition, planes, cullPadding);
        }

        bool Fail(string message)
        {
            if (message != _lastError) Debug.LogError("[GrassGenTestDriver] " + message, this);
            _lastError = message;
            return false;
        }

        void Release()
        {
            _gpu?.Dispose();
            _gpu = null;
            _dispatcher = null;
            _selector = null;
            _props = null;
            _hasGenerated = false;
            if (_meshes != null)
            {
                foreach (Mesh m in _meshes)
                {
                    if (m == null) continue;
                    if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
                }
                _meshes = null;
            }
        }
    }
}
