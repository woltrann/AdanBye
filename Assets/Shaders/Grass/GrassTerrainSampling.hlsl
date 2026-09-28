#ifndef ADANBYE_GRASS_TERRAIN_SAMPLING_INCLUDED
#define ADANBYE_GRASS_TERRAIN_SAMPLING_INCLUDED

// Terrain heightmap / alphamap örnekleme yardımcıları (compute tarafı).
//
// Neden Load + elle enterpolasyon (SAMPLE_TEXTURE2D değil): Unity'nin kendi Terrain shader'ı
// (URP TerrainLitInput.hlsl: TerrainInstancing) heightmap'i tamsayı texel koordinatıyla Load eder;
// yani texel i, terrain'in i * heightmapScale.x konumundaki KÖŞE noktasıdır (vertex-hizalı, yarım texel
// kayması YOK). Bilinear sampler + UV kullanmak burada yarım texel hata üretmenin en kolay yoludur;
// Load ile hizalamayı biz belirleriz.
//
// Alphamap için Unity: splatUV = (uv * (res - 1) + 0.5) / res  (TerrainLitPasses.hlsl). Bu, uv=0 -> ilk
// texel MERKEZİ, uv=1 -> son texel MERKEZİ demektir; heightmap ile aynı vertex-hizalı kural.
// Bu yüzden ikisi de "texel koordinatı = uv * (res - 1)" ile çalışır.
//
// Referans: com.unity.render-pipelines.universal@17.0.4/Shaders/Terrain/TerrainLitInput.hlsl,
//           TerrainLitPasses.hlsl.

// Unity heightmap'i 16 bit'in yalnızca yarısını (0..32766) kullanır; C++ tarafındaki kMaxHeight budur.
// (URP: _TerrainHeightmapScale.y = hmScale.y / kMaxHeight). Bkz. TerrainLitInput.hlsl.
static const float GRASS_HEIGHTMAP_MAX = 32766.0 / 65535.0;

// Terrain hücresini iki üçgene bölen köşegen. Terrain mesh'inin gerçek köşegeni doğrulanmalı;
// bilinear ise mesh yüzeyinden sapabilir. Doğrulama aracı üçünü de ölçer.
#define GRASS_TERRAIN_DIAG_BILINEAR 0
#define GRASS_TERRAIN_DIAG_A        1   // 00-11 köşegeni
#define GRASS_TERRAIN_DIAG_B        2   // 10-01 köşegeni

// Dünya XZ -> heightmap/alphamap texel koordinatı (kesirli). resolution = eksen başına texel sayısı.
float2 Grass_WorldToTexelCoord(float2 worldXZ, float2 originXZ, float2 sizeXZ, float2 resolution)
{
    return (worldXZ - originXZ) / sizeXZ * (resolution - 1.0);
}

// Heightmap'ten ham (0..1 UNorm) yükseklik. Sınır dışı koordinatlar kenara sıkıştırılır (out-of-range Load
// sıfır döndürüp çim yüksekliğini çukura düşürmesin diye).
float Grass_LoadHeight01(Texture2D<float> heightmap, int2 texel, int2 maxTexel)
{
    return heightmap.Load(int3(clamp(texel, int2(0, 0), maxTexel), 0));
}

// Yüksekliği dünya Y'sine çevirir: terrain origin.y + h01 * size.y / kMaxHeight.
float Grass_HeightToWorldY(float h01, float originY, float sizeY)
{
    return originY + h01 * sizeY / GRASS_HEIGHTMAP_MAX;
}

// diagonal: GRASS_TERRAIN_DIAG_*. Dünya Y'sini döndürür.
float Grass_SampleTerrainHeightWS(Texture2D<float> heightmap, float2 texelCoord, int2 maxTexel,
                                  float originY, float sizeY, uint diagonal)
{
    float2 c = clamp(texelCoord, float2(0.0, 0.0), (float2)maxTexel);
    int2 i0 = min((int2)floor(c), maxTexel - 1); // son hücrede taşmayı engelle
    float2 f = c - i0;

    // x: texel X (dünya X), y: texel Y (dünya Z)
    float h00 = Grass_LoadHeight01(heightmap, i0,               maxTexel);
    float h10 = Grass_LoadHeight01(heightmap, i0 + int2(1, 0),  maxTexel);
    float h01 = Grass_LoadHeight01(heightmap, i0 + int2(0, 1),  maxTexel);
    float h11 = Grass_LoadHeight01(heightmap, i0 + int2(1, 1),  maxTexel);

    float h;
    if (diagonal == GRASS_TERRAIN_DIAG_A)
    {
        // 00-11 köşegeni: f.x > f.y ise (00,10,11) üçgeni, değilse (00,01,11).
        h = (f.x > f.y)
            ? h00 + f.x * (h10 - h00) + f.y * (h11 - h10)
            : h00 + f.y * (h01 - h00) + f.x * (h11 - h01);
    }
    else if (diagonal == GRASS_TERRAIN_DIAG_B)
    {
        // 10-01 köşegeni: f.x + f.y <= 1 ise (00,10,01), değilse (11,01,10).
        h = (f.x + f.y <= 1.0)
            ? h00 + f.x * (h10 - h00) + f.y * (h01 - h00)
            : h11 + (1.0 - f.x) * (h01 - h11) + (1.0 - f.y) * (h10 - h11);
    }
    else
    {
        h = lerp(lerp(h00, h10, f.x), lerp(h01, h11, f.x), f.y);
    }
    return Grass_HeightToWorldY(h, originY, sizeY);
}

// Alphamap: en yakın texel (birebir okuma doğrulaması için) ve bilinear (Unity shader'ı ile aynı kural).
float4 Grass_LoadSplat(Texture2D<float4> alphamap, int2 texel, int2 maxTexel)
{
    return alphamap.Load(int3(clamp(texel, int2(0, 0), maxTexel), 0));
}

float4 Grass_SampleSplatNearest(Texture2D<float4> alphamap, float2 texelCoord, int2 maxTexel)
{
    return Grass_LoadSplat(alphamap, (int2)floor(texelCoord + 0.5), maxTexel);
}

float4 Grass_SampleSplatBilinear(Texture2D<float4> alphamap, float2 texelCoord, int2 maxTexel)
{
    float2 c = clamp(texelCoord, float2(0.0, 0.0), (float2)maxTexel);
    int2 i0 = (int2)floor(c);
    float2 f = c - i0;
    float4 s00 = Grass_LoadSplat(alphamap, i0,              maxTexel);
    float4 s10 = Grass_LoadSplat(alphamap, i0 + int2(1, 0), maxTexel);
    float4 s01 = Grass_LoadSplat(alphamap, i0 + int2(0, 1), maxTexel);
    float4 s11 = Grass_LoadSplat(alphamap, i0 + int2(1, 1), maxTexel);
    return lerp(lerp(s00, s10, f.x), lerp(s01, s11, f.x), f.y);
}

#endif // ADANBYE_GRASS_TERRAIN_SAMPLING_INCLUDED
