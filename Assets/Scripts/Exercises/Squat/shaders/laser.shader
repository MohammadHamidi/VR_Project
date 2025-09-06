Shader "Custom/VR Laser Additive (BI)"
{
    Properties{
        _BaseMap("Beam Ramp (A)", 2D) = "white" {}
        _NoiseMap("Noise", 2D) = "gray" {}
        [HDR]_Color("Color", Color) = (1,0.2,0.2,1)
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.25
        _Scroll("Scroll Speed", Float) = 1.0
        _EdgeSoft("Edge Softness", Range(0,1)) = 0.5
        _Intensity("Intensity", Range(0,5)) = 1.0
    }
    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // Stereo (BI)
            #pragma multi_compile __ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include "UnityCG.cginc"

            sampler2D _BaseMap; float4 _BaseMap_ST;
            sampler2D _NoiseMap; float4 _NoiseMap_ST;
            fixed4 _Color;
            float _NoiseStrength, _Scroll, _EdgeSoft, _Intensity;

            struct appdata { float4 vertex: POSITION; float2 uv: TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f     { float4 pos: SV_POSITION; float2 uv: TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed ramp = tex2D(_BaseMap, float2(uv.x, 0.5)).a;
                float2 nuv = uv * _NoiseMap_ST.xy + _NoiseMap_ST.zw + float2(_Scroll * _Time.y, 0);
                fixed n = tex2D(_NoiseMap, nuv).r;
                ramp = saturate(ramp + (n - 0.5) * _NoiseStrength);
                fixed edge = smoothstep(0, _EdgeSoft, uv.y) * smoothstep(0, _EdgeSoft, 1 - uv.y);
                fixed3 col = _Color.rgb * ramp * edge * _Intensity;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
