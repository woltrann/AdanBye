# WP-6 / WP-8 / WP-9 Sahne Kurulum Rehberi

Kod hazır; sahne/prefab bağlantısını sen yapacaksın (kod .unity/.prefab dosyalarına dokunmadı).

## 1. Player Variant (Prefabs/Player/Player Variant.prefab)

Player kök objesi = Rigidbody + PlayerManager + PlayerMotor'ın olduğu obje.

1. **Eski bileşen:** `PlayerPoisonStatus` script dosyası silindi; prefabda "Missing Script" görünecek. Kök objede o Missing Script'i Remove Component ile kaldır.
2. Kök objeye **AtmosphereSensor** ekle.
   - `Base Outdoor Density` = 0.2, `Max Density` = 1.
3. Kök objeye **PlayerToxinExposure** ekle (`Main Character` boş kalabilir; PlayerManager'dan alınır).
4. Kök objede `PlayerManager`, `DeviceChargeController`, `PlayerStamina` zaten olmalı.
5. Player'da gövde collider'ı **tek** olsun (Capsule). Birden fazla collider aynı hacme girerse biri çıkınca hacim erken düşer.

## 2. Trigger'ın oyuncuya ulaşması

- Unity, trigger olayını hem collider'ın objesine hem o collider'ın bağlı olduğu **Rigidbody objesine** yollar. Sensör Rigidbody ile aynı objede (kök) olduğu için çocuk collider'dan da olay alır.
- Hacim objesinde: Collider + **Is Trigger = açık**. Hacimde Rigidbody gerekmez (Player'ın Rigidbody'si yeter).
- Layer: Edit > Project Settings > Physics > Layer Collision Matrix'te Player'ın layer'ı ile hacmin layer'ı çarpışabilir olmalı (tetikler için de geçerli). Önerilen: hacimler için `Atmosphere` layer'ı aç, yalnızca Player ile işaretle.
- Player Rigidbody'si kinematic olsa da çalışır; trigger collider'ın kendisi static olabilir.

## 3. IndoorZone / GasVolume

- Boş obje > BoxCollider (Is Trigger) > **IndoorZone** ekle: içindeyken iç mekan (dış taban 0.2 yok sayılır).
- Boş obje > Collider (Is Trigger) > **GasVolume** ekle, `Density` ayarla: içindeyken yoğunluğa eklenir (iç mekanda da).
- Örnekler: bina içi = IndoorZone (hacim binayı kaplasın, kapıyı biraz dışarı taşırsın). Zehirli oda = IndoorZone + GasVolume(0.8). Açık alanda gaz bulutu = sadece GasVolume(0.5) (0.2 + 0.5 = 0.7).
- Hacmi kapatırsan/yok edersen sensör kendini temizler.

## 4. HUD (StaminaHudView)

Bilek HUD hiyerarşisi bilinmediği için sen kur:
1. HUD Canvas altında `StaminaBar` objesi oluştur, üstüne **StaminaHudView** ekle.
2. Altına `Fill` (UI Image): Image Type = **Filled**, Fill Method = **Horizontal**, Fill Origin = **Left**. Sprite yoksa beyaz kare bile olur. Alanı `Stamina Fill`'e ata.
3. `Fill` ile aynı rect'i kaplayan bir parent (örn. `StaminaBar`) içine `CeilingMarker` (ince dikey Image) koy: Anchor Min/Max Y değerleri 0..1 (dikey gerilen), pivot X=0.5. Kod X anchor'ını kaydırır; `Ceiling Marker`'a ata. (Marker'ın parent'ı bar rect'inin kendisi olmalı.)
4. İstersen çökme titreşimi için bar'a **CanvasGroup** ekleyip `Collapse Feedback`'e ata (boş olabilir).
5. Alanlardan biri boş olsa kod patlamaz; yalnızca o kısım çalışmaz (uyarı loglanır).

## 5. WP-9 (gaz yorgunluğu), varsayılan KAPALI

Player'daki `PlayerStamina` > `Gas Affects Fatigue` işaretle. `Poison Ratio Threshold` (0.5) ve `Poisoned Fatigue Multiplier` (1.5) ayarlanır. Çarpan yalnızca tavan (Ceiling) kaybını hızlandırır.

## 6. Play modu kontrol listesi

- [ ] Dış mekanda filtre yavaş boşalıyor (baz 0.2: ~3500 sn, çok yavaş; test için `Base Outdoor Density` geçici yükselt).
- [ ] IndoorZone içinde filtre boşalmıyor.
- [ ] GasVolume içinde belirgin hızlı boşalıyor.
- [ ] Filtre 0 iken `currentPoison` artıyor.
- [ ] Kampta filtre doldurma sonrası zehir artışı duruyor.
- [ ] HUD: doluluk koşuyla azalıyor, tavan işareti yavaş sola kayıyor, çöküşte alfa titriyor.
- [ ] Console'da "bulunamadı" uyarısı yok (varsa referans eksik).
- [ ] WP-9 açıkken zehir >= %50'de tavan daha hızlı düşüyor.

## 7. Bilinen eksikler

- Baz 0.2 yoğunlukta filtre ~3500 sn dayanır; dengeyi `Base Outdoor Density` veya `ToxinExposureConfig.FilterDrainPerSecondAtFullDensity` (PlayerToxinExposure) ile ayarla.
- Zehirin kendisinin sonucu (can kaybı vb.) bu paketin kapsamı dışı.
- `Condition.LowStamina` diyaloğunu kodla tetikleyen yer yok (bkz. rapor); gerekirse ayrı iş.
