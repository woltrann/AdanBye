using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Kodla üretilen varsayılan blade mesh'i (kalıcı; Spike'tan bağımsız): pivot kökte, +Y yukarı, ~1 birim yükseklik ve
    /// 1 birim taban genişliği (instance yüksekliği/genişliği instance verisinden ölçeklenir), uca doğru daralan,
    /// hafif öne kavisli. Mesh çağıranın sahipliğindedir (Destroy edilmeli).
    /// </summary>
    public static class GrassDefaultBladeMesh
    {
        public static Mesh Create(int segments)
        {
            segments = Mathf.Max(1, segments);
            int vertexCount = segments * 2 + 1; // her segment seviyesinde sol/sağ + tek uç vertex
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var indices = new int[(segments - 1) * 6 + 3];

            const float bend = 0.25f; // z = bend * y^2
            for (int i = 0; i < segments; i++)
            {
                float y = (float)i / segments;
                float halfWidth = 0.5f * (1f - y * 0.8f);
                float z = bend * y * y;
                Vector3 n = new Vector3(0f, -2f * bend * y, 1f).normalized;

                int l = i * 2, r = i * 2 + 1;
                vertices[l] = new Vector3(-halfWidth, y, z);
                vertices[r] = new Vector3(halfWidth, y, z);
                normals[l] = normals[r] = n;
                uvs[l] = new Vector2(0f, y);
                uvs[r] = new Vector2(1f, y);
            }

            int tip = segments * 2;
            vertices[tip] = new Vector3(0f, 1f, bend);
            normals[tip] = new Vector3(0f, -2f * bend, 1f).normalized;
            uvs[tip] = new Vector2(0.5f, 1f);

            int idx = 0;
            for (int i = 0; i < segments - 1; i++)
            {
                int l0 = i * 2, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
                indices[idx++] = l0; indices[idx++] = l1; indices[idx++] = r0;
                indices[idx++] = r0; indices[idx++] = l1; indices[idx++] = r1;
            }
            int lt = (segments - 1) * 2, rt = lt + 1;
            indices[idx++] = lt; indices[idx++] = tip; indices[idx++] = rt;

            // DontSave: sahneye/asset'e sızmasın; sahibi Dispose'ta yok eder.
            var mesh = new Mesh { name = "GRASS_Blade" + segments, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            // Gerçek sınır instance verisinden gelir; RenderParams.worldBounds cull'u ayrı yönetir.
            mesh.bounds = new Bounds(new Vector3(0f, 0.5f, 0.1f), new Vector3(1f, 1f, 0.3f));
            return mesh;
        }
    }

    /// <summary>
    /// Kullanıcının verdiği blade mesh'inin sözleşmeye uyup uymadığını denetler (saf mantık; Mesh'e yazmaz).
    /// Sözleşme: pivot kökte (0,0,0), +Y yukarı, ~1 birim yükseklik; instance ölçeği (width, height, width) ile
    /// uygulandığı için mesh "birim boyutta" olmalıdır. Bounds/vertexCount okumak isReadable GEREKTİRMEZ.
    /// </summary>
    public static class GrassMeshContract
    {
        /// <summary>Pivot kökte değilse bile blade'i tamamen bozmayan tolerans (metre, birim mesh uzayında).</summary>
        public const float MinYTolerance = -0.05f;
        public const float MinHeight = 0.25f;
        public const float MaxHeight = 4f;

        /// <summary>Mesh hiç çizilemezse false + hata metni.</summary>
        public static bool IsUsable(int vertexCount, out string error)
        {
            error = vertexCount > 0 ? null : "Mesh'te vertex yok (vertexCount=0).";
            return vertexCount > 0;
        }

        /// <summary>Çizilebilir ama sözleşmeyi ihlal eden durumlar için uyarı metinleri ekler.</summary>
        public static void CollectWarnings(int subMeshCount, Bounds bounds, List<string> warnings)
        {
            if (subMeshCount > 1)
                warnings.Add($"subMeshCount={subMeshCount}; yalnızca submesh 0 çizilir.");

            if (bounds.min.y < MinYTolerance)
                warnings.Add($"pivot kökte değil: bounds.min.y={bounds.min.y:F3} (beklenen ~0; taban yerin altına gömülür).");

            float height = bounds.size.y;
            if (height < MinHeight || height > MaxHeight)
                warnings.Add($"ölçek beklenenden farklı: bounds.size.y={height:F3} (beklenen ~1; {MinHeight}..{MaxHeight} dışı). " +
                             "Instance ölçeği (width,height,width) ile uygulandığından blade çok küçük/büyük görünür.");
        }
    }

    /// <summary>
    /// LOD başına ÇİZİLECEK mesh'lerin çözümü ve sahipliği: ayarda boş bırakılan LOD'lar varsayılanla doldurulur
    /// (LOD0: 6 segmentli blade, LOD1: 3 segmentli blade, LOD2: üçgen), kullanıcı mesh'leri sözleşmeye karşı denetlenir.
    /// Sahiplik: yalnızca kodla ürettiğimiz mesh'ler Dispose'ta yok edilir; kullanıcının asset mesh'i ASLA Destroy edilmez.
    /// Sonuç raporu: hata = çizilemez mesh (renderer çizmez), uyarı = sözleşme ihlali (renderer tek sefer loglar).
    /// </summary>
    public sealed class GrassLodMeshSet : IDisposable
    {
        readonly Mesh[] _meshes;
        readonly bool[] _owned;

        public int Count => _meshes.Length;
        public Mesh this[int lod] => _meshes[lod];
        public bool IsOwned(int lod) => _owned[lod];

        GrassLodMeshSet(int count)
        {
            _meshes = new Mesh[count];
            _owned = new bool[count];
        }

        /// <param name="configured">Ayardaki LOD mesh'leri (eleman null = varsayılan). Uzunluk 1..LodDistanceTable.MaxLodCount.</param>
        public static bool TryCreate(IReadOnlyList<Mesh> configured, out GrassLodMeshSet set, out ValidationReport report)
        {
            set = null;
            report = new ValidationReport();

            int count = configured != null ? configured.Count : 0;
            if (count < 1 || count > LodDistanceTable.MaxLodCount)
            {
                report.AddError($"LOD mesh listesi 1..{LodDistanceTable.MaxLodCount} elemanlı olmalı (değer {count}).");
                return false;
            }

            var created = new GrassLodMeshSet(count);
            var warnings = new List<string>(3);
            for (int lod = 0; lod < count; lod++)
            {
                Mesh user = configured[lod];
                if (user == null)
                {
                    created._meshes[lod] = CreateDefault(lod);
                    created._owned[lod] = true;
                    continue;
                }

                if (!GrassMeshContract.IsUsable(user.vertexCount, out string error))
                {
                    report.AddError($"LOD{lod} mesh '{user.name}': {error}");
                    continue;
                }

                warnings.Clear();
                GrassMeshContract.CollectWarnings(user.subMeshCount, user.bounds, warnings);
                foreach (string w in warnings) report.AddWarning($"LOD{lod} mesh '{user.name}': {w}");
                created._meshes[lod] = user;
            }

            if (!report.IsValid)
            {
                created.Dispose(); // kısmen üretilmiş varsayılan mesh'ler sızmasın
                return false;
            }

            set = created;
            return true;
        }

        static Mesh CreateDefault(int lod)
        {
            switch (lod)
            {
                case 0: return GrassDefaultBladeMesh.Create(6);
                case 1: return GrassDefaultBladeMesh.Create(3);
                default: return GrassLodMeshFactory.CreateTriangle();
            }
        }

        /// <summary>Idempotent. Yalnızca kendi ürettiği mesh'leri yok eder.</summary>
        public void Dispose()
        {
            for (int i = 0; i < _meshes.Length; i++)
            {
                if (_owned[i] && _meshes[i] != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(_meshes[i]);
                    else UnityEngine.Object.DestroyImmediate(_meshes[i]);
                }
                _meshes[i] = null;
                _owned[i] = false;
            }
        }
    }
}
