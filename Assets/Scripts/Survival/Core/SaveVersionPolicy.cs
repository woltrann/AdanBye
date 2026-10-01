namespace AdanBye.Survival
{
    // Save sürüm kararları. Eski (v1) dosyalarda yeni alanlar JsonUtility ile 0 okunur:
    // stamina 0 = açılır açılmaz bayılma, filtre 0 = boş. Bu yüzden eski dosyada
    // hayatta kalma alanları geri yüklenmez, varsayılanlar kalır.
    public static class SaveVersionPolicy
    {
        public const int Current = 2;
        public const int SurvivalFieldsSince = 2;

        public static bool HasSurvivalData(int saveVersion) => saveVersion >= SurvivalFieldsSince;
    }
}
