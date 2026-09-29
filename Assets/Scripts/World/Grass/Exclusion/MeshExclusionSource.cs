using System;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Mesh üçgenlerini XZ'ye yansıtıp doldurur, sonra margin kadar genişletir (göl kıyısında çim kalmasın).
    /// Neden geçici canvas: Dilate tüm mask'ı büyütür; başka kaynakların (ağaç) alanını da şişirmemek için
    /// yalnız bu kaynağın katmanı genişletilip max ile birleştirilir.
    /// </summary>
    public sealed class MeshExclusionSource : IGrassExclusionSource
    {
        readonly Mesh _mesh;
        readonly Matrix4x4 _localToWorld;
        readonly float _margin;
        readonly ITerrainHeightProvider _heights;

        /// <param name="heights">Verilirse yalnız terrain yüksekliği su yüzeyinin (mesh world Y ortalaması) altındaki texel'ler maskelenir.</param>
        public MeshExclusionSource(Mesh mesh, Matrix4x4 localToWorld, float margin = 1.0f,
            ITerrainHeightProvider heights = null)
        {
            _heights = heights;
            _mesh = mesh != null ? mesh : throw new ArgumentNullException(nameof(mesh));
            _localToWorld = localToWorld;
            _margin = Mathf.Max(0f, margin);
        }

        public void Rasterize(ExclusionMaskCanvas canvas)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (!_mesh.isReadable)
                throw new InvalidOperationException($"Mesh '{_mesh.name}' okunabilir değil (Read/Write kapalı).");

            Vector3[] verts = _mesh.vertices;
            var world = new Vector2[verts.Length];
            float sumY = 0f;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 w = _localToWorld.MultiplyPoint3x4(verts[i]);
                world[i] = new Vector2(w.x, w.z);
                sumY += w.y;
                min = Vector2.Min(min, world[i]);
                max = Vector2.Max(max, world[i]);
            }

            ExclusionMaskCanvas layer = canvas.CreateEmptyLike();
            for (int s = 0; s < _mesh.subMeshCount; s++)
            {
                int[] idx = _mesh.GetTriangles(s);
                for (int i = 0; i + 2 < idx.Length; i += 3)
                    layer.FillTriangleXZ(world[idx[i]], world[idx[i + 1]], world[idx[i + 2]]);
            }
            // Önce yükseklikle kırp, sonra dilate: marj yalnız gerçek su kenarından yayılsın.
            if (_heights != null && verts.Length > 0)
                ClipToWaterLevel(layer, min, max, sumY / verts.Length);
            layer.Dilate(_margin);
            canvas.MaxMerge(layer);
        }

        // Neden mesh ortalama Y: göl düz plane; tek su seviyesi yeterli. Yalnız bbox içindeki dolu texel'lerde
        // SampleHeight çağrılır (4M texel'in tamamı değil).
        void ClipToWaterLevel(ExclusionMaskCanvas layer, Vector2 min, Vector2 max, float waterY)
        {
            int res = layer.Resolution;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(layer.WorldToTexelX(min.x)) - 1, 0, res - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(layer.WorldToTexelX(max.x)) + 1, 0, res - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(layer.WorldToTexelZ(min.y)) - 1, 0, res - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt(layer.WorldToTexelZ(max.y)) + 1, 0, res - 1);
            byte[] data = layer.Data;
            for (int tz = z0; tz <= z1; tz++)
            {
                float wz = layer.TexelToWorldZ(tz);
                for (int tx = x0; tx <= x1; tx++)
                {
                    int i = tz * res + tx;
                    if (data[i] == 0) continue;
                    if (_heights.SampleHeightWorld(layer.TexelToWorldX(tx), wz) >= waterY) data[i] = 0;
                }
            }
        }
    }
}
