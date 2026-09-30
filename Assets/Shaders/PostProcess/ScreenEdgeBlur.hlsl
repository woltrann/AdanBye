// Ekran kenarı bulanıklığı (Fullscreen Shader Graph Custom Function): merkez net, Mask arttıkça (kenara doğru)
// 12 örnekli disk bulanıklığı. Full Screen Pass Renderer Feature kaynak görüntüyü '_BlitTexture' olarak verir.
//
// Neden URP Sample Buffer node'u kullanılmadı: o node nokta örnekleme (Load) yapar; bulanıklık için bilinear
// filtre gerekir. Neden kendi _BlitTexture/sampler tanımı: Shader Graph Fullscreen şablonu Blit.hlsl'i dahil etmez,
// _BlitTexture'ı yalnızca Sample Buffer node'u tanımlar. Sampler adındaki 'LinearClamp', Unity'de inline sampler
// anlamına gelir (bilinear + clamp); ad başka bir sampler ile çakışmasın diye özel tutuldu.
#ifndef ADANBYE_SCREEN_EDGE_BLUR_INCLUDED
#define ADANBYE_SCREEN_EDGE_BLUR_INCLUDED

TEXTURE2D_X(_BlitTexture);
SAMPLER(sampler_EdgeBlur_LinearClamp);

#define EDGE_BLUR_TAPS 12

// Mask: 0 = net, 1 = tam bulanık. Radius: UV biriminde en büyük bulanıklık yarıçapı (yüksekliğe göre; yatay ofset
// en-boy oranıyla düzeltilir, bulanıklık daire kalır). Precision: Graph Settings'te Single olmalı (yalnızca _float var).
void EdgeBlur_float(float2 UV, float Mask, float Radius, out float3 Color)
{
    float3 center = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_EdgeBlur_LinearClamp, UV, 0).rgb;
    Color = center;

    // Merkezde (ekranın büyük kısmı) tek örnek: maliyet yalnızca kenar şeridinde ödenir.
    [branch]
    if (Mask < 0.01)
        return;

    float aspect = _ScreenParams.x / _ScreenParams.y;
    float2 scale = float2(1.0 / aspect, 1.0) * Radius * Mask;

    float3 sum = center;
    [unroll]
    for (int i = 0; i < EDGE_BLUR_TAPS; i++)
    {
        // Vogel diski: altın açı ile eşit yoğunlukta dağılım; sabit desen olduğundan titreme/gürültü yok.
        float r = sqrt((i + 0.5) / EDGE_BLUR_TAPS);
        float theta = i * 2.39996323;
        float2 offset = float2(cos(theta), sin(theta)) * r * scale;
        sum += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_EdgeBlur_LinearClamp, UV + offset, 0).rgb;
    }
    Color = sum / (EDGE_BLUR_TAPS + 1);
}

#endif
