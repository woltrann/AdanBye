using System.Collections.Generic;

namespace AdanBye.Grass
{
    /// <summary>
    /// Kullanıcı verisini (ScriptableObject/Inspector) doğrulayan çekirdek sınıfların ortak sonuç tipi.
    /// Neden istisna değil rapor: Inspector'dan girilen veri aynı anda birden fazla yanlış içerebilir; hepsini tek
    /// seferde göstermek, ilk hatada patlamaktan çok daha iyi bir yazma deneyimi. Ayrıca hatayı nasıl ele alacağı
    /// (LogError + bileşeni kapat, Inspector HelpBox, test) çağıranın kararıdır; çekirdek bu kararı çalmaz.
    /// Hata varsa <see cref="IsValid"/> false olur; uyarılar üretimi engellemez.
    /// </summary>
    public sealed class ValidationReport
    {
        readonly List<string> _errors = new List<string>();
        readonly List<string> _warnings = new List<string>();

        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool IsValid => _errors.Count == 0;

        public void AddError(string message) => _errors.Add(message);
        public void AddWarning(string message) => _warnings.Add(message);

        /// <summary>
        /// Başka bir raporun hata/uyarılarını ekler. Neden var: ayar dönüşümü birkaç bağımsız doğrulayıcıyı
        /// (LOD, layer, üretim) çalıştırır; biri başarısız olunca diğerleri atlanmasın, hepsi tek raporda görünsün.
        /// </summary>
        public void Merge(ValidationReport other)
        {
            if (other == null) return;
            _errors.AddRange(other._errors);
            _warnings.AddRange(other._warnings);
        }

        /// <summary>Hata ve uyarıları tek metinde birleştirir (log/HelpBox için).</summary>
        public override string ToString()
        {
            var parts = new List<string>(_errors.Count + _warnings.Count);
            foreach (string e in _errors) parts.Add("HATA: " + e);
            foreach (string w in _warnings) parts.Add("UYARI: " + w);
            return string.Join("\n", parts);
        }
    }
}
