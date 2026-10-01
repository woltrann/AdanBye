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
- [ ] Koşu staminası 0'da bayılma olmaz (sadece koşu kapanır); ana stamina 0'a inince bitkin -> bayılma; uyanınca ana stamina %25. Ana stamina %30 altına inince hız x0.85.
- [ ] Kamp itemi: stamina ve tavan 100'e döner, ardından save yazılır.
- [ ] Kamp, kaydet, oyunu kapat/aç: stamina, tavan, telefon/saat/fener şarjı ve gaz filtresi korunur.
- [ ] ESKİ save dosyasıyla yükleme: oyuncu bayılmaz, stamina 100, şarjlar dolu, Console'da "Eski save (v...)" logu.
- [ ] Zıplama kapalı kalır (jumpEnabled=false).

## Bitkinlik ve bayılma (v3: iki havuz)
İki ayrı havuz vardır; bayılma YALNIZCA ana staminaya bağlıdır.

| | Koşu staminası (kodda `Current`) | Ana stamina (kodda `Ceiling`) |
|---|---|---|
| Neyle azalır | Koşunca (`RunDrainPerSecond`) | Her zaman: dururken/yürürken `CeilingDrainIdlePerSecond`, koşarken `CeilingDrainRunPerSecond` (gaz: fatigueMultiplier ile çarpılır) |
| Neyle dolar | Durunca, `RegenDelaySeconds` sonra `RegenPerSecond` ile; ana staminayı asla aşamaz | Kendiliğinden ASLA; yalnızca kampta tam dolum (`RestAtCamp`). Uyanınca `WakeMainStaminaFraction` kadar |
| 0'a inince | Sadece koşu kapanır (yürümeye devam). Bitkinlik/bayılma/yavaşlama YOK. `RunResumeThreshold`'a çıkınca koşu açılır | Bitkinlik: `ExhaustionGraceSeconds` (3 sn) koşu yok, dolum yok, hız x`ExhaustedSpeedMultiplier` -> bayılma (yürüse bile) |
| Düşükken | - | Oran < `FatigueSlowdownThreshold` (0.30) ise hız x`FatigueSlowdownMultiplier` (0.85) |

Akış: ana stamina 0 -> bitkin (3 sn) -> bayıl (`CollapseDurationSeconds`, Collapsed olayı tek sefer) -> uyan:
ana stamina = Max x `WakeMainStaminaFraction` (0.25), koşu staminası = ana staminanın tamamı. Ceza yok, taban yok.
Çökme/bitkin süresince ana stamina zaten 0'dır (düşecek bir şey yok); uyku sırasında yorulma modellenmez.
Kamp ve save yükleme bitkinliği sıfırlar. Save formatı değişmedi (currentStamina = koşu, staminaCeiling = ana).

**Silinen alanlar:** `CeilingFloor`, `CollapseCeilingPenalty`, `PostCollapseStaminaFraction` artık yok (prefab'daki yetim
YAML zararsız). **Yeni alan:** `WakeMainStaminaFraction` prefab'da yoksa kod varsayılanı 0.25 uygulanır.
Prefab'daki serileştirilmiş config kod varsayılanını EZER; aşağıdaki değerleri PlayerMain.prefab > PlayerStamina > Config'ten kontrol et.

### Inspector'da ayarlayacağın değerler
| Alan | Etkisi |
|---|---|
| RunDrainPerSecond (12) | Dolu barla koşu süresi = 100 / değer (12 -> 8.3 sn, 8 -> 12.5 sn) |
| CeilingDrainIdlePerSecond (0.05) | Ana stamina dururken/yürürken kaybı |
| CeilingDrainRunPerSecond (0.3) | Ana stamina koşarken kaybı |
| ExhaustionGraceSeconds (3) | 0'dan bayılmaya süre; 0 = anında bayıl |
| WakeMainStaminaFraction (0.25) | Uyanınca ana stamina (zorunlu uyku az da olsa doldurur) |

### Gerçek-zaman tablosu (Max 100)
| Senaryo | Ana stamina bitme süresi |
|---|---|
| Yalnızca dururken/yürürken (0.05/sn) | 2000 sn = **33 dk** |
| %30 süre koşarak (0.7x0.05 + 0.3x0.3 = 0.125/sn) | 800 sn = **13 dk** |
| Sürekli koşu (0.3/sn) | 333 sn = **5.5 dk** |

Koşu staminası dolu barla: RunDrainPerSecond 12 -> 8.3 sn, 8 -> 12.5 sn (koşu ana stamina yönünden de 0.3/sn yer).

Gün süresi (TerrainTest12 dayDuration 3840 sn = **64 dk**) ile karşılaştır: varsayılanlarla yalnızca durarak bile 33 dk'da
bayılırsın; yani gün sonunda kampa ulaşılıyorsa varsayılanlar ÇOK sert. "Gün sonunda kamp" varsayımıyla öneri (karar senin):
`CeilingDrainIdlePerSecond` 0.012, `CeilingDrainRunPerSecond` 0.04 -> yalnızca durarak 139 dk, %30 koşuyla
(0.7x0.012 + 0.3x0.04 = 0.0204/sn) 82 dk, sürekli koşuda 42 dk; yani normal oyunda gün bitmeden bayılma olmaz,
çok koşan oyuncu gün sonuna yetişemeyebilir. Gaz (fatigueMultiplier) bunu kısaltacağından biraz pay bırakıldı.

### HUD notu
Stamina barı (`StatKind.Stamina`) = koşu staminası, `limitFill` soluk bölgesi = ana stamina. Ana staminayı ayrı bar
olarak göstermek istersen yeni bir StatBarView'de `StatKind = Energy` seç (dolu oran = ana stamina/Max).
Uyarı rengi: bitkin veya bayılmışken yanar.

Kod kancası: `PlayerStamina.Exhausted` olayı yok (model olayı var, abonesi yok); `IStaminaReadout.IsExhausted` okunabilir.
Animator parametresi eklenmedi (kapsam dışı).

## Bilinen eksikler
- Çökme animasyon klibi yok (geçici state).
- Yorgunluk çarpanı sabit 1 (WP-9 bağlayacak).
- Aktivite motordan bir kare gecikmeli türetilir.
- MainCharacter.CaptureState kullanılmıyor, kapsam dışı; SaveManager eski elle yazım kodu duruyor.
- Kamp itemID 50 magic number olarak kaldı.
