Shader "BehaviorSim/BoidGPU"
{
    Properties
    {
        _Color ("Color", Color) = (0.35, 0.90, 0.55, 1)
        _Scale ("Scale", Float) = 0.35
    }

    SubShader
    {
        // DisableBatching keeps per-draw state intact.
        // No RenderPipeline tag — works in both Built-in and URP.
        Tags { "RenderType" = "Opaque" "DisableBatching" = "True" }
        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   4.5   // needed for StructuredBuffer in vertex stage

            #include "UnityCG.cginc"

            struct BoidData { float2 pos; float2 vel; };

            // Set via Shader.SetGlobalBuffer each frame — avoids MaterialPropertyBlock
            // StructuredBuffer binding issues in Unity 6 + URP
            StructuredBuffer<BoidData> _BoidBuffer;

            fixed4 _Color;
            float  _Scale;

            struct v2f { float4 pos : SV_POSITION; };

            v2f Vert(float3 vertex : POSITION, uint instanceID : SV_InstanceID)
            {
                BoidData b = _BoidBuffer[instanceID];

                // Rotate +Y tip toward velocity heading
                float  h = atan2(b.vel.y, b.vel.x) - UNITY_HALF_PI;
                float  c = cos(h), s = sin(h);
                float2 lp = vertex.xy * _Scale;
                float2 wp = float2(c * lp.x - s * lp.y,
                                   s * lp.x + c * lp.y) + b.pos;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 0, 1));
                return o;
            }

            fixed4 Frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
}
