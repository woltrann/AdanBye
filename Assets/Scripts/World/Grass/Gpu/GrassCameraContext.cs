using System;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Tek bir kameraya ait çim kaynakları: GPU buffer'ları, LOD başına MaterialPropertyBlock, chunk seçici ve görünüm.
    /// Neden kamera başına: compute üretimi kamera konumuna/frustum'una bağlıdır ve çizim beginCameraRendering'de
    /// ertelenerek yürür; kameralar tek buffer'ı paylaşsaydı ikinci kameranın dispatch'i, birincinin henüz yürütülmemiş
    /// çiziminin verisini ezerdi. Bedel: kamera başına ~58 MB (varsayılan LOD bütçeleri 400k+600k+800k * 32 B);
    /// güvenli varsayılan olarak Game + SceneView için kabul edildi, filtre diğer kameraları zaten eler.
    /// </summary>
    internal sealed class GrassCameraContext : IDisposable
    {
        static readonly int VisibleInstancesId = Shader.PropertyToID("_GrassVisibleInstances");

        readonly MaterialPropertyBlock[] _props;

        public GrassGpuResources Gpu { get; }
        public VisibleChunkSelector Selector { get; }
        public GrassCameraView View { get; } = new GrassCameraView();

        GrassCameraContext(GrassGpuResources gpu, VisibleChunkSelector selector, MaterialPropertyBlock[] props)
        {
            Gpu = gpu;
            Selector = selector;
            _props = props;
        }

        /// <summary>LOD'un instance buffer'ını shader'ın <c>_GrassVisibleInstances</c>'ına bağlayan blok (RenderParams.matProps).</summary>
        public MaterialPropertyBlock Props(int lod) => _props[lod];

        public static bool TryCreate(int[] lodBudgets, int maxChunks, ChunkBoundsTable table, float drawDistance,
                                     Vector3 chunkPadding, GrassLodMeshSet meshes, out GrassCameraContext context, out string error)
        {
            context = null;
            if (meshes.Count != lodBudgets.Length)
            {
                error = $"LOD mesh sayısı ({meshes.Count}) bütçe sayısına ({lodBudgets.Length}) eşit değil.";
                return false;
            }

            if (!GrassGpuResources.TryCreate(lodBudgets, maxChunks, out GrassGpuResources gpu, out error)) return false;

            if (!VisibleChunkSelector.TryCreate(table, maxChunks, chunkPadding,
                                                out VisibleChunkSelector selector, out error))
            {
                gpu.Dispose();
                return false;
            }

            var props = new MaterialPropertyBlock[lodBudgets.Length];
            for (int lod = 0; lod < props.Length; lod++)
            {
                if (!gpu.ConfigureDrawArgs(lod, meshes[lod], 0, out error))
                {
                    gpu.Dispose();
                    return false;
                }
                props[lod] = new MaterialPropertyBlock();
                props[lod].SetBuffer(VisibleInstancesId, gpu.Instances(lod));
            }

            context = new GrassCameraContext(gpu, selector, props);
            error = null;
            return true;
        }

        public void Dispose() => Gpu.Dispose(); // idempotent (GrassGpuResources.IsDisposed)
    }
}
