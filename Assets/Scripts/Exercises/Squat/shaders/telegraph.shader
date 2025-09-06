Shader "Custom/VR Telegraph Alpha (BI)"
{
    Properties{
        _BaseMap("Dash/Ramp (A)", 2D) = "white" {}
        [HDR]_Color("Color", Color) = (1,0,0,0.6)
        _BlinkSpeed("Blink Speed", Range(0,10)) = 2.0
        _EdgeSoft("Edge Softness", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile __ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include "UnityCG.cginc"

            sampler2D _BaseMap; float4 _BaseMap_ST;
            fixed4 _Color;
            float _BlinkSpeed, _EdgeSoft;

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

                // dashed/gradient در آلفای تکسچر
                fixed a = tex2D(_BaseMap, float2(uv.x, 0.5)).a;

                // چشمک نرم (برای VR سرعت زیاد نذار)
                fixed blink = 0.7 + 0.3 * sin(_Time.y * _BlinkSpeed);
                a *= blink;

                fixed edge = smoothstep(0, _EdgeSoft, uv.y) * smoothstep(0, _EdgeSoft, 1 - uv.y);
                a *= edge;

                return fixed4(_Color.rgb, a * _Color.a);
            }
            ENDCG
        }
    }
}
