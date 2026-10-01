# Survival WP-2 Kurulum Rehberi

Sağlık/şarj tick mantığı UXobjects'ten çıkarıldı. Sahne/prefab bağlantısını elle yapman gerekiyor.

## 1. Player prefab'ına bileşen ekle
`Assets/Prefabs/Player/Player Variant.prefab` (kaynak: `Player.prefab`; PlayerPoisonStatus bu varyantta, sahneler `Player Variant` kullanıyor: TerrainTest12, EnemyChase, Tutorial).
Prefab'ı aç, kök objeye (PlayerManager'ın olduğu GameObject) ekle:
1. **Player Vitals Ticker** (Add Component > `PlayerVitalsTicker`)
2. **Device Charge Controller** (`DeviceChargeController`)

`Main Character` alanları boş kalabilir: PlayerManager.mainCharacter'dan otomatik alınır. Hız alanları (Inspector, saniye/birim):
Açlık 7, Susuzluk 5; Telefon 10/10, Saat 15/15, Fener 7, Droid 15.
Prefab'ı kaydet. Başka bir sahnede kendi Player kopyası varsa (Player.prefab, TestAdan vb.) orada da aynısını yap.

## 2. UXobjects (sahnede, Canvas altı)
- `Device Charge` alanı: BOŞ bırak (PlayerManager.Instance üzerinden bulunur). Bulunamazsa konsolda uyarı çıkar; o zaman Player'daki bileşeni elle sürükle (sahne instance'ı).
- `Phone/Watch/Flash Charge Percent` ve `Gass Filter Percent` TMP referansları aynen kalır.
- Silinen alanlar (phoneCharge, watchCharge, flashCharge, gassFilter, isRecharge, droidRecharge, isFlash, isOutsideforGassFilter) Inspector'da "Missing" görünmez; Unity serileşmiş eski değerleri sessizce yok sayar. Bir sahnede yine de hata görürsen UXobjects bileşenini seç, sağ üst `⋮ > Reset` yerine alanları tek tek kontrol et ve sahneyi kaydet (eski değerler temizlenir).

## 3. Doğrulama (Play modu)
- [ ] Konsolda "DeviceChargeController/MainCharacter bulunamadı" uyarısı YOK.
- [ ] Telefon % yaklaşık 10 sn'de 1, saat 15 sn'de 1 düşüyor (UI "N%" formatı).
- [ ] Kamp şarjı (item ID 54) telefon/saat/fener'i 100 yapıyor.
- [ ] F ile fener açılınca fener % ~7 sn'de 1 düşüyor; kapalıyken sabit.
- [ ] Güneş paneli (Recharger) alındıktan sonra gündüz telefon/saat artıyor, gece düşüyor.
- [ ] Açlık 7 sn'de 1, susuzluk 5 sn'de 1 düşüyor (MainCharacter asset'inde izle).
- [ ] Droid chip (case 0) alınca droid şarjı 15 sn'de +1 artıyor.
- [ ] Toksin maskesi tüketilince gaz filtresi 100%.
- [ ] Saat metni ve kalp atışı animasyonu çalışıyor.

## 4. Gaz filtresi için dış kapı
`DeviceChargeController.DrainGasFilter(float amount)` public: WP-8 AtmosphereSensor dışarıda geçen süreye göre bunu çağıracak. Boşalınca `IsGasFilterEmpty` true olur ve PlayerPoisonStatus zehirlenmeyi uygular.

## Bilinen eksikler
- Gaz filtresi henüz boşalmıyor (eskiden de boşaltan kod yoktu; WP-8).
- Şarj/filtre/fener durumu save'e girmiyor (WP-5, ISaveable).
- İlk tick artık başlangıçta değil, bir tam aralık sonra geliyor (eski coroutine hemen düşürüyordu).
- Cihazlar sürekli (kesirli) azalır, UI yuvarlar; eskisi 10 sn'de bir basamaklı düşerdi.
- Fener bitince otomatik kapanmıyor (eski davranış korundu).
