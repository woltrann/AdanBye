using System;
using System.Text;
using UnityEngine;

namespace AdanBye.Grass.Editor
{
    /// <summary>Tek bir hata metriği için max/ortalama toplayıcı.</summary>
    public struct ErrorStat
    {
        public double Max;
        public double Sum;
        public int Count;
        public int MaxIndex;

        public double Mean => Count > 0 ? Sum / Count : 0;

        public void Add(double error, int index)
        {
            if (error > Max) { Max = error; MaxIndex = index; }
            Sum += error;
            Count++;
        }
    }

    /// <summary>Doğrulama sonucu. Saf veri; biçimlendirme <see cref="ToString"/> içinde.</summary>
    public sealed class TerrainSamplingReport
    {
        public const double HeightToleranceMeters = 0.01;   // < 1 cm
        public const double SplatToleranceUnorm = 1.0 / 255.0;

        public int PointCount;
        public int Seed;
        public string HeightmapDescription;
        public string AlphamapDescription;
        public bool HeightmapTextureWasNull;

        public ErrorStat HeightBilinear;
        public ErrorStat HeightDiagonalA;
        public ErrorStat HeightDiagonalB;
        public ErrorStat SplatBilinear;   // 1/255 biriminde DEĞİL, ham [0..1] fark
        public ErrorStat SplatNearest;

        public Vector2 WorstHeightPoint;  // dünya XZ (en iyi varyant için)
        public string BestHeightVariant;
        public double BestHeightMax;

        public bool HeightPass => BestHeightMax < HeightToleranceMeters;
        public bool SplatPass => SplatBilinear.Max < SplatToleranceUnorm + 1e-6 && SplatNearest.Max < SplatToleranceUnorm + 1e-6;

        public override string ToString()
        {
            var sb = new StringBuilder(512);
            sb.AppendLine("=== Grass / Validate Terrain Sampling ===");
            sb.AppendLine($"Nokta: {PointCount}  seed: {Seed}");
            sb.AppendLine($"heightmapTexture: {HeightmapDescription}");
            sb.AppendLine($"alphamapTexture[0]: {AlphamapDescription}");
            sb.AppendLine("Yükseklik hatası (m)  [max / ortalama]");
            AppendStat(sb, "  bilinear      ", HeightBilinear, 1.0);
            AppendStat(sb, "  üçgen köşegen A (00-11)", HeightDiagonalA, 1.0);
            AppendStat(sb, "  üçgen köşegen B (10-01)", HeightDiagonalB, 1.0);
            sb.AppendLine($"  -> En iyi varyant: {BestHeightVariant}  max={BestHeightMax * 100.0:F4} cm  (kabul < 1 cm): {(HeightPass ? "GEÇTİ" : "KALDI")}");
            sb.AppendLine("Splat hatası (1/255 birimi)  [max / ortalama]");
            AppendStat(sb, "  bilinear (Unity kuralı)", SplatBilinear, 255.0);
            AppendStat(sb, "  en yakın texel", SplatNearest, 255.0);
            sb.AppendLine($"  -> kabul <= 1 (1/255): {(SplatPass ? "GEÇTİ" : "KALDI")}");
            return sb.ToString();
        }

        static void AppendStat(StringBuilder sb, string label, ErrorStat s, double scale)
        {
            sb.AppendLine($"{label}: max={s.Max * scale:F5}  mean={s.Mean * scale:F5}  (n={s.Count})");
        }
    }

    /// <summary>
    /// Compute shader'ın heightmapTexture / alphamapTextures'tan okuduğunu CPU referansıyla karşılaştırır.
    /// Neden sınıf (static değil): compute shader dışarıdan verilir (DIP) — menü, test veya ileride başka
    /// bir kaynak (BakedCopy) aynı doğrulayıcıyı farklı shader/terrain ile yeniden kullanabilsin.
    /// </summary>
    public sealed class TerrainSamplingValidator
    {
        const int KernelThreads = 64;
        const int ResultsPerPoint = 4; // GrassTerrainSamplingTest.compute ile sözleşme

        readonly ComputeShader _shader;

