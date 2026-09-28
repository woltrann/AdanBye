using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Compute'a giden chunk satırı. Düzen GrassGenerate.compute içindeki GrassChunk ile BİREBİR aynı (16 bayt).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GrassGpuChunk
    {
        public const int Stride = 16;

        public int cx;
        public int cz;
        /// <summary>Compute okumaz (LOD seyreltmeyle yapılır, hücre seviyesiyle değil); düzen uyumu için taşınır.</summary>
        public int cellLevel;
        public int chunkIndex;
    }

    /// <summary>
    /// Çim üretiminin GPU buffer'larının sahibi: LOD başına ayrı instance buffer'ı, tek durum buffer'ı (LOD başına
    /// sayaç + taşma bayrağı), LOD başına bir indirect komutu içeren args buffer'ı ve chunk listesi.
    /// Yalnızca oluşturma/yükleme/serbest bırakma yapar; dispatch mantığı GrassComputeDispatcher'da. Dispose idempotent;
    /// domain reload'da yönetilmeyen buffer sızmaması için sahibi (bileşen) OnDisable'da Dispose etmelidir.
    /// Neden LOD başına ayrı instance buffer: RenderMeshIndirect'te startInstance yok (WP-3a bulgusu); her LOD kendi
    /// buffer'ıyla (RenderParams.matProps) çizilmelidir.
    /// </summary>
    public sealed class GrassGpuResources : IDisposable
    {
        // Makul üst sınırlar: bozuk bir ayar (ör. 2^31 instance) sürücüyü/VRAM'i çökertmek yerine açık hata versin.
        public const int MaxInstanceCapacity = 1 << 24; // 16M * 32 B = 512 MB (LOD başına)
        public const int MaxChunkCapacity = 65535;      // Dispatch z ekseni sınırı

        /// <summary>Compute'taki GRASS_MAX_LODS ile aynı.</summary>
        public const int MaxLodCount = LodDistanceTable.MaxLodCount;

        /// <summary>State: LOD başına 2 uint: [lod*2] = sayaç, [lod*2+1] = taşma bayrağı (compute ile aynı).</summary>
        public const int StatePerLod = 2;
        public const int StateCount = MaxLodCount * StatePerLod;
        /// <summary>IndirectDrawIndexedArgs: indexCount, instanceCount, startIndex, baseVertex, startInstance.</summary>
        public const int ArgsPerLod = 5;
        public const int ArgsCount = MaxLodCount * ArgsPerLod;

        readonly GraphicsBuffer[] _instances = new GraphicsBuffer[MaxLodCount];
        GraphicsBuffer _state;
        GraphicsBuffer _args;
        GraphicsBuffer _chunks;
        readonly GrassGpuChunk[] _staging; // frame başına allocation olmasın diye tek sefer ayrılır
        readonly int[] _capacities;

        public int LodCount => _capacities.Length;
        public int ChunkCapacity { get; }
        public bool IsDisposed { get; private set; }

        public GraphicsBuffer State => _state;
        /// <summary>LOD başına bir komut (startCommand = lod ile RenderMeshIndirect'e verilir).</summary>
        public GraphicsBuffer Args => _args;
        public GraphicsBuffer Chunks => _chunks;

        /// <summary>lod 0..MaxLodCount-1; LodCount'tan büyük indeksler yer tutucudur (yalnızca compute binding'i için).</summary>
        public GraphicsBuffer Instances(int lod) => _instances[lod];
        public int InstanceCapacity(int lod) => _capacities[lod];

        GrassGpuResources(int[] lodCapacities, int chunkCapacity)
        {
            _capacities = (int[])lodCapacities.Clone();
            ChunkCapacity = chunkCapacity;
            _staging = new GrassGpuChunk[chunkCapacity];
        }

        /// <param name="lodInstanceCapacities">LOD başına görünür instance kapasitesi = o LOD'un bütçesi (1..MaxLodCount eleman).</param>
        public static bool TryCreate(int[] lodInstanceCapacities, int chunkCapacity, out GrassGpuResources resources, out string error)
        {
            resources = null;
            if (!SystemInfo.supportsComputeShaders) { error = "Bu cihaz compute shader desteklemiyor."; return false; }
            if (lodInstanceCapacities == null || lodInstanceCapacities.Length < 1 || lodInstanceCapacities.Length > MaxLodCount)
            {
                error = $"LOD sayısı 1..{MaxLodCount} olmalı.";
                return false;
            }
            for (int i = 0; i < lodInstanceCapacities.Length; i++)
            {
                if (lodInstanceCapacities[i] <= 0 || lodInstanceCapacities[i] > MaxInstanceCapacity)
                {
                    error = $"LOD{i} instanceCapacity 1..{MaxInstanceCapacity} aralığında olmalı (değer {lodInstanceCapacities[i]}).";
                    return false;
                }
            }
            if (chunkCapacity <= 0 || chunkCapacity > MaxChunkCapacity)
            {
                error = $"chunkCapacity 1..{MaxChunkCapacity} aralığında olmalı (değer {chunkCapacity}).";
                return false;
            }

            var created = new GrassGpuResources(lodInstanceCapacities, chunkCapacity);
            try
            {
                // Kullanılmayan LOD slotları için 1 elemanlık yer tutucu: compute'ta üç UAV de bağlı olmalı ve AYNI buffer
                // birden çok UAV slotuna bağlanamaz (D3D11: yinelenen binding'ler düşer, gerçek LOD'a yazma kaybolur;
                // 3b-2'de ölçüldü). Yer tutucuya hiçbir zaman yazılmaz (bütçe 0, lod < _LodCount koşulu).
                for (int i = 0; i < MaxLodCount; i++)
                {
                    int capacity = i < lodInstanceCapacities.Length ? lodInstanceCapacities[i] : 1;
                    created._instances[i] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity, GrassInstance.Stride);
                }
                created._state = new GraphicsBuffer(GraphicsBuffer.Target.Structured, StateCount, sizeof(uint));
                created._args = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, ArgsCount, sizeof(uint));
                created._chunks = new GraphicsBuffer(GraphicsBuffer.Target.Structured, chunkCapacity, GrassGpuChunk.Stride);
            }
            catch (Exception e)
            {
                // Kısmen oluşmuş buffer'lar sızmasın.
                created.Dispose();
                error = "GPU buffer oluşturulamadı: " + e.Message;
                return false;
            }

            // Args şimdilik "0 instance çiz" içerir; mesh bilgisi ConfigureDrawArgs ile gelir. Böylece kurulumdan
            // sonra ama ilk dispatch'ten önce çizim yapılırsa hiçbir şey çizilir, çöp okunmaz.
            created._args.SetData(new uint[ArgsCount]);
            created._state.SetData(new uint[StateCount]);

            resources = created;
            error = null;
            return true;
        }

        /// <summary>
        /// Bir LOD'un args komutunun mesh'e bağlı alanlarını yazar (instanceCount 0; onu her frame compute yazar).
        /// Mesh değişirse yeniden çağır.
        /// </summary>
        public bool ConfigureDrawArgs(int lod, Mesh mesh, int subMeshIndex, out string error)
        {
            if (IsDisposed) { error = "GrassGpuResources dispose edilmiş."; return false; }
            if (lod < 0 || lod >= LodCount) { error = $"Geçersiz LOD {lod} (0..{LodCount - 1})."; return false; }
            if (mesh == null) { error = $"LOD{lod} mesh'i null."; return false; }
            if (subMeshIndex < 0 || subMeshIndex >= mesh.subMeshCount) { error = $"LOD{lod}: geçersiz submesh {subMeshIndex}."; return false; }

            _args.SetData(new[]
            {
                mesh.GetIndexCount(subMeshIndex),
                0u,
                mesh.GetIndexStart(subMeshIndex),
                (uint)mesh.GetBaseVertex(subMeshIndex),
                0u,
            }, 0, lod * ArgsPerLod, ArgsPerLod);
            error = null;
            return true;
        }

        /// <summary>
        /// Seçicinin çıktısını GPU chunk buffer'ına yükler; yüklenen sayıyı döner. Kapasite ctor'da seçicinin
        /// MaxChunks'ına eşit seçilmelidir; yine de aşarsa yazma kapasiteye kırpılır (sınır dışı yazma olmaz).
        /// </summary>
        public int UploadChunks(VisibleChunkSelector selector)
        {
            if (IsDisposed || selector == null) return 0;

            int count = Math.Min(selector.Count, ChunkCapacity);
            for (int i = 0; i < count; i++)
            {
                VisibleChunk c = selector[i];
                _staging[i] = new GrassGpuChunk { cx = c.Cx, cz = c.Cz, cellLevel = c.CellLevel, chunkIndex = c.Index };
            }
            if (count > 0) _chunks.SetData(_staging, 0, 0, count);
            return count;
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            for (int i = 0; i < _instances.Length; i++) ReleaseBuffer(ref _instances[i]);
            ReleaseBuffer(ref _state);
            ReleaseBuffer(ref _args);
            ReleaseBuffer(ref _chunks);
        }

        static void ReleaseBuffer(ref GraphicsBuffer buffer)
        {
            buffer?.Release();
            buffer = null;
        }
    }
}
