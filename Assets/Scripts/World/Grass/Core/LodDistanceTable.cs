using System.Collections.Generic;

namespace AdanBye.Grass
{
    /// <summary>
    /// Bir LOD basamağının saf veri tanımı (mesh referansı YOK: mesh Unity nesnesidir, doğrulama mantığı
    /// ondan bağımsız test edilebilsin diye mesh'ler GrassMeshLodSet tarafında ayrı tutulur).
    /// </summary>
    public readonly struct LodLevelSpec
    {
        /// <summary>Bu LOD'un bittiği kameraya uzaklık (m). LOD'lar artan mesafede sıralı olmalı.</summary>
        public readonly float MaxDistance;

        /// <summary>Bu LOD'da tutulan instance oranı (0, 1]. LOD0 için tipik 1.</summary>
        public readonly float KeepRatio;

        /// <summary>Seyrelme telafisi için genişlik çarpanı üst sınırı (&gt;= 1).</summary>
        public readonly float MaxWidthCompensation;

        /// <summary>
        /// Bu LOD'un sonundaki geçiş bandı genişliği (m): LOD bitmeden bu kadar önce lodFade azalmaya başlar.
        /// [0, LOD'un kendi mesafe aralığı] içinde olmalı.
        /// </summary>
        public readonly float TransitionBand;

        public LodLevelSpec(float maxDistance, float keepRatio, float maxWidthCompensation, float transitionBand)
        {
            MaxDistance = maxDistance;
            KeepRatio = keepRatio;
            MaxWidthCompensation = maxWidthCompensation;
            TransitionBand = transitionBand;
        }
    }

    /// <summary>
    /// Doğrulanmış LOD listesini compute/shader'a gidecek düz dizilere çevirir. Yalnızca geçerli veriyle
    /// üretilebilir (<see cref="TryCreate"/>); böylece GPU'ya bozuk tablo (ör. keepRatio 0 -> 1/0) hiç gitmez.
    /// Hata bildirimi: istisna yerine <see cref="ValidationReport"/> — kullanıcı verisi aynı anda birçok hata
    /// içerebilir, hepsi tek seferde gösterilsin; çağıran (LogError + disable / Inspector) kanalı seçsin.
    /// </summary>
    public sealed class LodDistanceTable
    {
        /// <summary>Hücre seviyeleri 0/1/2 ve "compute x3" planıyla eşleşen üst sınır.</summary>
        public const int MaxLodCount = 3;

        /// <summary>Compute'a SetFloats ile gidecek diziler; DEĞİŞTİRME (paylaşılan, kopyasız).</summary>
        public float[] MaxDistanceSq { get; }
        public float[] KeepRatio { get; }
        public float[] WidthCompensation { get; }
        public float[] TransitionBand { get; }

        public int Count => MaxDistanceSq.Length;

        /// <summary>Çizim mesafesi = son LOD'un bitişi; bunun ötesinde çim yok (terrain rengi).</summary>
        public float DrawDistance { get; }

        LodDistanceTable(IReadOnlyList<LodLevelSpec> specs)
        {
            int n = specs.Count;
            MaxDistanceSq = new float[n];
            KeepRatio = new float[n];
            WidthCompensation = new float[n];
            TransitionBand = new float[n];
            for (int i = 0; i < n; i++)
            {
                LodLevelSpec s = specs[i];
                MaxDistanceSq[i] = s.MaxDistance * s.MaxDistance;
                KeepRatio[i] = s.KeepRatio;
                // Seyrelen çimin boşluğunu kalınlaştırarak kapatır; sınırsız bırakılsa çok seyrek LOD'da devasa
                // bıçaklar oluşurdu.
                WidthCompensation[i] = System.Math.Min(1f / s.KeepRatio, s.MaxWidthCompensation);
                TransitionBand[i] = s.TransitionBand;
            }
            DrawDistance = specs[n - 1].MaxDistance;
        }