        public TerrainSamplingValidator(ComputeShader shader)
        {
            _shader = shader != null ? shader : throw new ArgumentNullException(nameof(shader));
        }

        public TerrainSamplingReport Validate(Terrain terrain, int pointCount, int seed)
        {
            if (!GrassTerrainSamplingInfo.TryCreate(terrain, out GrassTerrainSamplingInfo info, out string error))
                throw new InvalidOperationException(error);

            TerrainData data = terrain.terrainData;
            var report = new TerrainSamplingReport { PointCount = pointCount, Seed = seed };

            // Editor'da (Play dışı) dolu olup olmadığı özellikle test edilen kısım: null veya
            // oluşturulmamış RT burada yakalanır.
            RenderTexture heightmap = data.heightmapTexture;
            if (heightmap == null)
            {
                report.HeightmapTextureWasNull = true;
                throw new InvalidOperationException("terrainData.heightmapTexture NULL (Editor, Play dışı). BakedCopy gerekir.");
            }
            report.HeightmapDescription = $"{heightmap.width}x{heightmap.height} {heightmap.graphicsFormat} IsCreated={heightmap.IsCreated()}";
            if (!heightmap.IsCreated())
                throw new InvalidOperationException("heightmapTexture var ama IsCreated()==false. " + report.HeightmapDescription);

            if (data.alphamapTextureCount < 1)
                throw new InvalidOperationException("alphamapTextureCount == 0.");
            Texture2D alphamap = data.GetAlphamapTexture(0);
            if (alphamap == null)
                throw new InvalidOperationException("GetAlphamapTexture(0) NULL.");
            report.AlphamapDescription = $"{alphamap.width}x{alphamap.height} {alphamap.graphicsFormat}";

            Vector2[] points = GenerateRandomPoints(info, pointCount, seed);
            Vector4[] gpu = RunCompute(info, heightmap, alphamap, points);

            // CPU referansı: alphamap'i tek seferde (512*512*layers) al; noktada yeniden çağırmak yavaş ve gereksiz.
            float[,,] cpuAlpha = data.GetAlphamaps(0, 0, data.alphamapResolution, data.alphamapResolution);
            int channels = Mathf.Min(data.alphamapLayers, 4);

            Compare(report, info, terrain, cpuAlpha, channels, points, gpu);
            return report;
        }

        static Vector2[] GenerateRandomPoints(GrassTerrainSamplingInfo info, int count, int seed)
        {
            // Sabit seed: iki çalıştırma karşılaştırılabilsin, hata bulunursa aynı noktalar tekrarlanabilsin.
            var rng = new System.Random(seed);
            var points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                points[i] = new Vector2(
                    info.Origin.x + (float)rng.NextDouble() * info.Size.x,
                    info.Origin.z + (float)rng.NextDouble() * info.Size.z);
            }
            return points;
        }

        Vector4[] RunCompute(GrassTerrainSamplingInfo info, RenderTexture heightmap, Texture2D alphamap, Vector2[] points)
        {
            int kernel = _shader.FindKernel("CSSample");
            ComputeBuffer pointBuffer = null;
            ComputeBuffer resultBuffer = null;
            try
            {
                pointBuffer = new ComputeBuffer(points.Length, sizeof(float) * 2);
                pointBuffer.SetData(points);
                resultBuffer = new ComputeBuffer(points.Length * ResultsPerPoint, sizeof(float) * 4);

                info.ApplyTo(_shader);
                _shader.SetInt("_PointCount", points.Length);
                _shader.SetBuffer(kernel, "_Points", pointBuffer);
                _shader.SetBuffer(kernel, "_Results", resultBuffer);
                _shader.SetTexture(kernel, "_Heightmap", heightmap);
                _shader.SetTexture(kernel, "_Alphamap0", alphamap);
                _shader.Dispatch(kernel, Mathf.CeilToInt(points.Length / (float)KernelThreads), 1, 1);

                // Editor aracı: senkron readback (GPU stall) kabul edilebilir; runtime kodunda kullanılmaz.
                var results = new Vector4[points.Length * ResultsPerPoint];
                resultBuffer.GetData(results);
                return results;
            }
            finally
            {
                pointBuffer?.Release();
                resultBuffer?.Release();
            }
        }

