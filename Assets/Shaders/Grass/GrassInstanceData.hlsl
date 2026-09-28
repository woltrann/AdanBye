#ifndef ADANBYE_GRASS_INSTANCE_DATA_INCLUDED
#define ADANBYE_GRASS_INSTANCE_DATA_INCLUDED

// Çim instance verisinin TEK KAYNAĞI: buffer düzeni + unpack. Hem elle yazılmış procedural shader
// (GrassInstancing.hlsl) hem Shader Graph Custom Function'ı (GrassSGInstance.hlsl) bunu include eder;
// böylece iki yolun aynı veriyi farklı çözmesi (sessiz görsel hata) mümkün olmaz.
// C# karşılığı: AdanBye.Grass.GrassInstance (alan sırası/boyutu değişirse ikisi birlikte değişir).
//
// Bu dosya bilinçli olarak pragma, buffer bildirimi ya da Unity instancing makrosu İÇERMEZ:
// yalnızca saf veri + saf fonksiyon; her ortamda (SG preview dahil) derlenir.

// 32 baytlık plan düzeni (Marshal.SizeOf == 32 testi WP-2'de).
//   position    : dünya konumu (kök noktası)
//   yawHeight   : düşük 16 bit = yaw (unorm16, 0..2pi), yüksek 16 bit = yükseklik (half, metre)
//   widthFade   : düşük 16 bit = genişlik (half, metre), yüksek 16 bit = lodFade (unorm16)
//   colorRGBA8  : tint (R | G<<8 | B<<16 | A<<24)
//   normalOct   : terrain normali (oktahedral) - spike'ta yazılmıyor, rezerve
//   hash        : instance başına rastgelelik (rüzgar fazı, renk jitter'ı)
struct GrassInstance
{
    float3 position;
    uint yawHeight;
    uint widthFade;
    uint colorRGBA8;
    uint normalOct;
    uint hash;
};

struct GrassInstanceData
{
    float3 position;
    float yaw;
    float height;
    float width;
    float lodFade;
    half4 color;
    uint hash;
};

GrassInstanceData Grass_Unpack(GrassInstance i)
{
    GrassInstanceData d;
    d.position = i.position;
    d.yaw = (i.yawHeight & 0xFFFFu) * (6.28318530718 / 65535.0);
    d.height = f16tof32(i.yawHeight >> 16);
    d.width = f16tof32(i.widthFade & 0xFFFFu);
    d.lodFade = (i.widthFade >> 16) * (1.0 / 65535.0);
    d.color = half4((i.colorRGBA8 & 0xFFu) / 255.0,
                    ((i.colorRGBA8 >> 8) & 0xFFu) / 255.0,
                    ((i.colorRGBA8 >> 16) & 0xFFu) / 255.0,
                    ((i.colorRGBA8 >> 24) & 0xFFu) / 255.0);
    d.hash = i.hash;
    return d;
}

// Grass_Unpack'in tersi. C# karşılığı: AdanBye.Grass.GrassInstance.Pack — işlem sırası bilerek aynı tutuldu
// (yaw sarma, unorm16 yuvarlama, half sınırı, NaN -> 0); GrassGenerateParityTests iki tarafı bit düzeyinde karşılaştırır.
// 'color' 0..1 aralığındadır, 8 bite yuvarlanır. Bozuk (NaN/Inf/negatif) skalerler GPU'ya rastgele bit örüntüsü
// olarak gitmesin diye C# ile aynı şekilde temizlenir.
GrassInstance Grass_Pack(float3 position, float yaw, float height, float width, float lodFade,
                         float4 color, uint normalOct, uint hash)
{
    const float twoPi = 6.28318530718;

    // abs(x) < 3e38: NaN ve +-Inf için false (isnan/isinf derleyici tarafından katlanabilir; karşılaştırma katlanmaz).
    yaw = (abs(yaw) < 3.0e38) ? yaw - floor(yaw / twoPi) * twoPi : 0.0;
    uint yaw16 = min((uint)(yaw / twoPi * 65535.0 + 0.5), 65535u);

    height = (height > 0.0) ? min(height, 65504.0) : 0.0; // 65504 = half'in en büyük sonlu değeri
    width = (width > 0.0) ? min(width, 65504.0) : 0.0;

    float fade = (lodFade > 0.0) ? min(lodFade, 1.0) : 0.0;
    uint fade16 = (uint)(fade * 65535.0 + 0.5);

    uint4 c = (uint4)(saturate(color) * 255.0 + 0.5);

    GrassInstance o;
    o.position = position;
    o.yawHeight = yaw16 | (f32tof16(height) << 16);
    o.widthFade = f32tof16(width) | (fade16 << 16);
    o.colorRGBA8 = c.x | (c.y << 8) | (c.z << 16) | (c.w << 24);
    o.normalOct = normalOct;
    o.hash = hash;
    return o;
}

#endif // ADANBYE_GRASS_INSTANCE_DATA_INCLUDED
