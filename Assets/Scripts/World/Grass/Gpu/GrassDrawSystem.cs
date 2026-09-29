using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdanBye.Grass
{
    /// <summary>
    /// Çim çiziminin kurulumu + kare akışı (MonoBehaviour DEĞİL: sahne/Editor bağımsız, IDisposable). Kurulum
    /// (<see cref="TryCreate"/>) tüm bağımlılıkları doğrular; kare akışı (<see cref="Render"/>) kamera başına:
    /// filtre -> chunk seç -> yükle -> compute (üret + LOD + cull) -> LOD başına RenderMeshIndirect.
    /// Durumsuz üretim: her kare her kamera için baştan üretilir, önceki kareden veri saklanmaz.
    /// Hata metinleri kurulumda <see cref="ValidationReport"/>'ta, kare akışında tek seferlik loglanır (aynı hata spam yapmaz).
    /// </summary>
    public sealed class GrassDrawSystem : IDisposable
    {
        readonly GrassRuntimeConfig _config;
        readonly Material _material;
        readonly Terrain _terrain;
        readonly GrassTerrainSamplingInfo _terrainInfo;
        readonly ChunkBoundsTable _boundsTable;
        readonly GrassLodMeshSet _meshes;
        readonly GrassComputeDispatcher _dispatcher;
        readonly int _layer;
        readonly int _maxChunks;
        readonly float _boundsMargin;
        readonly Vector3 _chunkPadding;
        int _lastSelectedChunks;
        readonly GrassCameraRegistry<GrassCameraContext> _cameras = new GrassCameraRegistry<GrassCameraContext>();
        readonly Func<Camera, GrassCameraContext> _createContext; // delegate bir kez: per-frame allocation olmasın
        readonly TerrainCallbacks.HeightmapChangedCallback _heightmapChanged;

        // Aynı mesaj oturum boyunca bir kez loglanır: per-frame hata (ör. bozuk girdi) konsolu boğmasın.
        readonly HashSet<string> _logged = new HashSet<string>();
        bool _disposed;

        /// <summary>Şu an kaynağı ayrılmış kamera sayısı (Game + SceneView ...).</summary>
        public int CameraCount => _cameras.Count;
        public GrassRuntimeConfig Config => _config;
        public int MaxChunks => _maxChunks;
        /// <summary>Son çizilen karede (son kamera) seçicinin seçtiği chunk sayısı; Inspector sayacı içindir.</summary>
        public int LastSelectedChunks => _lastSelectedChunks;
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Kamera başına yaklaşık GPU belleği (bayt; TAHMİN): LOD instance buffer'ları + chunk buffer'ı. Küçük state/args
        /// buffer'ları ihmal edilir. Inspector'ın "kamera başına ~X MB" göstergesi içindir.
        /// </summary>
        public long EstimatedBytesPerCamera => EstimateBytesPerCamera(_config.CopyLodInstanceBudgets(), _maxChunks);

        public static long EstimateBytesPerCamera(int[] lodInstanceBudgets, int maxChunks)
        {
            long bytes = (long)maxChunks * GrassGpuChunk.Stride;
            foreach (int budget in lodInstanceBudgets) bytes += (long)budget * GrassInstance.Stride;
            return bytes;
        }

        GrassDrawSystem(GrassRuntimeConfig config, Material material, Terrain terrain, in GrassTerrainSamplingInfo terrainInfo,
                        ChunkBoundsTable boundsTable, GrassLodMeshSet meshes, GrassComputeDispatcher dispatcher, int layer, int maxChunks)
        {
            _config = config;
            _material = material;
            _terrain = terrain;
            _terrainInfo = terrainInfo;
            _boundsTable = boundsTable;
            _meshes = meshes;
            _dispatcher = dispatcher;
            _layer = layer;
            _maxChunks = maxChunks;

            // RenderParams.worldBounds pay'ı: rüzgar/ezme eğilmesi (cullPadding) + en geniş blade (LOD genişlik telafisiyle).
            float maxWidthComp = 1f;
            foreach (float w in config.Lods.WidthCompensation) maxWidthComp = Mathf.Max(maxWidthComp, w);
            _boundsMargin = config.CullPadding + config.Generate.WidthRange.y * maxWidthComp;

            // Chunk frustum dolgusu aynı marjı kullanır: aksi halde chunk dışına taşan bıçaklar, chunk elendiği için
            // ekran kenarında aniden kaybolurdu. Dikey: zemin AABB'sinin üstüne uzanan boy + iki kat marj.
            _chunkPadding = new Vector3(_boundsMargin, 2f * _boundsMargin + config.Generate.HeightRange.y, _boundsMargin);

            _createContext = CreateContext;
            _heightmapChanged = OnHeightmapChanged;
            TerrainCallbacks.heightmapChanged += _heightmapChanged;
        }

        /// <summary>
        /// Chunk seçici çıktı kapasitesi: çizim halkasına (yarıçap + chunk köşegeni payı) düşen chunk sayısı + %25 güvenlik,
        /// grid'in toplam chunk sayısını ve compute z-dispatch sınırını aşmaz. Neden formül: sabit 400 gibi bir sayı
        /// chunk boyutu/mesafe değişince ya boşa GPU belleği ya sessiz kırpma olurdu.
        /// </summary>
        public static int ComputeMaxChunks(float drawDistance, float chunkSize, int gridChunkCount)
        {
            float reach = drawDistance + chunkSize;
            double ring = Math.PI * reach * reach / ((double)chunkSize * chunkSize);
            // Neden int.MaxValue'ya kırpıyoruz: çok büyük çizim mesafesi/çok küçük chunk'ta halka int'i aşar ve
            // (int) dönüşümü int.MinValue'ya sarıp aşağıdaki Max(1, ...) ile sessizce 1 chunk'a düşürürdü.
            int wanted = (int)Math.Min(Math.Ceiling(ring * 1.25), int.MaxValue);
            int cap = Math.Min(gridChunkCount, GrassGpuResources.MaxChunkCapacity);
            return Math.Max(1, Math.Min(wanted, cap));
        }

        /// <param name="layer">Çizimde kullanılan GameObject layer'ı (RenderParams.layer; kamera cullingMask'i ile eşleşir).</param>
        public static bool TryCreate(GrassSettings settings, Material material, Terrain terrain, ComputeShader compute, int layer,
                                     out GrassDrawSystem system, out ValidationReport report)
        {
            system = null;
            report = new ValidationReport();

            if (!SystemInfo.supportsComputeShaders) report.AddError("Bu GPU/API compute shader desteklemiyor.");
            if (!SystemInfo.supportsIndirectArgumentsBuffer) report.AddError("Bu GPU/API indirect argument buffer desteklemiyor.");
            if (settings == null) report.AddError("GrassSettings atanmamış.");
            if (material == null) report.AddError("Çim materyali atanmamış.");
            if (compute == null)
                report.AddError("GrassGenerate compute shader'ı atanmamış (build'de referans serileştirilmiş olmalı; Editor'de otomatik atanır).");
            if (terrain == null) report.AddError("Terrain yok (atanmamış ve sahnede aktif Terrain bulunamadı).");
            else if (terrain.terrainData == null) report.AddError("Terrain'in TerrainData'sı yok.");
            if (!report.IsValid) return false;

            // Kullanıcının materyaline yazmayız; yalnızca bilgilendiririz. Ölçüm: instancing açıkken de çizim çalıştı, bu yüzden
            // zorunlu değil; yine de RenderMeshIndirect kendi instance verisini kullandığından kapalı tutmak önerilir.
            if (material.enableInstancing)
                report.AddWarning($"'{material.name}' materyalinde GPU Instancing açık; RenderMeshIndirect yolunda genelde gerekmez (kapatılması önerilir; çizim açıkken de çalışır). Materyale dokunulmadı.");

            TerrainData data = terrain.terrainData;
            if (!settings.TryBuildRuntime(TerrainLayerNames(data), out GrassRuntimeConfig config, out ValidationReport configReport))
            {
                report.Merge(configReport);
                return false;
            }
            report.Merge(configReport); // geçerli olsa da uyarılar taşınır

            if (!GrassTerrainSamplingInfo.TryCreate(terrain, out GrassTerrainSamplingInfo info, out string error)) return Fail(report, error);
            if (!ChunkGrid.TryCreate(info.Origin.x, info.Origin.z, info.Size.x, info.Size.z, config.Generate.ChunkSize,
                                     out ChunkGrid grid, out error)) return Fail(report, error);

            // Yükseklikler yalnızca chunk sınırlarını kurmak için bir kez okunur (üretim GPU'da heightmap dokusundan yapar).
            int res = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, res, res);
            if (!ChunkBoundsTable.TryCreate(grid, heights, info.Origin.y, info.Size.y, out ChunkBoundsTable table, out error))
                return Fail(report, error);

            if (!GrassComputeDispatcher.TryCreate(compute, out GrassComputeDispatcher dispatcher, out error)) return Fail(report, error);

            if (!GrassLodMeshSet.TryCreate(config.LodMeshes, out GrassLodMeshSet meshes, out ValidationReport meshReport))
            {
                report.Merge(meshReport);
                return false;
            }
            report.Merge(meshReport);

            int maxChunks = ComputeMaxChunks(config.DrawDistance, config.Generate.ChunkSize, grid.Count);
            system = new GrassDrawSystem(config, material, terrain, info, table, meshes, dispatcher, layer, maxChunks);
            return true;
        }

        static bool Fail(ValidationReport report, string error)
        {
            report.AddError(error);
            return false;
        }

        // Alphamap0 yalnızca 4 kanal taşır: fazla layer ilk 4'e kırpılır (GrassGenerate.compute de yalnızca _Alphamap0 okur).
        static List<string> TerrainLayerNames(TerrainData data)
        {
            TerrainLayer[] layers = data.terrainLayers;
            int n = Mathf.Min(layers.Length, LayerDensityMapper.MaxLayers);
            var names = new List<string>(n);
            for (int i = 0; i < n; i++) names.Add(layers[i] != null ? layers[i].name : "(null)");
            return names;
        }

        GrassCameraContext CreateContext(Camera camera)
        {
            if (!GrassCameraContext.TryCreate(_config.CopyLodInstanceBudgets(), _maxChunks, _boundsTable, _config.DrawDistance,
                                              _chunkPadding, _meshes, out GrassCameraContext context, out string error))
            {
                LogOnce($"'{camera.name}' kamerası için çim kaynakları oluşturulamadı: {error}");
                return null;
            }
            return context;
        }

        /// <summary>
        /// Bir kamera için çimi üretir ve çizim komutlarını gönderir (beginCameraRendering'den çağrılır).
        /// Reddedilen kamera / hata durumunda false: çizim atlanır, oyun düşmez.
        /// </summary>
        public bool Render(Camera camera, IGrassCameraFilter filter)
        {
            if (_disposed || camera == null || filter == null || !filter.ShouldRender(camera)) return false;

            if (_terrain == null || _terrain.terrainData == null)
            {
                LogOnce("Terrain veya TerrainData yok edilmiş; çim çizilmiyor.");
                return false;
            }

            _cameras.PruneDestroyed();
            GrassCameraContext ctx = _cameras.GetOrCreate(camera, _createContext);
            if (ctx == null) return false;

            Vector3 camPos = camera.transform.position;
            GrassViewParams view = ctx.View.Update(camera, _config.CullPadding);
            _lastSelectedChunks = ctx.Selector.Select(camPos, _config.DrawDistance, ctx.View.Frustum);
            if (ctx.Selector.Overflowed) LogOnce($"Chunk seçici kapasiteyi aştı ({_maxChunks}); uzak chunk'lar çizilmiyor olabilir.");
            int chunkCount = ctx.Gpu.UploadChunks(ctx.Selector);

            TerrainData data = _terrain.terrainData;
            if (!_dispatcher.Dispatch(ctx.Gpu, _terrainInfo, data.heightmapTexture, data.GetAlphamapTexture(0), _config.Generate,
                                      _config.Lods, view, chunkCount, out string error))
            {
                LogOnce("Compute dispatch başarısız: " + error);
                return false;
            }

            Bounds bounds = GrassDrawBounds.Compute(camPos, _config.DrawDistance, _terrainInfo.Origin.y,
                                                    _terrainInfo.Origin.y + _terrainInfo.Size.y,
                                                    _config.Generate.HeightRange.y, _boundsMargin);
            for (int lod = 0; lod < _config.Lods.Count; lod++)
            {
                var rp = new RenderParams(_material)
                {
                    camera = camera,
                    layer = _layer,
                    worldBounds = bounds,
                    shadowCastingMode = ShadowCastingMode.Off, // çim gölge atmaz (plan)
                    receiveShadows = true,
                    matProps = ctx.Props(lod),
                };
                // LOD başına ayrı mesh + ayrı args komutu (startCommand = lod) + ayrı instance buffer (matProps).
                Graphics.RenderMeshIndirect(rp, _meshes[lod], ctx.Gpu.Args, 1, lod);
            }
            return true;
        }

        // Terrain fırçası: yalnızca CPU verisi senkronlandığında (synched) yüksekliği yeniden okuyabiliriz;
        // senkronsuz çağrıda GetHeights eski veriyi verirdi.
        void OnHeightmapChanged(Terrain terrain, RectInt heightRegion, bool synched)
        {
            if (_disposed || !synched || terrain != _terrain || terrain.terrainData == null) return;

            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            if (!_boundsTable.TryUpdateRegion(data.GetHeights(0, 0, res, res), heightRegion, out _))
                LogOnce("Terrain yükseklik güncellemesi chunk sınırlarına uygulanamadı (çözünürlük değişmiş olabilir; renderer'ı yeniden kur).");
        }

        void LogOnce(string message)
        {
            if (!_logged.Add(message)) return;
            Debug.LogError("[GrassDrawSystem] " + message);
        }

        /// <summary>Idempotent: olay aboneliğini keser, tüm kamera kaynaklarını ve kendi ürettiği mesh'leri bırakır.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            TerrainCallbacks.heightmapChanged -= _heightmapChanged;
            _cameras.DisposeAll();
            _meshes.Dispose();
            _logged.Clear();
        }
    }
}
