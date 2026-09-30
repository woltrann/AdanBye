#ifndef ADANBYE_GRASS_SG_INTERACTION_INCLUDED
#define ADANBYE_GRASS_SG_INTERACTION_INCLUDED

// Shader Graph Custom Function (File modu) - çimin etkileşimcilerden (oyuncu, hayvan vb.) ezilmesi.
//
// Bağlama (Custom Function node, Inspector):
//   Type   : File
//   Source : bu dosya
//   Name   : GrassInteraction       (-> GrassInteraction_float; Graph Precision = Single olmalı)
//   Inputs : RootWS (Vector3)       <- GrassInstance node'unun 'Position' çıkışı (blade kökü, dünya uzayı)
//   Outputs: PushWS (Vector3), Amount (Float)
//
// Global'ler C# tarafından (GrassInteractionContract adlarıyla) yazılır; SG blackboard'una EKLENMEZ.
// Hiç set edilmediyse count = 0 olur ve sonuç sıfırdır.
//
// Sözleşme: dizi boyutu GrassInteractionContract.MaxInteractors ile aynı olmalı (parity testi kontrol eder).
#define GRASS_MAX_INTERACTORS 16

#ifndef SHADERGRAPH_PREVIEW
float _GrassInteractorCount;
float4 _GrassInteractorPosRadius[GRASS_MAX_INTERACTORS]; // xyz dünya konumu, w yarıçap (m)
float4 _GrassInteractorParams[GRASS_MAX_INTERACTORS];    // x güç (0..1), y dikey menzil (m)
#endif

void GrassInteraction_float(float3 RootWS, out float3 PushWS, out float Amount)
{
#ifdef SHADERGRAPH_PREVIEW
    PushWS = float3(0.0, 0.0, 0.0);
    Amount = 0.0;
#else
    float2 push = float2(0.0, 0.0);
    float amount = 0.0;

    // Sayaç float gelir; negatif/NaN durumunda döngüye girmemesi için saturate benzeri alt sınır.
    int count = (int)min(max(_GrassInteractorCount, 0.0), (float)GRASS_MAX_INTERACTORS);

    [loop]
    for (int i = 0; i < count; i++)
    {
        float4 pr = _GrassInteractorPosRadius[i];
        float4 prm = _GrassInteractorParams[i];

        float3 d = RootWS - pr.xyz;
        if (abs(d.y) > prm.y) continue;

        float dist = length(d.xz);
        // Neden max: yarıçap 0 ise sıfıra bölme olmasın.
        float t = saturate(1.0 - dist / max(pr.w, 1e-4));
        float w = t * t * prm.x;

        // Neden eşik: dist ~ 0 iken normalize NaN üretir; tam üstündeki blade'e yön verilmez.
        float2 dir = dist > 1e-4 ? d.xz / dist : float2(0.0, 0.0);

        push += dir * w;
        amount = max(amount, w);
    }

    // Neden kırpma: birden çok etkileşimcinin itmesi toplanır; üst sınır 1 (yön korunur).
    float len = length(push);
    if (len > 1.0) push /= len;

    PushWS = float3(push.x, 0.0, push.y);
    Amount = amount;
#endif
}

#endif // ADANBYE_GRASS_SG_INTERACTION_INCLUDED
