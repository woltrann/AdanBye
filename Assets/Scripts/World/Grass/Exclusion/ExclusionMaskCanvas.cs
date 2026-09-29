using System;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Terrain'e hizalı, tek kanallı (R8) exclusion mask'ının CPU tarafı. 255 = çim yok, 0 = serbest.
    /// Neden ayrı saf sınıf: kaynaklar (ağaç, göl...) GPU/asset'ten bağımsız test edilebilsin; baker (WP-6c)
    /// yalnızca <see cref="Data"/>'yı texture'a yazar.
    ///
    /// Eşleme sözleşmesi: dünya origin -> texel 0, origin+size -> son texel (res-1). Yani texel MERKEZLERİ
    /// [origin, origin+size] aralığına gerilir; texel boyu size/(res-1). Compute tarafı aynı formülle
    /// (pos-origin)/size*(res-1) örneklemeli (point filter) yoksa yarım texel kayar.
    /// Satır düzeni: data[z * res + x] (x = dünya X, z = dünya Z).
    /// </summary>
    public sealed class ExclusionMaskCanvas
    {
        readonly byte[] _data;

        public Vector2 Origin { get; }
        public Vector2 Size { get; }
        public int Resolution { get; }
        public float TexelSize { get; }
        public byte[] Data => _data;

        public ExclusionMaskCanvas(Vector2 originXZ, Vector2 sizeXZ, int resolution)
        {
            if (resolution < 2) throw new ArgumentOutOfRangeException(nameof(resolution), "Çözünürlük en az 2 olmalı.");
            if (sizeXZ.x <= 0f || sizeXZ.y <= 0f) throw new ArgumentOutOfRangeException(nameof(sizeXZ), "Boyut pozitif olmalı.");

            Origin = originXZ;
            Size = sizeXZ;
            Resolution = resolution;
            // Kare texel varsayımı (terrain 1000x1000): X ve Z boyutları farklıysa yarıçap/margin dönüşümü belirsizleşir.
            TexelSize = sizeXZ.x / (resolution - 1);
            _data = new byte[resolution * resolution];
        }

        /// <summary>Aynı geometriye sahip boş canvas (geçici katman için, sonra <see cref="MaxMerge"/>).</summary>
        public ExclusionMaskCanvas CreateEmptyLike() => new ExclusionMaskCanvas(Origin, Size, Resolution);

        public float WorldToTexelX(float worldX) => (worldX - Origin.x) / Size.x * (Resolution - 1);
        public float WorldToTexelZ(float worldZ) => (worldZ - Origin.y) / Size.y * (Resolution - 1);
        public float TexelToWorldX(int tx) => Origin.x + tx * Size.x / (Resolution - 1);
        public float TexelToWorldZ(int tz) => Origin.y + tz * Size.y / (Resolution - 1);

        public byte GetTexel(int tx, int tz) => _data[tz * Resolution + tx];

        /// <summary>En yakın texel'i okur; terrain dışı 0 döner.</summary>
        public byte SampleWorld(float worldX, float worldZ)
        {
            int tx = (int)Math.Round(WorldToTexelX(worldX));
            int tz = (int)Math.Round(WorldToTexelZ(worldZ));
            if (tx < 0 || tz < 0 || tx >= Resolution || tz >= Resolution) return 0;
            return _data[tz * Resolution + tx];
        }

        public void Clear() => Array.Clear(_data, 0, _data.Length);

        /// <summary>
        /// Disk basar: merkezden radius'a kadar 255, radius..radius+falloff arası doğrusal 255->0.
        /// Neden max: üst üste binen şekiller (iki ağaç) birbirinin değerini silmemeli.
        /// </summary>
        public void StampDisk(Vector2 centerXZ, float radius, float falloff)
        {
            if (radius < 0f || falloff < 0f || float.IsNaN(radius) || float.IsNaN(falloff)) return;
            float outer = radius + falloff;
            if (!TryTexelRange(centerXZ.x - outer, centerXZ.x + outer, centerXZ.y - outer, centerXZ.y + outer,
                    out int x0, out int x1, out int z0, out int z1)) return;

            for (int tz = z0; tz <= z1; tz++)
            {
                float dz = TexelToWorldZ(tz) - centerXZ.y;
                int row = tz * Resolution;
                for (int tx = x0; tx <= x1; tx++)
                {
                    float dx = TexelToWorldX(tx) - centerXZ.x;
                    float d = (float)Math.Sqrt(dx * dx + dz * dz);
                    byte v;
                    if (d <= radius) v = 255;
                    else if (d >= outer) continue;
                    else v = (byte)Math.Round(255f * (1f - (d - radius) / falloff));
                    if (v > _data[row + tx]) _data[row + tx] = v;
                }
            }
        }

        /// <summary>Üçgeni (sarım yönünden bağımsız) XZ düzleminde 255 ile doldurur; texel merkezi içerideyse yazar.</summary>
        public void FillTriangleXZ(Vector2 a, Vector2 b, Vector2 c)
        {
            float minX = Math.Min(a.x, Math.Min(b.x, c.x)), maxX = Math.Max(a.x, Math.Max(b.x, c.x));
            float minZ = Math.Min(a.y, Math.Min(b.y, c.y)), maxZ = Math.Max(a.y, Math.Max(b.y, c.y));
            if (!TryTexelRange(minX, maxX, minZ, maxZ, out int x0, out int x1, out int z0, out int z1)) return;

            float area = Edge(a, b, c);
            if (Math.Abs(area) < 1e-12f) return; // Dejenere üçgen alan kaplamaz.
            float sign = area > 0f ? 1f : -1f;

            for (int tz = z0; tz <= z1; tz++)
            {
                float pz = TexelToWorldZ(tz);
                int row = tz * Resolution;
                for (int tx = x0; tx <= x1; tx++)
                {
                    var p = new Vector2(TexelToWorldX(tx), pz);
                    // Kenar üstü (==0) dahil: komşu üçgenler arasında dikişte delik kalmasın.
                    if (Edge(a, b, p) * sign >= 0f && Edge(b, c, p) * sign >= 0f && Edge(c, a, p) * sign >= 0f)
                        _data[row + tx] = 255;
                }
            }
        }

        /// <summary>
        /// Maskeyi marginM metre genişletir (daire yapı elemanıyla max filtre). Texel merkezi mesafesi
        /// &lt;= margin olan komşu dahil, böylece margin = 1 texel boyu tam 1 texel büyütür.
        /// </summary>
        public void Dilate(float marginM)
        {
            if (marginM <= 0f || float.IsNaN(marginM)) return;
            float rTexel = marginM / TexelSize;
            int r = (int)Math.Ceiling(rTexel);
            // Satır başına yatay yarı-genişlik tablosu: iç döngüde sqrt yok.
            int[] halfWidth = new int[2 * r + 1];
            float r2 = rTexel * rTexel + 1e-4f;
            for (int dz = -r; dz <= r; dz++)
                halfWidth[dz + r] = (int)Math.Floor(Math.Sqrt(Math.Max(0f, r2 - dz * dz)));

            byte[] src = (byte[])_data.Clone();
            int res = Resolution;
            for (int tz = 0; tz < res; tz++)
            {
                for (int tx = 0; tx < res; tx++)
                {
                    byte best = 0;
                    for (int dz = -r; dz <= r && best < 255; dz++)
                    {
                        int nz = tz + dz;
                        if (nz < 0 || nz >= res) continue;
                        int hw = halfWidth[dz + r];
                        int xa = Math.Max(0, tx - hw), xb = Math.Min(res - 1, tx + hw);
                        int row = nz * res;
                        for (int nx = xa; nx <= xb; nx++)
                            if (src[row + nx] > best) best = src[row + nx];
                    }
                    _data[tz * res + tx] = best;
                }
            }
        }

        /// <summary>Başka canvas'ı texel bazında max ile birleştirir (aynı geometri şart).</summary>
        public void MaxMerge(ExclusionMaskCanvas other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (other.Resolution != Resolution || other.Origin != Origin || other.Size != Size)
                throw new ArgumentException("Canvas geometrisi uyuşmuyor.", nameof(other));
            for (int i = 0; i < _data.Length; i++)
                if (other._data[i] > _data[i]) _data[i] = other._data[i];
        }

        // Dünya kutusunu texel aralığına çevirir ve terrain'e kırpar; tamamen dışarıdaysa false.
        bool TryTexelRange(float minX, float maxX, float minZ, float maxZ, out int x0, out int x1, out int z0, out int z1)
        {
            x0 = x1 = z0 = z1 = 0;
            float sum = minX + maxX + minZ + maxZ;
            if (float.IsNaN(sum) || float.IsInfinity(sum)) return false;
            double fx0 = Math.Ceiling((double)WorldToTexelX(minX)), fx1 = Math.Floor((double)WorldToTexelX(maxX));
            double fz0 = Math.Ceiling((double)WorldToTexelZ(minZ)), fz1 = Math.Floor((double)WorldToTexelZ(maxZ));
            // double üzerinde kırp: devasa değerler int'e taşmadan terrain sınırına iner.
            x0 = (int)Math.Max(0.0, fx0); x1 = (int)Math.Min(Resolution - 1.0, fx1);
            z0 = (int)Math.Max(0.0, fz0); z1 = (int)Math.Min(Resolution - 1.0, fz1);
            return x0 <= x1 && z0 <= z1;
        }

        static float Edge(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    }
}
