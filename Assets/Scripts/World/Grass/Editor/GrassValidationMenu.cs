using UnityEditor;
using UnityEngine;

namespace AdanBye.Grass.Editor
{
    /// <summary>
    /// Menü girişi. Neden mantık taşımaz: doğrulama <see cref="TerrainSamplingValidator"/>'da; burası yalnızca
    /// terrain + compute asset'ini bulup sonucu konsola yazar (tek sorumluluk, aynı doğrulayıcı testten de çağrılabilir).
    /// </summary>
    public static class GrassValidationMenu
    {
        const string ComputePath = "Assets/Shaders/Grass/GrassTerrainSamplingTest.compute";
        const int PointCount = 1000;
        const int Seed = 12345;

        [MenuItem("Grass/Validate Terrain Sampling")]
        public static void ValidateTerrainSampling()
        {
            Run();
        }

        /// <summary>Aracı çalıştırır ve raporu döndürür (menüden ve otomasyondan çağrılabilir).</summary>
        public static string Run(int pointCount = PointCount, int seed = Seed)
        {
            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
            if (compute == null)
            {
                string msg = $"Compute shader bulunamadı: {ComputePath}";
                Debug.LogError(msg);
                return msg;
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                const string msg = "Aktif terrain yok; önce bir terrain içeren sahne açın.";
                Debug.LogError(msg);
                return msg;
            }

            try
            {
                string text = new TerrainSamplingValidator(compute).Validate(terrain, pointCount, seed).ToString();
                Debug.Log(text);
                return text;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Terrain örnekleme doğrulaması başarısız: {e.Message}");
                return "HATA: " + e.Message;
            }
        }
    }
}
