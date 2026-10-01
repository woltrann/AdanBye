# Stamina HUD: Saat (WatchPanel) Entegrasyonu

Not: Saat yüzü sprite'ının nerede bittiğini göremiyorum; aşağıdaki anchor değerleri sahne YAML'ından çıkarıldı, yerleşimi Scene görünümünde gözle ayarla.

## 0. Önce karar: 5 sahnede ayrı kopya
`Canvas/GamePanel/WatchPanel` prefab değil; BacisMechanics, CampSite, TerrainTest, TerrainTest12, Tutorial sahnelerinde ayrı kopyalar var.
- **Öneri:** GamePanel (veya WatchPanel) bir prefab'a çevrilsin (Project'e sürükle), diğer sahnelerde eski kopya silinip prefab konsun. Stamina bir kez kurulur.
- **Alternatif:** Aşağıdaki adımları her sahnede tekrarla.
Kararı sen ver; ikisinde de adımlar aynı.

## 1. Alan değişikliği (önemli)
`ceilingMarker` (RectTransform) kaldırıldı, yerine `ceilingFill` (Image) geldi. Eski referansı atamıştıysan Inspector'da "Missing/eski alan" görünür ya da boşalır; `ceilingFill`'i yeniden ata. Eski marker objesini sil.

## 2. Alan -> Image eşlemesi
| StaminaHudView alanı | Image |
|---|---|
| `staminaFill` | Öndeki, renk değiştiren dolu Image |
| `ceilingFill` | Arkadaki soluk Image (yorgunluk sınırı) |
| `collapseFeedback` | (ops.) Göstergeyi saran CanvasGroup |
| Low/Mid/High | Varsayılan kırmızı/sarı/yeşil; HungerBar'daki FilledChanger değerleriyle aynı yap |
| `collapsedColor` | Çökünce kırmızıya yakın renk |

İki Image da: Image Type = Filled, **aynı** Fill Method/Origin/Clockwise, **aynı rect**, Raycast Target KAPALI. Hiyerarşide `ceilingFill` `staminaFill`'in ÜSTÜNDE (önce çizilir = arkada). Ceiling rengi: aynı hue, düşük alfa (ör. 0.3-0.4).

## Seçenek A: Üçüncü yarım daire
1. `HungerBar`'ı Duplicate et, adı `StaminaBar`.
2. Üstündeki `FilledChanger` bileşenini kaldır, `StaminaHudView` ekle.
3. Çocuklar: `fill` -> `staminaFill`; `background` -> `ceilingFill` rolü için kullan (Image Type Filled, Radial180, fill origin fill ile aynı, rengi soluk) veya ayrı bir `ceiling` Image ekle ve `fill`'in üstüne sırala. `background` fillAmount'ı kodla değişeceği için dekoratif zemin gerekiyorsa ona ayrı bir Image tut.
4. Hunger/Thirst x~0.47-0.55, y~0.54-0.76'da iç içe; Stamina için bir iç/dış halka seç, RectTransform'u küçültüp/büyütüp gözle hizala. Icon'u stamina simgesiyle değiştir.

## Seçenek B: SliderPosion altında yatay ince bar
1. `SliderPosion` (y~0.22-0.28) altına boş yer bırak; WatchPanel altına `StaminaBar` (boş GameObject) ekle, anchor x 0.196-0.447, y ~0.14-0.19 (gözle ayarla).
2. Altına iki Image: `Ceiling` (üstte sıralamada ilk), `Fill` (sonra). Slider KULLANMA.
3. Her ikisi: Image Type Filled, Fill Method Horizontal, Origin Left. Sprite yoksa Source Image boş (düz renk) olur, Filled çalışması için bir sprite (ör. UISprite/beyaz kare) ata.
4. `StaminaBar`'a `StaminaHudView` ekle; Fill -> `staminaFill`, Ceiling -> `ceilingFill`.

## Test
Play: bar koşunca azalmalı, renk yeşil->sarı->kırmızı; soluk sınır kamp dinlenmesi/gaz yorgunluğunda küçülmeli; çökünce kırmızı + titreşim.
