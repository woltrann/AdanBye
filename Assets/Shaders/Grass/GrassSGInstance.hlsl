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

#endif // ADANBYE_GRASS_SG_INSTANCE_INCLUDED