        /// <summary>
        /// Tabloyu doğrular ve üretir. <paramref name="table"/> yalnızca <c>report.IsValid</c> iken doludur.
        /// Uyarılar üretimi engellemez; hatalar (artan keepRatio dahil) engeller.
        /// </summary>
        public static bool TryCreate(IReadOnlyList<LodLevelSpec> specs, out LodDistanceTable table, out ValidationReport report)
        {
            table = null;
            report = new ValidationReport();

            if (specs == null || specs.Count == 0)
            {
                report.AddError("LOD listesi boş: en az 1 LOD gerekli.");
                return false;
            }
            if (specs.Count > MaxLodCount)
            {
                report.AddError($"LOD sayısı {specs.Count} > desteklenen üst sınır {MaxLodCount}.");
                return false;
            }

            float previousDistance = 0f;
            float previousKeep = float.MaxValue;
            for (int i = 0; i < specs.Count; i++)
            {
                LodLevelSpec s = specs[i];
                bool distanceOk = IsFinite(s.MaxDistance) && s.MaxDistance > 0f;

                if (!distanceOk)
                    report.AddError($"LOD{i}: maxDistance sonlu ve > 0 olmalı (değer {s.MaxDistance}).");
                else if (s.MaxDistance <= previousDistance)
                    report.AddError($"LOD{i}: maxDistance ({s.MaxDistance}) önceki LOD'dan ({previousDistance}) büyük olmalı (artan sıra).");

                // keepRatio 0 kabul edilmez: 1/keepRatio sonsuz olur ve LOD hiçbir şey çizmez; kapatmak için LOD'u sil.
                if (!IsFinite(s.KeepRatio) || s.KeepRatio <= 0f || s.KeepRatio > 1f)
                    report.AddError($"LOD{i}: keepRatio (0, 1] aralığında olmalı (değer {s.KeepRatio}).");
                // Neden hata (uyarı değil): seyreltme rank eşiğiyle yapılır (rank < yoğunluk*keep), yani uzak LOD'da
                // tutulan blade yakın LOD'da da tutulur (alt küme) — pop'suz geçişin dayanağı bu. Bu garanti yalnızca
                // keepRatio azalırken geçerli; artarsa uzak LOD'da yakında olmayan blade'ler belirir ve LOD sınırında
                // görünür pop oluşur. Blend()/compute artan tabloda da "çalışır" ama pop'suzluk sözünü tutamaz.
                else if (s.KeepRatio > previousKeep)
                    report.AddError($"LOD{i}: keepRatio ({s.KeepRatio}) önceki LOD'dan ({previousKeep}) büyük olamaz; " +
                                    "keepRatio uzaklaştıkça azalmalı (aksi halde LOD geçişinde çim pop'u oluşur).");

                if (!IsFinite(s.MaxWidthCompensation) || s.MaxWidthCompensation < 1f)
                    report.AddError($"LOD{i}: maxWidthCompensation >= 1 olmalı (değer {s.MaxWidthCompensation}).");

                if (!IsFinite(s.TransitionBand) || s.TransitionBand < 0f)
                    report.AddError($"LOD{i}: transitionBand sonlu ve >= 0 olmalı (değer {s.TransitionBand}).");
                else if (distanceOk && s.MaxDistance > previousDistance && s.TransitionBand > s.MaxDistance - previousDistance)
                    report.AddError($"LOD{i}: transitionBand ({s.TransitionBand}) LOD'un kendi aralığından ({s.MaxDistance - previousDistance}) büyük olamaz.");

                if (distanceOk) previousDistance = s.MaxDistance;
                if (IsFinite(s.KeepRatio) && s.KeepRatio > 0f) previousKeep = s.KeepRatio;
            }

            if (!report.IsValid) return false;

            table = new LodDistanceTable(specs);
            return true;
        }

