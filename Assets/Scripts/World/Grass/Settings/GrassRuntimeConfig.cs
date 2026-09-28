using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// <see cref="GrassSettings"/>'in DOĞRULANMIŞ, salt-okunur çalışma zamanı görüntüsü. Yalnızca
    /// <see cref="GrassSettings.TryBuildRuntime"/> üretir; bu yüzden elinde config olan kod bozuk veriyle
    /// karşılaşmaz (LodDistanceTable/LayerDensityMapper/GrassGenerateSettings zaten doğrulanmış tiplerdir).
    /// ScriptableObject referansı taşımaz: SO sonradan değişse de çalışan sistem tutarlı snapshot'la yaşar.
    /// Tek Unity nesnesi <see cref="LodMeshes"/> (Mesh referansları): doğrulama mantığından ayrı liste olarak tutulur.
    /// Dizi alanları ctor'da KOPYALANIR ve dışarıya salt-okunur sarmalayıcıyla verilir: aksi halde snapshot'ın
    /// "değişmez" sözü, paylaşılan bir diziyi değiştiren herhangi bir çağıranla bozulurdu.
    /// </summary>
    public sealed class GrassRuntimeConfig
    {
        readonly int[] _lodInstanceBudgets;

        public GrassGenerateSettings Generate { get; }
        public LodDistanceTable Lods { get; }
        public LayerDensityMapper Layers { get; }

        /// <summary>Chunk seçiminin kamera uzaklık sınırı (m); >= son LOD'un maxDistance'ı.</summary>
        public float DrawDistance { get; }

        /// <summary>Blade frustum culling küresine eklenen pay (m).</summary>
        public float CullPadding { get; }

        /// <summary>
        /// LOD başına buffer kapasitesi (salt-okunur görünüm; dizi olarak geri çevrilemez).
        /// GrassGpuResources.TryCreate int[] ister: <see cref="CopyLodInstanceBudgets"/> kullan.
        /// </summary>
        public IReadOnlyList<int> LodInstanceBudgets { get; }

        /// <summary>
        /// LOD başına mesh; eleman NULL olabilir = "varsayılanı kullan" (bkz. <see cref="GrassLodEntry.mesh"/>).
        /// Uzunluk = <c>Lods.Count</c>.
        /// </summary>
        public IReadOnlyList<Mesh> LodMeshes { get; }

        internal GrassRuntimeConfig(GrassGenerateSettings generate, LodDistanceTable lods, LayerDensityMapper layers,
                                    float drawDistance, float cullPadding, int[] lodInstanceBudgets, Mesh[] lodMeshes)
        {
            Generate = generate;
            Lods = lods;
            Layers = layers;
            DrawDistance = drawDistance;
            CullPadding = cullPadding;
            _lodInstanceBudgets = (int[])lodInstanceBudgets.Clone();
            LodInstanceBudgets = Array.AsReadOnly(_lodInstanceBudgets);
            LodMeshes = Array.AsReadOnly((Mesh[])lodMeshes.Clone());
        }

        /// <summary>Bütçelerin BAĞIMSIZ kopyası (GrassGpuResources.TryCreate'e verilir; değiştirmek config'i etkilemez).</summary>
        public int[] CopyLodInstanceBudgets() => (int[])_lodInstanceBudgets.Clone();
    }
}
