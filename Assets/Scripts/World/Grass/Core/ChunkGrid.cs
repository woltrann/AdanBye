using Unity.Mathematics;

namespace AdanBye.Grass
{
    /// <summary>
    /// Terrain'in XZ düzlemini sabit boyutlu chunk'lara bölen ızgara: dünya konumu &lt;-&gt; chunk koordinatı,
    /// chunk sayısı ve terrain kenarında kısmi chunk sınırları. Yalnızca sayısal veri tutar (Terrain'e bağımlı
    /// değil) — böylece EditMode'da test edilir ve seçici/bounds tablosu aynı sözleşmeyi paylaşır.
    /// Chunk (cx, cz): x ekseninde cx, z ekseninde cz; düz indeks = cz * CountX + cx.
    /// </summary>
    public readonly struct ChunkGrid
    {
        // Kenar tam chunk katıysa float hatası yüzünden fazladan boş bir chunk doğmasın diye tolerans.
        const double CountEpsilon = 1e-6;

        // Sağlıksız girdide (ör. 0.001 m chunk) milyarlarca chunk'lık tablo ayırmayı önleyen üst sınır.
        public const int MaxChunkCount = 1 << 20;

        public float OriginX { get; }
        public float OriginZ { get; }
        public float SizeX { get; }
        public float SizeZ { get; }
        public float ChunkSize { get; }
        public int CountX { get; }
        public int CountZ { get; }

        public int Count => CountX * CountZ;

        ChunkGrid(float originX, float originZ, float sizeX, float sizeZ, float chunkSize, int countX, int countZ)
        {
            OriginX = originX;
            OriginZ = originZ;
            SizeX = sizeX;
            SizeZ = sizeZ;
            ChunkSize = chunkSize;
            CountX = countX;
            CountZ = countZ;
        }

        /// <summary>
        /// Izgarayı üretir; geçersiz girdide false + neden döner (istisna değil: çağıran kendi hata kanalını seçer).
        /// </summary>
        public static bool TryCreate(float originX, float originZ, float sizeX, float sizeZ, float chunkSize,
                                     out ChunkGrid grid, out string error)
        {
            grid = default;
            if (!IsFinite(originX) || !IsFinite(originZ)) { error = "Terrain origin sonlu bir sayı olmalı."; return false; }
            if (!IsFinite(sizeX) || !IsFinite(sizeZ) || sizeX <= 0f || sizeZ <= 0f)
            {
                error = "Terrain boyutu (X,Z) sonlu ve > 0 olmalı.";
                return false;
            }
            if (!IsFinite(chunkSize) || chunkSize <= 0f) { error = "Chunk boyutu sonlu ve > 0 olmalı."; return false; }

            // int'e çevirmeden ÖNCE double'da kontrol: aşırı küçük chunk int taşmasıyla sessizce küçük bir sayıya sarardı.
            double countXd = math.max(math.ceil(sizeX / (double)chunkSize - CountEpsilon), 1.0);
            double countZd = math.max(math.ceil(sizeZ / (double)chunkSize - CountEpsilon), 1.0);
            if (countXd * countZd > MaxChunkCount)
            {
                error = $"Chunk sayısı ({countXd * countZd:0}) üst sınırı ({MaxChunkCount}) aşıyor; chunk boyutunu büyüt.";
                return false;
            }

            grid = new ChunkGrid(originX, originZ, sizeX, sizeZ, chunkSize, (int)countXd, (int)countZd);
            error = null;
            return true;
        }

        public bool Contains(int cx, int cz) => (uint)cx < (uint)CountX && (uint)cz < (uint)CountZ;

        public int ToIndex(int cx, int cz) => cz * CountX + cx;

        public int2 FromIndex(int index) => new int2(index % CountX, index / CountX);

        /// <summary>
        /// Dünya XZ'sini chunk'a çevirir. Terrain dışı (veya NaN) ise false. Terrain'in tam uç kenarındaki
        /// nokta (origin+size) son chunk'a dahildir: terrain yüzeyi orada biter ama nokta geçerlidir.
        /// </summary>
        public bool TryWorldToChunk(float worldX, float worldZ, out int cx, out int cz)
        {
            cx = 0;
            cz = 0;
            float lx = worldX - OriginX;
            float lz = worldZ - OriginZ;
            // NaN karşılaştırmaları false döndüğü için negatif-mantık NaN'ı da eler.
            if (!(lx >= 0f && lx <= SizeX && lz >= 0f && lz <= SizeZ)) return false;

            cx = math.min((int)(lx / ChunkSize), CountX - 1);
            cz = math.min((int)(lz / ChunkSize), CountZ - 1);
            return true;
        }

        /// <summary>
        /// Terrain dışındaki noktaları en yakın kenar chunk'ına sıkıştırır. Kamera terrain'in dışındayken bile
        /// halka taramasının başlangıç chunk'ını bulmak için (kamera terrain'e komşu olabilir).
        /// </summary>
        public int2 WorldToChunkClamped(float worldX, float worldZ)
        {
            int cx = (int)math.floor((worldX - OriginX) / ChunkSize);
            int cz = (int)math.floor((worldZ - OriginZ) / ChunkSize);
            return new int2(math.clamp(cx, 0, CountX - 1), math.clamp(cz, 0, CountZ - 1));
        }

        /// <summary>
        /// Chunk'ın terrain ile kesişen XZ dikdörtgeni. Kenardaki chunk'lar chunkSize'tan küçük olabilir (kısmi
        /// chunk); bounds'un terrain dışına taşmaması compute'un dışarı örnekleme yapmasını ve boşuna çim
        /// aramasını önler.
        /// </summary>
        public void GetChunkRect(int cx, int cz, out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = OriginX + cx * ChunkSize;
            minZ = OriginZ + cz * ChunkSize;
            maxX = math.min(minX + ChunkSize, OriginX + SizeX);
            maxZ = math.min(minZ + ChunkSize, OriginZ + SizeZ);
        }

        /// <summary>
        /// Verilen dünya dikdörtgeniyle çakışabilecek chunk aralığı (dahil, terrain'e sıkıştırılmış).
        /// Rect tamamen terrain dışındaysa false. Halka taramasında tüm ızgarayı gezmemek için.
        /// </summary>
        public bool TryGetChunkRange(float minX, float minZ, float maxX, float maxZ, out int2 minChunk, out int2 maxChunk)
        {
            minChunk = default;
            maxChunk = default;
            if (!(maxX >= OriginX && maxZ >= OriginZ && minX <= OriginX + SizeX && minZ <= OriginZ + SizeZ)) return false;

            minChunk = WorldToChunkClamped(minX, minZ);
            maxChunk = WorldToChunkClamped(maxX, maxZ);
            return true;
        }

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
