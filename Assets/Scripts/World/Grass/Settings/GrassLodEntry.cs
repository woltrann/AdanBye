using System;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Inspector'da düzenlenen tek LOD basamağı. Saf doğrulama <see cref="LodLevelSpec"/> + <see cref="LodDistanceTable"/>
    /// tarafında kalır; bu sınıf yalnızca serileştirilebilir veri taşır ve <see cref="ToSpec"/> ile Core'a çevrilir
    /// (Core'a Unity/ScriptableObject bağımlılığı girmesin diye).
    /// </summary>
    [Serializable]
    public sealed class GrassLodEntry
    {
        /// <summary>
        /// Bu LOD'un blade mesh'i. BOŞ = "varsayılanı kullan": çizim katmanı (GrassRenderer, WP-3b-3b) boş mesh'i kendi
        /// varsayılanıyla doldurur — LOD2 için <see cref="GrassLodMeshFactory.CreateTriangle"/>, LOD0/1 için varsayılan blade.
        /// Neden doğrulama hatası değil: kullanıcı yalnızca istediği LOD'a kendi mesh'ini verebilsin.
        /// </summary>
        [Tooltip("Boş = varsayılan (LOD2: kodla üretilen üçgen). Sözleşme: pivot kökte, +Y yukarı, 1 birim boy.")]
        public Mesh mesh;

        [Tooltip("Bu LOD'un bittiği kamera uzaklığı (m). LOD'lar artan sırada olmalı.")]
        public float maxDistance = 25f;

        [Tooltip("Bu LOD'da tutulan blade oranı (0, 1]. Uzaklaştıkça azalmalı.")]
        public float keepRatio = 1f;

        [Tooltip("Seyrelmeyi kapatmak için genişlik çarpanı üst sınırı (>= 1).")]
        public float maxWidthCompensation = 1f;

        [Tooltip("LOD sonundaki geçiş bandı (m): bitmeden bu kadar önce blade boyu azalmaya başlar.")]
        public float transitionBand = 5f;

        [Tooltip("Bu LOD'un GPU buffer kapasitesi (instance). Aşılırsa fazlası çizilmez. 32 bayt/instance.")]
        public int instanceBudget = 400000;

        public LodLevelSpec ToSpec() => new LodLevelSpec(maxDistance, keepRatio, maxWidthCompensation, transitionBand);

        /// <summary>
        /// Plan varsayılanı: mesafe/oran/telafi/bant <see cref="GrassLodDefaults"/>'tan (tek kaynak), bütçe 400k/600k/800k
        /// (toplam ~58 MB; plan bütçesi). Neden ikisi ayrı kaynak: bütçe LodLevelSpec'in parçası değil (GPU kapasitesi).
        /// </summary>
        public static GrassLodEntry[] CreateDefaults()
        {
            LodLevelSpec[] specs = GrassLodDefaults.Create();
            int[] budgets = { 400000, 600000, 800000 };
            var entries = new GrassLodEntry[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                entries[i] = new GrassLodEntry
                {
                    maxDistance = specs[i].MaxDistance,
                    keepRatio = specs[i].KeepRatio,
                    maxWidthCompensation = specs[i].MaxWidthCompensation,
                    transitionBand = specs[i].TransitionBand,
                    instanceBudget = budgets[i],
                };
            }
            return entries;
        }
    }
}
