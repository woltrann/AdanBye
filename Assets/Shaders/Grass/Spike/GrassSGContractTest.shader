// WP-3a SPIKE shader'ı (kalıcı değil). Shader Graph sözleşmesini EL YAZIMIYLA taklit eder:
//  * `multi_compile_instancing` VAR, `procedural:` pragması YOK (SG'de de yok).
//  * Instance indeksi SV_InstanceID'den gelir (SG'nin 'Instance ID' node'u: instancing kapalıyken input.instanceID).
//  * Buffer okuma GrassSGInstance.hlsl'deki GrassInstance_float Custom Function'ı ile.
//  * Pozisyon dünya uzayında üretilir; SG'nin vertex çıkışı gibi önce object'e (Transform node), sonra şablonun
//    TransformObjectToWorld'ü ile geri dünyaya çevrilir. Object matrisi identity iken bu no-op'tur; instancing AÇIKsa
//    unity_ObjectToWorldArray[unity_InstanceID] (RenderMeshIndirect'te doldurulmaz) okunur ve bozulma görülür.
// ShadowCaster pass'i BİLEREK yok.
Shader "AdanBye/Spike/GrassSGContractTest"
{
    Properties
    {
        [HideInInspector] _GrassContractVersion("", Float) = 1
        [HideInInspector] _DebugTint("", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        // Her pass aynı vertex mantığını kullanır (SG'de de tüm pass'ler aynı vertex graph'ını paylaşır).
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/Shaders/Grass/GrassSGInstance.hlsl"

        // WP-3b-2: LOD başına debug rengi (MaterialPropertyBlock ile). Varsayılan beyaz => önceki davranış değişmez.
        float4 _DebugTint;

        struct GrassAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
            // SG şablonu gibi her varyantta açıkça SV_InstanceID (UNITY_VERTEX_INPUT_INSTANCE_ID makrosu bazı
            // varyantlarda tanımsız kalıp "invalid subscript 'instanceID'" veriyordu; makro yerine açık alan).
            uint instanceID : SV_InstanceID;
        };

        // Vertex graph'ının çıktısı: dünya uzayı pozisyon/normal + instance rengi.
        void GrassVertexGraph(GrassAttributes input, out float3 positionWS, out float3 normalWS, out float3 tint)
        {
            float3 rootPos; float yaw, height, width, hash;
            GrassInstance_float((float)input.instanceID, rootPos, yaw, height, width, tint, hash);

            float s, c;
            sincos(yaw, s, c);
            // WP-1c: mesh local * (width,height,width) -> Y'de yaw -> + kök (matris ile birebir aynı).
            float3 p = input.positionOS.xyz * float3(width, height, width);
            positionWS = float3(c * p.x + s * p.z, p.y, -s * p.x + c * p.z) + rootPos;
            float3 n = input.normalOS;
            normalWS = float3(c * n.x + s * n.z, n.y, -s * n.x + c * n.z);
        }

        // SG şablonunun vertex çıkışını taklit eder: Transform(World->Object) sonra TransformObjectToWorld.
        float4 GrassToClip(float3 positionWS)
        {
            float3 positionOS = TransformWorldToObject(positionWS);
            return TransformWorldToHClip(TransformObjectToWorld(positionOS));
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_instancing

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float2 uv         : TEXCOORD1;
                float3 tint       : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(GrassAttributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float3 positionWS;
                GrassVertexGraph(input, positionWS, o.normalWS, o.tint);
                o.positionCS = GrassToClip(positionWS);
                o.uv = input.uv;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half shade = lerp(0.55h, 1.0h, saturate(input.uv.y));
                return half4(input.tint * shade * _DebugTint.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(GrassAttributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float3 positionWS, tint;
                GrassVertexGraph(input, positionWS, o.normalWS, tint);
                o.positionCS = GrassToClip(positionWS);
                return o;
            }

            void Frag(Varyings input
                , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
#endif
            )
            {
#if defined(_GBUFFER_NORMALS_OCT)
                float3 normalWS = normalize(input.normalWS);
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
                outNormalWS = half4(PackFloat2To888(remapped), 0.0);
#else
                outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
#endif
#ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = float4(EncodeMeshRenderingLayer(GetMeshRenderingLayer()), 0, 0, 0);
#endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
