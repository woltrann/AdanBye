# WP-4 / WP-5 Kurulum Rehberi (Stamina + Kamp + Save)

Kod hazır; sahne/prefab/animator bağlantıları elle yapılır.

## 1. Player Variant prefab'ına PlayerStamina ekle
1. Player Variant prefab'ını aç (PlayerManager, PlayerMotor, DeviceChargeController'ın olduğu kök obje).
2. `Add Component > PlayerStamina`.
3. `Config` alanı `[Serializable]` sınıf olduğu için Inspector'da açılır ve varsayılanlar zaten dolu gelir
   (Max 100, RunDrain 12/sn, Regen 8/sn, FatigueSlowdownThreshold 0.30, Multiplier 0.85, CollapseDuration 4 sn...).
   Elle atama gerekmez. Not: bileşeni eklemeden önce prefab'da eski bir serileştirme varsa değerler 0 görünürse
   sağ üst menüden `Reset` yap.
4. Başka referans yok; PlayerMotor/PlayerJumpController/PlayerAnimatorSync aynı objeden kendisi bulur.

## 2. Animator Controller: IsCollapsed
1. Animator penceresinde `Parameters > + > Bool`, adı tam olarak `IsCollapsed`.
2. Geçici çökme durumu: klip yok, bu yüzden boş bir state oluştur (`Collapsed`, Motion boş) ya da mevcut
   Idle klibini ata (karakter kilitli ve duruyor görünür).
3. `Any State -> Collapsed`: koşul `IsCollapsed == true`, `Can Transition To Self` kapalı, Has Exit Time kapalı.
4. `Collapsed -> Idle` (ya da önceki ana state): koşul `IsCollapsed == false`, Has Exit Time kapalı.
5. Parametre yoksa oyun açılışında Console'da tek seferlik uyarı çıkar; çökme yine de çalışır (hareket kilitlenir), sadece görsel olmaz.

## 3. Yüzmede çökme notu
PlayerSwimController input okumaz; hareket kilidi PlayerMotor'da uygulanır, bu yüzden suda çökme yatay yüzmeyi de
durdurur. Yüzme yükselme/batma etkisi (ElevationDelta) kilitten etkilenmez. Suda çökme tasarımı kullanıcı kararına bağlı.

## 4. Kamp ve Save
- Kamp itemi (itemID 50) kullanılınca önce `RestAtCamp` (stamina + tavan dolar), sonra kayıt alınır.
- SaveManager/PlayerManager sahnede zaten varsa ek bağlantı gerekmez.
- Yeni kayıtlar `saveVersion = 2` yazar. Eski kayıtta (v<2) stamina/şarj/filtre geri yüklenmez, varsayılanlar kalır.

## 5. Play Modu kontrol listesi
- [ ] Console'da PlayerStamina/Animator uyarısı yok (ya da yalnızca Animator'ı henüz hazırlamadıysan beklenen uyarı).
- [ ] Shift ile sürekli koşu: yaklaşık 8 sn sonra (100 / 12) çökme.
- [ ] Çökmede ~4 sn hareket ve dönüş kilitli, sonra kontrol geri gelir; koşu hemen açılmaz (eşik 20).
- [ ] Çökme sonrası stamina tavanı 10 düşer; tavan %30 altına inince yürüme/koşu hızı x0.85.
- [ ] Kamp itemi: stamina ve tavan 100'e döner, ardından save yazılır.
- [ ] Kamp, kaydet, oyunu kapat/aç: stamina, tavan, telefon/saat/fener şarjı ve gaz filtresi korunur.
- [ ] ESKİ save dosyasıyla yükleme: oyuncu bayılmaz, stamina 100, şarjlar dolu, Console'da "Eski save (v...)" logu.
- [ ] Zıplama kapalı kalır (jumpEnabled=false).

## Bilinen eksikler
- Çökme animasyon klibi yok (geçici state).
- Yorgunluk çarpanı sabit 1 (WP-9 bağlayacak).
- Aktivite motordan bir kare gecikmeli türetilir.
- MainCharacter.CaptureState kullanılmıyor, kapsam dışı; SaveManager eski elle yazım kodu duruyor.
- Kamp itemID 50 magic number olarak kaldı.
