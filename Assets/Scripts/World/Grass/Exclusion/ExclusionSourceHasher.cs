using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Bake girdilerinden deterministik parmak izi (FNV-1a 64). Neden: mask asset'inin kaynaklarına göre
    /// eskidiğini (ağaç eklendi/taşındı, göl kaydı) bake etmeden anlamak. Float'lar bit düzeyinde hash'lenir;
    /// küçük kayma da mask'ı değiştirebileceği için kasıtlı yuvarlama yok.
    /// </summary>
    public sealed class ExclusionSourceHasher
    {
        const ulong Offset = 14695981039346656037UL;
        const ulong Prime = 1099511628211UL;

        ulong _h = Offset;

        public ulong Value => _h;

        public string ToHex() => _h.ToString("x16");

        public ExclusionSourceHasher AddInt(int v)
        {
            unchecked
            {
                for (int i = 0; i < 4; i++) { _h ^= (byte)(v >> (8 * i)); _h *= Prime; }
            }
            return this;
        }

        public ExclusionSourceHasher AddFloat(float v)
        {
            // -0 ve +0 aynı geometri: tek biçime indir, gereksiz "stale" çıkmasın.
            if (v == 0f) v = 0f;
            return AddInt(BitConverter.SingleToInt32Bits(v));
        }

        public ExclusionSourceHasher AddVector2(Vector2 v) => AddFloat(v.x).AddFloat(v.y);

        public ExclusionSourceHasher AddMatrix(Matrix4x4 m)
        {
            for (int i = 0; i < 16; i++) AddFloat(m[i]);
            return this;
        }

        /// <summary>Sayı + her ağacın konumu/prototip/ölçeği (sıra duyarlı; sağlayıcı sırası deterministik).</summary>
        public ExclusionSourceHasher AddTrees(IReadOnlyList<TreeInstanceInfo> trees)
        {
            AddInt(trees.Count);
            for (int i = 0; i < trees.Count; i++)
                AddVector2(trees[i].PositionXZ).AddInt(trees[i].PrototypeIndex).AddFloat(trees[i].WidthScale);
            return this;
        }

        /// <summary>Mesh vertex + index verisi ve dünya matrisi. Okunamayan mesh yalnızca matris + sayıları katar.</summary>
        public ExclusionSourceHasher AddMesh(Mesh mesh, Matrix4x4 localToWorld)
        {
            AddMatrix(localToWorld);
            if (mesh == null) return AddInt(-1);
            AddInt(mesh.vertexCount).AddInt(mesh.subMeshCount);
            if (!mesh.isReadable) return this;

            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++) AddFloat(verts[i].x).AddFloat(verts[i].y).AddFloat(verts[i].z);
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                int[] idx = mesh.GetTriangles(s);
                AddInt(idx.Length);
                for (int i = 0; i < idx.Length; i++) AddInt(idx[i]);
            }
            return this;
        }
    }
}
