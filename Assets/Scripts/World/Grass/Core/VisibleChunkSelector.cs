using System;
using Unity.Mathematics;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>Seçicinin çıktı satırı: chunk koordinatı, hücre seviyesi (0/1/2) ve kameraya XZ uzaklığı².</summary>
    public readonly struct VisibleChunk
    {
        public readonly int Cx;
        public readonly int Cz;
        public readonly int Index;
        public readonly int CellLevel;
        public readonly float DistanceSq;

        public VisibleChunk(int cx, int cz, int index, int cellLevel, float distanceSq)
        {
            Cx = cx;
            Cz = cz;
            Index = index;
            CellLevel = cellLevel;
            DistanceSq = distanceSq;
        }
    }

    /// <summary>
    /// Kamera konumu + çizim mesafesi halkası + frustum ile bu frame çim üretilecek chunk listesini çıkarır ve her
    /// chunk'a mesafeye göre hücre seviyesi (0 = en sık, 2 = en seyrek) atar.
    /// Frame başına GC allocation YOK: çıktı dizisi ctor'da bir kez ayrılır, Select yalnızca doldurur.
    /// Halka yatay (XZ) mesafedir; yükseklik farkı çim yoğunluğunu değil yalnızca frustum testini ilgilendirir.
    /// Chunk mesafesi chunk dikdörtgenine EN YAKIN nokta üzerinden ölçülür: chunk'ın yakın ucu halkadaysa çim
    /// orada üretilebilir; merkez ölçümü kenarda çim eksilmesine yol açardı.
    /// </summary>
    public sealed class VisibleChunkSelector
    {
        readonly ChunkBoundsTable _table;
        readonly ChunkGrid _grid;
        readonly VisibleChunk[] _chunks;
        readonly float[] _distSq; // _chunks ile paralel; taşmada en uzağı bulmak için
        readonly float _level1StartSq;
        readonly float _level2StartSq;
        readonly float _verticalPadding;
        int _worstIndex;

        /// <summary>Son Select çağrısındaki chunk sayısı.</summary>
        public int Count { get; private set; }

        /// <summary>
        /// Son Select'te aday sayısı maxChunks'ı aştı mı. Aşarsa EN YAKIN maxChunks chunk tutulur (uzaklar
        /// düşer): yakın çim görünür kalır, taşma tanılamada raporlanır.
        /// </summary>
        public bool Overflowed { get; private set; }

        public int MaxChunks => _chunks.Length;

        public VisibleChunk this[int index] => _chunks[index];

        /// <summary>Geçerli girişler; alan ayırmaz (dizi dilimi).</summary>
        public ReadOnlySpan<VisibleChunk> Chunks => new ReadOnlySpan<VisibleChunk>(_chunks, 0, Count);

        VisibleChunkSelector(ChunkBoundsTable table, int maxChunks, float level1Start, float level2Start, float verticalPadding)
        {
            _table = table;
            _grid = table.Grid;
            _chunks = new VisibleChunk[maxChunks];
            _distSq = new float[maxChunks];
            _level1StartSq = level1Start * level1Start;
            _level2StartSq = level2Start * level2Start;
            _verticalPadding = verticalPadding;
        }

        /// <param name="maxChunks">Çıktı kapasitesi (&gt; 0).</param>
        /// <param name="level1Start">Hücre seviyesi 1'in başladığı mesafe (m); &gt; 0.</param>
        /// <param name="level2Start">Hücre seviyesi 2'nin başladığı mesafe (m); level1Start'tan büyük.</param>
        /// <param name="verticalPadding">
        /// Chunk AABB'sini yukarı/aşağı genişletir (m). Neden: bounds yalnızca zemin yüksekliğini kapsar, çim
        /// bıçakları bunun üstüne uzanır; dolgu olmazsa ekran kenarında çim aniden kesilir.
        /// </param>
        public static bool TryCreate(ChunkBoundsTable table, int maxChunks, float level1Start, float level2Start,
                                     float verticalPadding, out VisibleChunkSelector selector, out string error)
        {
            selector = null;
            if (table == null) { error = "ChunkBoundsTable null."; return false; }
            if (maxChunks <= 0) { error = "maxChunks > 0 olmalı."; return false; }
            if (!(level1Start > 0f) || float.IsInfinity(level1Start)) { error = "level1Start sonlu ve > 0 olmalı."; return false; }
            if (!(level2Start > level1Start) || float.IsInfinity(level2Start)) { error = "level2Start, level1Start'tan büyük ve sonlu olmalı."; return false; }
            if (!(verticalPadding >= 0f) || float.IsInfinity(verticalPadding)) { error = "verticalPadding sonlu ve >= 0 olmalı."; return false; }

            selector = new VisibleChunkSelector(table, maxChunks, level1Start, level2Start, verticalPadding);
            error = null;
            return true;
        }

        /// <summary>
        /// Görünür chunk'ları seçer ve sayısını döner. Geçersiz girdi (NaN konum, drawDistance &lt;= 0/NaN/sonsuz)
        /// per-frame yolda istisna atmak yerine 0 chunk döner: bozuk bir frame oyunu düşürmemeli.
        /// <paramref name="frustum"/> null ise yalnızca halka uygulanır.
        /// </summary>
        public int Select(Vector3 cameraPosition, float drawDistance, IFrustum frustum)
        {
            Count = 0;
            Overflowed = false;

            if (!IsFinite(cameraPosition.x) || !IsFinite(cameraPosition.z)) return 0;
            if (!(drawDistance > 0f) || float.IsInfinity(drawDistance)) return 0;

            float camX = cameraPosition.x;
            float camZ = cameraPosition.z;
            if (!_grid.TryGetChunkRange(camX - drawDistance, camZ - drawDistance, camX + drawDistance, camZ + drawDistance,
                                        out int2 lo, out int2 hi))
            {
                return 0; // kamera + halka terrain'in tamamen dışında
            }

            float drawSq = drawDistance * drawDistance;
            var padding = new Vector3(0f, _verticalPadding * 2f, 0f); // Bounds.Expand toplam boyuta eklenir

            for (int cz = lo.y; cz <= hi.y; cz++)
            {
                for (int cx = lo.x; cx <= hi.x; cx++)
                {
                    _grid.GetChunkRect(cx, cz, out float minX, out float minZ, out float maxX, out float maxZ);
                    float dx = math.max(math.max(minX - camX, camX - maxX), 0f);
                    float dz = math.max(math.max(minZ - camZ, camZ - maxZ), 0f);
                    float distSq = dx * dx + dz * dz;
                    if (distSq > drawSq) continue;

                    if (frustum != null)
                    {
                        Bounds bounds = _table.GetBounds(cx, cz);
                        bounds.Expand(padding);
                        if (!frustum.Intersects(bounds)) continue;
                    }

                    int level = distSq >= _level2StartSq ? 2 : (distSq >= _level1StartSq ? 1 : 0);
                    Add(new VisibleChunk(cx, cz, _grid.ToIndex(cx, cz), level, distSq));
                }
            }
            return Count;
        }

        void Add(in VisibleChunk chunk)
        {
            if (Count < _chunks.Length)
            {
                _chunks[Count] = chunk;
                _distSq[Count] = chunk.DistanceSq;
                Count++;
                if (Count == _chunks.Length) _worstIndex = FindWorst();
                return;
            }

            Overflowed = true;
            // Kapasite dolu: yalnızca mevcut en uzaktan daha yakınsa onun yerini al (en yakın N korunur).
            if (chunk.DistanceSq >= _distSq[_worstIndex]) return;
            _chunks[_worstIndex] = chunk;
            _distSq[_worstIndex] = chunk.DistanceSq;
            _worstIndex = FindWorst();
        }

        int FindWorst()
        {
            int worst = 0;
            for (int i = 1; i < Count; i++)
                if (_distSq[i] > _distSq[worst]) worst = i;
            return worst;
        }

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
