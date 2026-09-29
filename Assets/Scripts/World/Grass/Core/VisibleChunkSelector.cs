using System;
using Unity.Mathematics;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>Seçicinin çıktı satırı: chunk koordinatı, düz indeksi ve kameraya XZ uzaklığı².</summary>
    public readonly struct VisibleChunk
    {
        public readonly int Cx;
        public readonly int Cz;
        public readonly int Index;
        public readonly float DistanceSq;

        public VisibleChunk(int cx, int cz, int index, float distanceSq)
        {
            Cx = cx;
            Cz = cz;
            Index = index;
            DistanceSq = distanceSq;
        }
    }

    /// <summary>
    /// Kamera konumu + çizim mesafesi halkası + frustum ile bu frame çim üretilecek chunk listesini çıkarır.
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
        readonly Vector3 _padding; // yarım-genişlik (kenar başına) dolgu; Select'te Bounds.Expand için 2 ile çarpılır
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

        VisibleChunkSelector(ChunkBoundsTable table, int maxChunks, Vector3 padding)
        {
            _table = table;
            _grid = table.Grid;
            _chunks = new VisibleChunk[maxChunks];
            _distSq = new float[maxChunks];
            _padding = padding;
        }

        /// <param name="maxChunks">Çıktı kapasitesi (&gt; 0).</param>
        /// <param name="padding">
        /// Chunk AABB'sini her eksende her iki yönde genişletir (m). Neden: bounds yalnızca zemin yüksekliğini kapsar,
        /// çim bıçakları yukarı uzanır (dikey) ve rüzgar/genişlik yüzünden chunk sınırının dışına taşar (yatay);
        /// dolgu olmazsa ekran kenarında çim aniden kesilir.
        /// </param>
        public static bool TryCreate(ChunkBoundsTable table, int maxChunks,
                                     Vector3 padding, out VisibleChunkSelector selector, out string error)
        {
            selector = null;
            if (table == null) { error = "ChunkBoundsTable null."; return false; }
            if (maxChunks <= 0) { error = "maxChunks > 0 olmalı."; return false; }
            if (!IsValidPadding(padding.x) || !IsValidPadding(padding.y) || !IsValidPadding(padding.z))
            {
                error = "padding bileşenleri sonlu ve >= 0 olmalı.";
                return false;
            }

            selector = new VisibleChunkSelector(table, maxChunks, padding);
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
            Vector3 padding = _padding * 2f; // Bounds.Expand toplam boyuta eklenir

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

                    Add(new VisibleChunk(cx, cz, _grid.ToIndex(cx, cz), distSq));
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

        static bool IsValidPadding(float v) => v >= 0f && !float.IsInfinity(v); // NaN >= 0 false

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
