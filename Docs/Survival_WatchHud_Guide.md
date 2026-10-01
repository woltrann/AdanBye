# Saat (WatchPanel) Göstergeleri: StatBarView

Açlık, susuzluk ve stamina göstergeleri TEK bileşenle sürülür: `StatBarView` (Assets/Scripts/UI-UX). Eski `FilledChanger` (açlık/susuzluk) ve `StaminaHudView` (stamina) yerini bu bileşene bıraktı; `StaminaHudView` silindi, `FilledChanger` sahne/prefab'lar taşındıktan sonra silinecek.

## 0. WatchPanel artık prefab
`Assets/Prefabs/Hud/WatchPanel.prefab`. Şu an yalnızca `TerrainTest12` prefab instance'ı kullanıyor; `BacisMechanics`, `CampSite`, `TerrainTest`, `Tutorial` sahnelerinde hâlâ ayrı (prefab olmayan) kopyalar var (her birinde 2 FilledChanger).
- Prefab instance'ı olan sahnelerde değişiklik **prefab'tan** yapılır; sahnedeki instance üzerinde bileşen eklemek/silmek override olur ve prefab güncellemesiyle çakışır. Araç prefab instance'larını bilerek atlar.
- Raw kopyası olan sahneler için en temizi: eski kopyayı silip prefab'ı sürüklemek; ya da aşağıdaki aracı o sahnede çalıştırmak.

## 1. Açlık/Susuzluk dönüşümü (araçla)
Menü: `Tools/AdanBye/HUD/`
1. Dönüştürülecek sahneyi aç (ve/veya `WatchPanel.prefab`'ı çift tıklayıp Prefab Mode'da aç).
2. **`FilledChanger -> StatBarView Önizleme (değiştirmez)`**: Console'a hangi nesnelerin değişeceğini, hangilerinin atlanacağını yazar; hiçbir şeyi değiştirmez.
3. **`FilledChanger -> StatBarView (açık sahne + açık prefab)`**: `isHunger` -> `Kind`, `fillImage` -> `fill`, low/mid/high renkleri kopyalanır, `FilledChanger` silinir. Sadece FilledChanger'a dokunur. Tekrar çalıştırmak güvenlidir (zaten dönüşmüş nesne yeniden dönüşmez; yarım kalmış FilledChanger varsa temizler). Undo (Ctrl+Z) çalışır.
4. Araç **kaydetmez**: sahneyi/prefab'ı sen Ctrl+S ile kaydet. Beğenmezsen kaydetmeden kapat ya da Ctrl+Z.
5. Console'daki "ATLANDI (prefab instance)" satırları için kaynak prefab'ı Prefab Mode'da açıp aracı orada çalıştır.

Karakter asset'i artık barlara sürüklenmez: `PlayerManager.Instance.mainCharacter` kullanılır. Farklı asset gerekirse `Character Override` alanına ata.

## 2. Alan -> Image eşlemesi
| StatBarView alanı | Anlamı |
|---|---|
| `Kind` | Hunger / Thirst / Stamina |
| `Fill` | Öndeki, renk değiştiren dolu Image (Filled) |
| `Limit Fill` | (ops., yalnız Stamina) Arkadaki soluk Image: yorgunluk sınırı |
| `Low/Mid/High Color` | Renk geçişi (varsayılan kırmızı/sarı/yeşil) |
| `Alert Color` | Stamina çökünce renk |
| `Alert Feedback` | (ops.) Çökmede alfa titreşimi uygulanacak CanvasGroup |

Image'lar: Image Type = Filled, aynı Fill Method/Origin/Clockwise, aynı rect, **Raycast Target KAPALI**. `Limit Fill` hiyerarşide `Fill`'in ÜSTÜNDE (önce çizilir = arkada); rengi aynı hue, düşük alfa (0.3-0.4).

## 3. Stamina barı ekleme
**Yarım daire (önerilen):** `WatchPanel.prefab`'ı Prefab Mode'da aç, `HungerBar`'ı Duplicate et, adı `StaminaBar`. `StatBarView.Kind = Stamina`. Çocuk `fill` -> `Fill`; arkada soluk bir Image ekleyip -> `Limit Fill` (`background` dekoratif zemin ise ona dokunma, fillAmount'ı kodla sürülmeyen Image olarak kalsın). RectTransform'u küçültüp iç/dış halka olarak gözle hizala, icon'u değiştir.

**Yatay bar:** `SliderPosion` altında boş yere `StaminaBar` (boş GameObject), altına `Limit`(üstte) ve `Fill` Image'ları; ikisi de Filled / Horizontal / Origin Left (Source Image olarak beyaz kare gibi bir sprite şart). `StatBarView` ekle, `Kind = Stamina`, alanları ata.

Prefab'ı kaydet; instance kullanan sahneler otomatik güncellenir.

## 4. Test
Play: açlık/susuzluk değeri düşünce bar ve renk düşmeli; stamina koşunca azalmalı, soluk sınır kamp dinlenmesi/gaz yorgunluğunda küçülmeli, çökünce kırmızı + titreşim. Player geç doğarsa bar 120 kareye kadar bağlanmayı dener; başarısızsa Console'da tek bir uyarı çıkar (`[StatBarView:<Kind>]`).

Yeni stat eklemek: `StatKind`'a değer + `StatBarSourceFactory`'ye bir satır + bir `IStatBarSource` sınıfı; `StatBarView` değişmez.
