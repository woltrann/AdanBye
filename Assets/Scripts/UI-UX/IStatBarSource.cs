// StatBarView'in gösterdiği verinin soyutlaması: view hangi statı çizdiğini bilmez, yalnızca 0..1 oranları okur.
// Okuma her karede çağrılır; implementasyonlar alloc yapmamalı.
public interface IStatBarSource
{
    // 0..1 doluluk.
    float Ratio { get; }
    // 0..1 üst sınır (ör. stamina yorgunluk sınırı). Sınırı olmayan stat 1 döner.
    float LimitRatio { get; }
    // Uyarı durumu (stamina çökmesi gibi); renk ve titreşim geri bildirimini açar.
    bool IsAlert { get; }
}
