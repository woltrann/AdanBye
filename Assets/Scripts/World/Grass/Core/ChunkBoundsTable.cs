using Unity.Mathematics;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Chunk başına dünya-uzayı AABB tablosu (XZ terrain'e kırpılmış, Y = chunk'ın kapsadığı yükseklik
    /// örneklerinin min/max'ı). Görünürlük seçicisi frustum testini bununla yapar: tüm terrain'i tek kutu
    /// sanmak dağların arkasındaki chunk'ları, çok gevşek kutu ise yanlış-pozitifleri artırırdı.
    /// Girdi ham float[,] (TerrainData.GetHeights sözleşmesi: heights[z, x], 0..1 normalize) + ölçek;
    /// Terrain nesnesine bağımlı değil. Tablo yükseklik dizisini SAKLAMAZ (mutable dizi tutmak gizli bağ olurdu);
    /// düzenlemede <see cref="TryUpdateRegion"/> güncel diziyi yeniden alır.
    /// </summary>
    public sealed class ChunkBoundsTable
    {
        readonly ChunkGrid _grid;
        readonly int _resX;
        readonly int _resZ;
        readonly float _originY;
        readonly float _heightScale;
        readonly float _sampleSpacingX;
        readonly float _sampleSpacingZ;
        readonly float[] _minY;
        readonly float[] _maxY;

        public ChunkGrid Grid => _grid;

        ChunkBoundsTable(ChunkGrid grid, int resX, int resZ, float originY, float heightScale)
        {
            _grid = grid;
            _resX = resX;
            _resZ = resZ;
            _originY = originY;
            _heightScale = heightScale;
            _sampleSpacingX = grid.SizeX / (resX - 1);
            _sampleSpacingZ = grid.SizeZ / (resZ - 1);
            _minY = new float[grid.Count];
            _maxY = new float[grid.Count];
        }

        /// <param name="heights">heights[z, x], 0..1 (TerrainData.GetHeights). En az 2x2.</param>
        /// <param name="originY">Terrain'in dünya Y konumu.</param>
        /// <param name="heightScale">Terrain size.y (ör. 600): dünya Y = originY + h * heightScale.</param>
        public static bool TryCreate(ChunkGrid grid, float[,] heights, float originY, float heightScale,
                                     out ChunkBoundsTable table, out string error)
        {
            table = null;
            if (!ValidateInputs(grid, heights, originY, heightScale, out error)) return false;

            var created = new ChunkBoundsTable(grid, heights.GetLength(1), heights.GetLength(0), originY, heightScale);
            for (int cz = 0; cz < grid.CountZ; cz++)
                for (int cx = 0; cx < grid.CountX; cx++)
                    created.RecomputeChunk(heights, cx, cz);

            table = created;
            return true;
        }

        /// <summary>
        /// Yalnızca <paramref name="sampleRegion"/> (heightmap örnek koordinatı, TerrainData'nın heightRegion'ı ile
        /// aynı: x, y=z) ile örnek paylaşan chunk'ları yeniden hesaplar. Neden tam yeniden kurmak değil: terrain
        /// fırçası bir frame'de küçük bir alanı değiştirir; 513x513'ü baştan taramak gereksiz. Region terrain
        /// dışına taşıyorsa kırpılır. Güncellenen chunk sayısını döner.
        /// </summary>
        public bool TryUpdateRegion(float[,] heights, RectInt sampleRegion, out int updatedChunks)
        {
            updatedChunks = 0;
            if (heights == null || heights.GetLength(1) != _resX || heights.GetLength(0) != _resZ) return false;

            int xMin = math.max(sampleRegion.xMin, 0);
            int zMin = math.max(sampleRegion.yMin, 0);
            int xMax = math.min(sampleRegion.xMax - 1, _resX - 1); // RectInt.xMax dışlayıcı
            int zMax = math.min(sampleRegion.yMax - 1, _resZ - 1);
            if (xMin > xMax || zMin > zMax) return true; // kırpma sonrası boş: yapılacak iş yok, hata da değil

            // Aday chunk'lar: bölgedeki örneklerin dünya aralığı, bir örnek aralığı kadar genişletilmiş
            // (chunk'ın bounds'u kenar dışındaki komşu örneği de kapsayabilir). Sonra kesin örtüşme süzülür.
            float wMinX = _grid.OriginX + (xMin - 1) * _sampleSpacingX;
            float wMaxX = _grid.OriginX + (xMax + 1) * _sampleSpacingX;
            float wMinZ = _grid.OriginZ + (zMin - 1) * _sampleSpacingZ;
            float wMaxZ = _grid.OriginZ + (zMax + 1) * _sampleSpacingZ;
            if (!_grid.TryGetChunkRange(wMinX, wMinZ, wMaxX, wMaxZ, out int2 lo, out int2 hi)) return true;

            for (int cz = lo.y; cz <= hi.y; cz++)
            {
                for (int cx = lo.x; cx <= hi.x; cx++)
                {
                    GetSampleRange(cx, cz, out int i0, out int i1, out int j0, out int j1);
                    bool overlaps = i0 <= xMax && i1 >= xMin && j0 <= zMax && j1 >= zMin;
                    if (!overlaps) continue;
                    RecomputeChunk(heights, cx, cz);
                    updatedChunks++;
                }
            }
            return true;
        }

        public float GetMinY(int cx, int cz) => _minY[_grid.ToIndex(cx, cz)];
        public float GetMaxY(int cx, int cz) => _maxY[_grid.ToIndex(cx, cz)];

        /// <summary>Chunk'ın dünya AABB'si (allocation yok; geçerli chunk koordinatı çağıranın sorumluluğu).</summary>
        public Bounds GetBounds(int cx, int cz)
        {
            _grid.GetChunkRect(cx, cz, out float x0, out float z0, out float x1, out float z1);
            int index = _grid.ToIndex(cx, cz);
            float y0 = _minY[index];
            float y1 = _maxY[index];
            var center = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
            var size = new Vector3(x1 - x0, y1 - y0, z1 - z0);
            return new Bounds(center, size);
        }

        static bool ValidateInputs(ChunkGrid grid, float[,] heights, float originY, float heightScale, out string error)
        {
            if (grid.Count <= 0) { error = "ChunkGrid geçersiz (default)."; return false; }
            if (heights == null) { error = "Yükseklik dizisi null."; return false; }
            if (heights.GetLength(0) < 2 || heights.GetLength(1) < 2)
            {
                error = "Yükseklik dizisi en az 2x2 olmalı (aralık hesabı örnek sayısı-1'e bölünür).";
                return false;
            }
            if (float.IsNaN(originY) || float.IsInfinity(originY)) { error = "originY sonlu olmalı."; return false; }
            if (float.IsNaN(heightScale) || float.IsInfinity(heightScale) || heightScale <= 0f)
            {
                error = "heightScale (terrain size.y) sonlu ve > 0 olmalı.";
                return false;
            }
            error = null;
            return true;
        }

        // Chunk'ın yüzeyini belirleyen örnek aralığı (dahil). Yüzey örnekler arasında interpolasyonla oluşur, bu
        // yüzden chunk kenarını çevreleyen floor/ceil örnekleri de dahil edilir; bu sayede min/max asla dar kalmaz.
        // Kayan nokta hatası en fazla bir örnek FAZLA dahil eder (muhafazakâr), eksik dahil etmez.
        void GetSampleRange(int cx, int cz, out int i0, out int i1, out int j0, out int j1)
        {
            _grid.GetChunkRect(cx, cz, out float x0, out float z0, out float x1, out float z1);
            i0 = math.clamp((int)math.floor((x0 - _grid.OriginX) / _sampleSpacingX), 0, _resX - 1);
            i1 = math.clamp((int)math.ceil((x1 - _grid.OriginX) / _sampleSpacingX), 0, _resX - 1);
            j0 = math.clamp((int)math.floor((z0 - _grid.OriginZ) / _sampleSpacingZ), 0, _resZ - 1);
            j1 = math.clamp((int)math.ceil((z1 - _grid.OriginZ) / _sampleSpacingZ), 0, _resZ - 1);
        }

        void RecomputeChunk(float[,] heights, int cx, int cz)
        {
            GetSampleRange(cx, cz, out int i0, out int i1, out int j0, out int j1);

            float min = float.MaxValue;
            float max = float.MinValue;
            for (int j = j0; j <= j1; j++)
            {
                for (int i = i0; i <= i1; i++)
                {
                    float h = heights[j, i];
                    // NaN/Inf örnek sınırları bozmasın diye atlanır; hiç geçerli örnek yoksa düz zemine düşer.
                    if (float.IsNaN(h) || float.IsInfinity(h)) continue;
                    if (h < min) min = h;
                    if (h > max) max = h;
                }
            }

            int index = _grid.ToIndex(cx, cz);
            if (min > max)
            {
                _minY[index] = _originY;
                _maxY[index] = _originY;
                return;
            }
            _minY[index] = _originY + min * _heightScale;
            _maxY[index] = _originY + max * _heightScale;
        }
    }
}
