using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Çim (ve ileride ağaç) shader'larının paylaştığı global rüzgar yönünü ayarlar.
    /// </summary>
    /// <remarks>
    /// Neden global: Assets/Shaders/Grass/Graph/GrassBladeLit.shadergraph'taki '_WindDirection'
    /// property'si Shader Graph'ta Global kapsamda (materyal başına değil, tüm çim/ağaç shader'ları
    /// arasında ortak olsun diye). Global kapsamdaki bir property Material Inspector'da GÖRÜNMEZ ve
    /// materyal dosyasına hiç yazılmaz; değeri yalnızca Shader.SetGlobalVector ile runtime'da verilir.
    /// Bu bileşen sahnede yoksa değer sıfır kalır: shader'daki Normalize(0,0) NaN üretir, bu NaN çim
    /// vertex pozisyonuna yayılır ve yapraklar tamamen kaybolur (yaşanan sorun buydu).
    /// </remarks>
    [ExecuteAlways]
    public sealed class GrassWindController : MonoBehaviour
    {
        static readonly int WindDirectionId = Shader.PropertyToID("_WindDirection");
        static readonly int WindStrengthId = Shader.PropertyToID("_WindStrength");
        static readonly int WindSpeedId = Shader.PropertyToID("_WindSpeed");

        [Tooltip("Rüzgar yönü (XZ düzleminde). SIFIR VERME: shader bunu normalize eder, (0,0) NaN üretir " +
                 "ve çim tamamen kaybolur.")]
        [SerializeField] Vector2 windDirection = new Vector2(1f, 0.3f);

        [Tooltip("Rüzgarın şiddeti")]
        [SerializeField] float windStrength = 1f;

        [Tooltip("Rüzgarın hızı")]
        [SerializeField] float windSpeed = 1f;



        void OnEnable() => Apply();

        // Inspector'dan canlı ayarlarken (Editor, Play dışı) global değeri hemen günceller.
        void OnValidate() => Apply();

        void Apply()
        {
            // Kullanıcı yanlışlıkla (0,0) girerse sessizce NaN'a düşmek yerine güvenli bir varsayılana
            // düşüyoruz; asıl neden burada ve sınıf yorumunda açık.
            Vector2 safe = windDirection.sqrMagnitude > 0.0001f ? windDirection : new Vector2(1f, 0f);

            Shader.SetGlobalFloat(WindStrengthId, windStrength);
            Shader.SetGlobalFloat(WindSpeedId, windSpeed);
            Shader.SetGlobalVector(WindDirectionId, safe);
        }
    }
}
