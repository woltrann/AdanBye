# Oyun Saati, Gaz Filtresi ve Bayılma Zaman Atlaması

## 1. Sahne bağlantısı: gerekmez
`DayCycle` artık `IGameClock` uygular ve `Awake`'te `DayCycle.Instance` atar; Survival kodu saati `GameClockLocator.Find()` ile
kendisi bulur. Sahnede sadece bir `DayCycle` olması yeterli (TerrainTest12 ve Tutorial'da zaten var).
Saat bulunamazsa gaz maruziyeti bekler ve Console'a tek sefer uyarı yazar.

## 2. PlayerMain.prefab'a CollapseTimeSkip ekleme
1. `Prefabs/PlayerMain.prefab` aç (Prefab Mode).
2. Kök objeye **Add Component > CollapseTimeSkip**.
3. `Min Hours = 1`, `Max Hours = 2` (varsayılan zaten bu). Kaydet.
Bileşen yoksa özellik kapalıdır (eski davranış). Aynı kökte PlayerStamina, PlayerVitalsTicker, DeviceChargeController, PlayerToxinExposure olmalı (zaten var).

## 3. Yeni config alanları (PlayerToxinExposure > Config)
| Alan | Anlamı | Varsayılan |
|---|---|---|
| Filter Drain Per Game Hour At Full Density | Yoğunluk 1'de filtrenin oyun saati başına kaybı. Süre (oyun saati) = 100 / (değer x yoğunluk) | 100 |
| Poison Per Game Hour At Full Density | Filtre boşken yoğunluk 1'de oyun saati başına zehir | 80 (eski his: 0.5/sn x 160 sn) |

Eski alanlar (`...PerSecondAtFullDensity`) kaldırıldı; prefab'daki eski 0.1428 değeri artık okunmaz (zararsız). Yeni alanlar prefab'da yoksa kod varsayılanı kullanılır.

## 4. 5 oyun saati kaç gerçek dakika (taban yoğunluk 0.2)
| dayDuration | 1 oyun saati | 5 oyun saati |
|---|---|---|
| 3840 sn (TerrainTest12) | 160 sn | 800 sn = ~13.3 dk |
| 480 sn (Tutorial) | 20 sn | 100 sn = ~1.7 dk |

Formül: `5 x dayDuration / 24` sn. Yoğunluk 1'de (gaz hacmi içinde) 1 oyun saati (3840'ta 160 sn).

## 5. Saat hızı artırılınca
`dayDuration` küçülürse (gün kısalırsa) **filtre ve zehir** oyun saatine bağlı olduğu için gerçek sürede orantılı hızlanır (dayDuration yarıya inerse filtre 13 dk yerine ~6.7 dk). Açlık/susuzluk/şarj gerçek saniye tabanlıdır (hızları değişmez), sadece bayılma atlamasında oyun saatine çevrilerek uygulanır.

## 6. Bayılınca ne olur
`Collapsed` olayı çökmenin başlangıcında tetiklenir; o anda 1-2 oyun saati rastgele atlanır: güneş/saat ilerler; açlık, susuzluk, telefon/saat/fener/droid şarjı ve gaz filtresi (zehir dahil) o süre kadar işler. Yoğunluk ve cihaz durumları (fener, güneş şarjı) atlama anındaki değerinde sabit varsayılır. Açlık/susuzluk 0'a kadar iner, altına inmez (başarısızlık kuralı henüz yok).
Console: `[CollapseTimeSkip] 1.4 oyun saati atlandı`.

## 7. Play kontrol listesi
- [ ] Koşarak staminayı bitir/bayıl: Console'da `[CollapseTimeSkip] X.X oyun saati atlandı` (1.0-2.0 arası).
- [ ] Saat (HUD/DayCycle) 1-2 saat ileri gitti.
- [ ] Açlık ~ (atlanan saat x 160 / 7), susuzluk ~ (x 160 / 5) birim düştü (3840'ta); şarjlar düştü.
- [ ] Dışarıdaysan gaz filtresi atlanan saat x 20 birim düştü (2 saat = 40).
- [ ] Dışarıda bekleyince filtre ~13 dk'da boşalır (3840'ta); boşalınca zehir artar.
- [ ] Kampta dinlen: Console'da `[PlayerStamina] Kamp dinlenmesi: stamina X->Y, tavan A->B`.
- [ ] Kaydet/yükle: saat doğru yüklenir (SaveManager artık `SetTimeOfDay01` kullanır).

## 8. Gaz hacmi oluşturma (şu an hiçbir sahnede yok)
1. Hierarchy > sağ tık > **Create Empty**, adı `GasVolume_Test`.
2. **Add Component > Box Collider** (veya Sphere); **Is Trigger** işaretli olsun; Size'ı alanı kaplayacak kadar büyüt.
3. **Add Component > GasVolume**; `Density` ayarla (0.5 varsayılan; yoğunluğa eklenir, toplam 1'e sınırlanır).
4. Player kökünde `Rigidbody` + `AtmosphereSensor` olmalı (trigger olayları için, zaten var). Hacme gir: filtre dışarıdakinden hızlı boşalır.
