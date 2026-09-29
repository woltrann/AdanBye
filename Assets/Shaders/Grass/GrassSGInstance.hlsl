// Neden target 4.5: SG şablonu Forward/DepthOnly/DepthNormals pass'lerini `#pragma target 2.0` ile derler;
// vertex aşamasında StructuredBuffer okumak SM 4.5 ister. Shader hata vermeden pass çizilmeyebilir (kullanıcı
// GrassBladeLit'te bunu yaşadı: shader hatasız ama çim yok). Bu satırın SG'ye ulaşması için Custom Function
// node'unda 'Use Pragmas' AÇIK olmalı (#include_with_pragmas). DENEYSEL: etkisi kullanıcı testiyle doğrulanacak.
#pragma target 4.5

#ifndef ADANBYE_GRASS_SG_INSTANCE_INCLUDED
#define ADANBYE_GRASS_SG_INSTANCE_INCLUDED

// Shader Graph Custom Function (File modu) - çim instance verisini vertex aşamasında okur.
//
// Bağlama (Custom Function node, Inspector):
//   Type   : File
//   Source : bu dosya
//   Name   : GrassInstance          (SG sonuna _float ekler -> GrassInstance_float; Graph Precision = Single olmalı)
//   Inputs : InstanceID (Float)     <- 'Instance ID' node'unun Out'u
//   Outputs: Position (Vector3), Yaw (Float), Height (Float), Width (Float), Color (Vector3), Hash (Float)
//
// Neden InstanceID dışarıdan parametre: SG'nin 'Instance ID' node'u vertex aşamasında SV_InstanceID'yi verir
// (instancing kapalı varyantta doğrudan input.instanceID, açık varyantta unity_InstanceID). Bu dosya hiçbir
// Unity instancing makrosuna (unity_InstanceID vb.) bağlı olmasın diye kimliği dışarıdan alır.
//
// Sözleşme: indirect args'taki instanceCount buffer uzunluğunu aşmaz (C# tarafı garanti eder). Yine de aralık
// dışı bir StructuredBuffer okuması D3D11'de tanımlı davranıştır ve 0 döner (çökme/çöp yok), bu yüzden burada
// ayrıca sınır kontrolü yapılmaz.
//
// Pragma notu: düz #include ile gelen pragma'lar işlenmez (WP-1b'de ölçüldü), bu yüzden Custom Function node'unda
// 'Use Pragmas' AÇIK olmalı (SG `#include_with_pragmas` üretir); yalnızca `procedural:` pragması bu yolda hata verir,
// bu dosyada o pragma yok. Aşağıdaki `#pragma target 4.5` deneyseldir (dosyanın en üstünde).

#include "Assets/Shaders/Grass/GrassInstanceData.hlsl"

// SG ana preview'ı (SHADERGRAPH_PREVIEW) buffer bağlayamaz; bildirim yalnızca gerçek shader'da olmalı.
#ifndef SHADERGRAPH_PREVIEW
StructuredBuffer<GrassInstance> _GrassVisibleInstances;
#endif

void GrassInstance_float(float InstanceID,
                         out float3 Position, out float Yaw, out float Height, out float Width,
                         out float3 Color, out float Hash)
{
#ifdef SHADERGRAPH_PREVIEW
    // Preview'da güvenli sabit: birim boyutlu, dönmemiş, beyaz blade.
    Position = float3(0.0, 0.0, 0.0);
    Yaw = 0.0;
    Height = 1.0;
    Width = 1.0;
    Color = float3(1.0, 1.0, 1.0);
    Hash = 0.0;
#else
    // +0.5: node uint'i float'a çevirir; yuvarlama hatasıyla bir eksik indeks okumayı önler
    // (float, 2^24'e kadar tamsayıyı birebir taşır; visible bütçesi bunun çok altında).
    GrassInstanceData d = Grass_Unpack(_GrassVisibleInstances[(uint)(InstanceID + 0.5)]);
    Position = d.position;
    Yaw = d.yaw;
    Height = d.height;
    Width = d.width;
    Color = d.color.rgb;
    Hash = (d.hash & 0xFFFFFFu) * (1.0 / 16777215.0); // 0..1, rüzgar fazı/renk jitter'ı için
#endif
}

// Terrain layer'ından gelen çim rengi (WP-4). Ayrı fonksiyon: GrassInstance_float imzası değişmesin, mevcut SG bağlantıları kırılmasın.
//
// Bağlama (ikinci Custom Function node, aynı ayarlar: Type File, Source bu dosya, Use Pragmas AÇIK):
//   Name   : GrassInstanceTint      (-> GrassInstanceTint_float)
//   Inputs : InstanceID (Float)     <- 'Instance ID' node'unun Out'u
//   Outputs: Tip (Vector3), Root (Vector3)   -- ikisi de LINEAR uzayda, doğrudan BaseColor'a verilebilir
//
// Neden burada sRGB->linear: layer tint'leri Inspector'da gamma olarak yazılır ve compute'a ham gider; proje Linear
// renk uzayındadır. Kök rengi (uç * koyulaştırma oranı) gamma'da hesaplanır, ÇÜNKÜ oran (lum(kök)/lum(uç)) gamma
// değerlerinden türetildi; linear'da çarparsak kök gereğinden parlak çıkar.
float3 Grass_SrgbToLinear(float3 c)
{
    // step + lerp: vektör koşullu ?: yerine her derleyicide aynı davranan bileşen bazlı seçim.
    float3 low = c * (1.0 / 12.92);
    float3 high = pow((c + 0.055) * (1.0 / 1.055), 2.4);
    return lerp(high, low, step(c, 0.04045));
}

void GrassInstanceTint_float(float InstanceID, out float3 Tip, out float3 Root)
{
#ifdef SHADERGRAPH_PREVIEW
    Tip = float3(1.0, 1.0, 1.0);
    Root = float3(0.5, 0.5, 0.5);
#else
    GrassInstanceData d = Grass_Unpack(_GrassVisibleInstances[(uint)(InstanceID + 0.5)]);
    float3 tipGamma = d.color.rgb;
    Tip = Grass_SrgbToLinear(tipGamma);
    Root = Grass_SrgbToLinear(tipGamma * d.color.a);
#endif
}

#endif // ADANBYE_GRASS_SG_INSTANCE_INCLUDED
