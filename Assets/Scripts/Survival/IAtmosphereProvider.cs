// Küçük, tek-amaçlı arayüz (ISP): tüketici (toksin maruziyeti) sensörün somut sınıfını değil
// sadece "şu an havada ne kadar gaz var" sorusunu bilir (DIP).
public interface IAtmosphereProvider
{
    // 0..maxDensity; 1 = tam yoğunluk (ToxinExposureConfig varsayılanları bu değere göre).
    float GasDensity { get; }
    bool IsIndoor { get; }
}