        static void Compare(TerrainSamplingReport report, GrassTerrainSamplingInfo info, Terrain terrain,
                            float[,,] cpuAlpha, int channels, Vector2[] points, Vector4[] gpu)
        {
            int res = info.AlphamapResolution;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p = points[i];

                // SampleHeight terrain uzayına göre yükseklik döndürür; GPU dünya Y'si (origin.y dahil) ürettiği
                // için origin.y eklenir. (Bu sahnede origin.y = 0, dolayısıyla bu ayrım burada ölçülemez.)
                double cpuHeight = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + info.Origin.y;
                Vector4 h = gpu[i * ResultsPerPoint + 0];
                report.HeightBilinear.Add(Math.Abs(h.x - cpuHeight), i);
                report.HeightDiagonalA.Add(Math.Abs(h.y - cpuHeight), i);
                report.HeightDiagonalB.Add(Math.Abs(h.z - cpuHeight), i);

                // Alphamap: Unity kuralı texelCoord = uv * (res - 1) (bkz. GrassTerrainSampling.hlsl başlığı).
                double cx = (p.x - info.Origin.x) / info.Size.x * (res - 1);
                double cz = (p.y - info.Origin.z) / info.Size.z * (res - 1);
                Vector4 gpuBilinear = gpu[i * ResultsPerPoint + 1];
                Vector4 gpuNearest = gpu[i * ResultsPerPoint + 2];

                double worstBilinear = 0, worstNearest = 0;
                for (int c = 0; c < channels; c++)
                {
                    double cpuBilinear = BilinearAlpha(cpuAlpha, cx, cz, c, res);
                    double cpuNearest = cpuAlpha[ClampIndex((int)Math.Floor(cz + 0.5), res),
                                                 ClampIndex((int)Math.Floor(cx + 0.5), res), c];
                    worstBilinear = Math.Max(worstBilinear, Math.Abs(Channel(gpuBilinear, c) - cpuBilinear));
                    worstNearest = Math.Max(worstNearest, Math.Abs(Channel(gpuNearest, c) - cpuNearest));
                }
                report.SplatBilinear.Add(worstBilinear, i);
                report.SplatNearest.Add(worstNearest, i);
            }

            // Kabul kararı, GPU'nun mesh yüzeyiyle en iyi örtüşen varyantı üzerinden verilir.
            ErrorStat best = report.HeightBilinear; string name = "bilinear";
            if (report.HeightDiagonalA.Max < best.Max) { best = report.HeightDiagonalA; name = "üçgen köşegen A (00-11)"; }
            if (report.HeightDiagonalB.Max < best.Max) { best = report.HeightDiagonalB; name = "üçgen köşegen B (10-01)"; }
            report.BestHeightVariant = name;
            report.BestHeightMax = best.Max;
            report.WorstHeightPoint = points[best.MaxIndex];
        }

        static double BilinearAlpha(float[,,] a, double cx, double cz, int c, int res)
        {
            cx = Math.Min(Math.Max(cx, 0), res - 1);
            cz = Math.Min(Math.Max(cz, 0), res - 1);
            int x0 = (int)Math.Floor(cx), z0 = (int)Math.Floor(cz);
            int x1 = Math.Min(x0 + 1, res - 1), z1 = Math.Min(z0 + 1, res - 1);
            double fx = cx - x0, fz = cz - z0;
            // GetAlphamaps dizisi [y(z), x, layer] sırasındadır.
            double s00 = a[z0, x0, c], s10 = a[z0, x1, c], s01 = a[z1, x0, c], s11 = a[z1, x1, c];
            return (s00 * (1 - fx) + s10 * fx) * (1 - fz) + (s01 * (1 - fx) + s11 * fx) * fz;
        }

        static int ClampIndex(int i, int res) => i < 0 ? 0 : (i >= res ? res - 1 : i);

        static float Channel(Vector4 v, int c) => c == 0 ? v.x : c == 1 ? v.y : c == 2 ? v.z : v.w;
    }
}