        /// <summary>
        /// Verilen uzaklığın (m) LOD indeksi; çizim mesafesinin ötesinde -1. Compute'taki seçimin CPU aynasıdır
        /// (tanılama ve testler için).
        /// </summary>
        public int FindLod(float distance)
        {
            if (!IsFinite(distance) || distance < 0f) return -1;
            float d2 = distance * distance;
            for (int i = 0; i < MaxDistanceSq.Length; i++)
                if (d2 < MaxDistanceSq[i]) return i;
            return -1;
        }

        /// <summary>
        /// Bir blade'in (mesafe, rank/yoğunluk oranı) için LOD kararı. GrassGenerate.compute'taki Grass_LodBlend'in
        /// CPU aynasıdır: iki taraf birlikte değişir; bu yüzden pop'suzluk özelliği (alt küme, süreklilik) burada
        /// EditMode testleriyle sabitlenir. <paramref name="rankOverDensity"/> = rank / yoğunluk (yoğunluk &gt; 0);
        /// compute'ta bölme yapılmaz, rank ile yoğunluk*keep kıyaslanır — matematiksel olarak aynı.
        /// </summary>
        public LodBlendResult Blend(float distance, float rankOverDensity)
        {
            int lod = FindLod(distance);
            if (lod < 0) return LodBlendResult.Culled;

            bool hasNext = lod + 1 < Count;
            float nextKeep = hasNext ? KeepRatio[lod + 1] : 0f;
            float comp = WidthCompensation[lod];
            float nextComp = hasNext ? WidthCompensation[lod + 1] : comp;

            float band = TransitionBand[lod];
            float fade = band > 0f
                ? System.Math.Max(0f, System.Math.Min(1f, ((float)System.Math.Sqrt(MaxDistanceSq[lod]) - distance) / band))
                : 1f;

            bool kept = rankOverDensity < KeepRatio[lod];
            bool survivesNext = rankOverDensity < nextKeep;
            float heightScale = survivesNext ? 1f : fade;
            float widthScale = survivesNext ? nextComp + (comp - nextComp) * fade : comp;
            return new LodBlendResult(lod, kept, heightScale, widthScale);
        }

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    /// <summary>
    /// <see cref="LodDistanceTable.Blend"/> sonucu. Lod = -1: çizim mesafesinin ötesi. Kept = false: bu LOD'da yok.
    /// HeightScale/WidthScale: geçiş bandında boy küçülmesi / genişlik telafisi (kept iken anlamlı).
    /// </summary>
    public readonly struct LodBlendResult
    {
        public static readonly LodBlendResult Culled = new LodBlendResult(-1, false, 0f, 0f);

        public readonly int Lod;
        public readonly bool Kept;
        public readonly float HeightScale;
        public readonly float WidthScale;

        public LodBlendResult(int lod, bool kept, float heightScale, float widthScale)
        {
            Lod = lod;
            Kept = kept;
            HeightScale = heightScale;
            WidthScale = widthScale;
        }

        /// <summary>Blade çizilir mi (LOD içinde, tutulmuş ve boyu sıfırdan büyük).</summary>
        public bool IsVisible => Lod >= 0 && Kept && HeightScale > 0f;
    }

    /// <summary>
    /// Plan varsayılanı LOD listesi (saf veri; ScriptableObject dönüşümü WP-3b-3'te).
    /// LOD0 0-25 m keep 1; LOD1 25-60 m keep 0.35 (genişlik x1.7 üst sınır); LOD2 60-120 m keep 0.1 (x3 üst sınır).
    /// Geçiş bantları LOD aralığının ~1/5..1/4'ü: pop'u gizleyecek kadar geniş, yakın LOD'u gereksiz küçültmeyecek kadar dar.
    /// </summary>
    public static class GrassLodDefaults
    {
        public static LodLevelSpec[] Create() => new[]
        {
            new LodLevelSpec(25f, 1f, 1f, 5f),
            new LodLevelSpec(60f, 0.35f, 1.7f, 10f),
            new LodLevelSpec(120f, 0.1f, 3f, 15f),
        };
    }
}
