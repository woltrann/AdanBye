#ifndef ADANBYE_GRASS_INSTANCING_INCLUDED
#define ADANBYE_GRASS_INSTANCING_INCLUDED

// Çim procedural instancing sözleşmesi (WP-1b taslağı).
//
// Kullanım (elle yazılmış HLSL shader, HER pass'in HLSLPROGRAM bloğunda):
//   #pragma multi_compile_instancing
//   #pragma instancing_options procedural:GrassInstancingSetup
//   ... Core.hlsl include'undan SONRA:
//   #include "Assets/Shaders/Grass/GrassInstancing.hlsl"
//
// Neden pragma bu dosyada değil (WP-1b'de ölçüldü, Unity 6000.0.60f1 / URP 17.0.4 / D3D11):
//  * Düz #include ile alınan dosyadaki pragma: "unknown pragma ignored" uyarısı, procedural varyant oluşmaz.
//  * #include_with_pragmas ile alınan dosyadaki `instancing_options procedural:`: derleme hatası
//    "UNITY_INSTANCING_PROCEDURAL_FUNC must be defined" -> çalışmıyor.
//  * Pragma'nın doğrudan HLSLPROGRAM içinde olması çalışıyor.
//
// Neden matris override: Graphics.RenderMeshIndirect'te obje başına matris yok; GPU'daki instance
// verisinden unity_ObjectToWorld / unity_WorldToObject'i kendimiz kurarız. URP'nin TransformObjectToWorld,
// TransformObjectToWorldNormal ve UNITY_MATRIX_M makroları bu iki değişkeni okur, yani ForwardLit /
// DepthOnly / DepthNormals'ın hazır URP include'ları da değişmeden çalışır.
//
// ÖLÇÜLMÜŞ DAVRANIŞ (D3D11, RenderMeshIndirect): unity_InstanceID = SV_InstanceID; indirect args'taki
// startInstance ELEMANA EKLENMEZ. Yani tek buffer'da birden çok komut (LOD x submesh) ofsetle bölünemez;
// her çizim komutu kendi buffer'ını (ya da MPB'den ofset uniform'unu) kullanmalı.
// Shader Graph: SG'nin Custom Function dosyası düz #include ile alındığı için pragma buradan enjekte
// EDİLEMEZ (yukarıdaki ölçüm HLSL yolunda; SG üretimi kodda ayrıca doğrulanmadı).
//
// Referans (MIT): ColinLeung-NiloCat/UnityURP-MobileDrawMeshInstancedIndirectExample (aynı kalıp).

// Buffer düzeni + Grass_Unpack ortak dosyada (Shader Graph yolu da aynısını kullanır).
#include "Assets/Shaders/Grass/GrassInstanceData.hlsl"

// Yalnızca procedural varyantta var; diğer varyantlarda (instancing kapalı) buffer bağlanmaz.
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<GrassInstance> _GrassVisibleInstances;
#endif

// Instancing kapalı varyantta (ör. materyal GPU instancing'i kapalıysa) güvenli varsayılan döner;
// shader derlenir ama çim çizilmez/yanlış çizilir, sessizce çöp buffer okumaz.
GrassInstanceData Grass_GetInstance(uint instanceIndex)
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    return Grass_Unpack(_GrassVisibleInstances[instanceIndex]);
#else
    GrassInstanceData d = (GrassInstanceData)0;
    d.height = 1.0;
    d.width = 1.0;
    d.lodFade = 1.0;
    d.color = half4(1, 1, 1, 1);
    return d;
#endif
}

// UNITY_INSTANCING_PROCEDURAL_FUNC olarak pragma'da adı verilen fonksiyon; UNITY_SETUP_INSTANCE_ID
// içinde, unity_InstanceID atandıktan HEMEN SONRA çağrılır.
void GrassInstancingSetup()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    GrassInstanceData d = Grass_GetInstance(unity_InstanceID);

    float s, c;
    sincos(d.yaw, s, c);
    float w = max(d.width, 1e-5);
    float h = max(d.height, 1e-5);

    // M = T * Ry(yaw) * S(width, height, width). Blade mesh'i pivot kökte, +Y yukarı, birim boyutlu.
    unity_ObjectToWorld = float4x4(
         c * w, 0.0,      s * w, d.position.x,
         0.0,   h,        0.0,   d.position.y,
        -s * w, 0.0,      c * w, d.position.z,
         0.0,   0.0,      0.0,   1.0);

    // Ters matris analitik (S^-1 * R^T * T^-1); genel matris tersi her vertex'te pahalı olurdu.
    unity_WorldToObject = float4x4(
         c / w, 0.0,     -s / w, -(c * d.position.x - s * d.position.z) / w,
         0.0,   1.0 / h,  0.0,   -d.position.y / h,
         s / w, 0.0,      c / w, -(s * d.position.x + c * d.position.z) / w,
         0.0,   0.0,      0.0,    1.0);
#endif
}

// Vertex shader'ın renk/varyasyon için çağırdığı yardımcı (UNITY_SETUP_INSTANCE_ID sonrası).
GrassInstanceData Grass_GetCurrentInstance()
{
    // unity_InstanceID yalnızca instancing varyantlarında tanımlı; instancing'siz varyant da DERLENMELİ
    // (multi_compile_instancing "hiçbiri" varyantını da üretir).
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    return Grass_GetInstance(unity_InstanceID);
#else
    return Grass_GetInstance(0);
#endif
}

#endif // ADANBYE_GRASS_INSTANCING_INCLUDED
