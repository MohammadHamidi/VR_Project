Shader "Custom/MultiStatePortal"
{
    Properties
    {
        [Header(Portal State)]
        [Enum(Closed,0,Opening,1,Open,2,Closing,3)] _PortalState ("Portal State", Float) = 0
        _StateTransition ("State Transition", Range(0, 1)) = 0
        
        [Header(Portal Core)]
        _Color ("Portal Color", Color) = (0, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 3)) = 1
        _EmissionColor ("Emission Color", Color) = (0, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 1
        
        [Header(Portal Pattern)]
        _MainTex ("Portal Texture", 2D) = "white" {}
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _SwirlTexture ("Swirl Pattern", 2D) = "white" {}
        
        [Header(Swirl Effect)]
        _SwirlStrength ("Swirl Strength", Range(0, 2)) = 1
        _SwirlSpeed ("Swirl Speed", Float) = 1
        _SwirlTightness ("Swirl Tightness", Range(0.1, 3)) = 1
        
        [Header(Pentagon Ring)]
        _RingRadius ("Ring Radius", Range(0.1, 1)) = 0.8
        _RingThickness ("Ring Thickness", Range(0.01, 0.2)) = 0.05
        _RingGlow ("Ring Glow", Range(0, 2)) = 1
        
        [Header(Spawn Points)]
        _SpawnPointSize ("Spawn Point Size", Range(0.01, 0.1)) = 0.03
        _SpawnPointGlow ("Spawn Point Glow", Range(0, 3)) = 2
        _SpawnPointColor ("Spawn Point Color", Color) = (1, 0.2, 0.2, 1)
        _SpawnPointCount ("Spawn Point Count", Range(3, 8)) = 5
        
        [Header(Animation)]
        _ScrollSpeed ("Scroll Speed", Float) = 0.5
        _RotationSpeed ("Rotation Speed", Float) = 0.3
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.3
        
        [Header(State Transitions)]
        _OpeningDuration ("Opening Duration", Float) = 2
        _ClosingDuration ("Closing Duration", Float) = 1.5
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0, 0.5)) = 0.1
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1, 0.8, 0, 1)
        
        [Header(Render Settings)]
        _Alpha ("Overall Alpha", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
        [Toggle] _ZWrite ("Z Write", Float) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Pass
        {
            Name "Portal"
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            Lighting Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
            };
            
            // Textures
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTexture;
            float4 _NoiseTexture_ST;
            sampler2D _SwirlTexture;
            float4 _SwirlTexture_ST;
            
            // Properties
            float _PortalState;
            float _StateTransition;
            
            fixed4 _Color;
            float _Intensity;
            fixed4 _EmissionColor;
            float _EmissionIntensity;
            
            float _SwirlStrength;
            float _SwirlSpeed;
            float _SwirlTightness;
            
            float _RingRadius;
            float _RingThickness;
            float _RingGlow;
            
            float _SpawnPointSize;
            float _SpawnPointGlow;
            fixed4 _SpawnPointColor;
            float _SpawnPointCount;
            
            float _ScrollSpeed;
            float _RotationSpeed;
            float _PulseSpeed;
            float _PulseIntensity;
            
            float _OpeningDuration;
            float _ClosingDuration;
            float _DissolveEdgeWidth;
            fixed4 _DissolveEdgeColor;
            
            float _Alpha;
            
            // Utility functions
            float2 RotateUV(float2 uv, float angle)
            {
                float2 center = float2(0.5, 0.5);
                float2 offset = uv - center;
                float cosAngle = cos(angle);
                float sinAngle = sin(angle);
                float2 rotated = float2(
                    offset.x * cosAngle - offset.y * sinAngle,
                    offset.x * sinAngle + offset.y * cosAngle
                );
                return rotated + center;
            }
            
            float DistanceToCenter(float2 uv)
            {
                return distance(uv, float2(0.5, 0.5)) * 2.0;
            }
            
            // Create pentagon ring
            float CreatePentagonRing(float2 uv)
            {
                float2 center = float2(0.5, 0.5);
                float2 toCenter = uv - center;
                
                // Convert to polar coordinates
                float angle = atan2(toCenter.y, toCenter.x);
                float dist = length(toCenter) * 2.0;
                
                // Create pentagon shape (5 sides)
                float pentagon = cos(floor(0.5 + angle * 2.5 / 6.28318) * 6.28318 / 2.5 - angle) * length(toCenter);
                pentagon = 1.0 - smoothstep(_RingRadius - _RingThickness, _RingRadius, pentagon * 2.0);
                pentagon *= smoothstep(_RingRadius + _RingThickness, _RingRadius, pentagon * 2.0);
                
                return pentagon;
            }
            
            // Create spawn points around pentagon
            float CreateSpawnPoints(float2 uv, float time)
            {
                float2 center = float2(0.5, 0.5);
                float points = 0;
                
                for(int i = 0; i < (int)_SpawnPointCount; i++)
                {
                    float angle = (float(i) / _SpawnPointCount) * 6.28318; // 2*PI
                    
                    // Position spawn point slightly outside pentagon ring
                    float spawnRadius = (_RingRadius + _RingThickness * 2) * 0.5;
                    float2 pointPos = center + float2(cos(angle), sin(angle)) * spawnRadius;
                    
                    float dist = distance(uv, pointPos);
                    float pointMask = 1.0 - smoothstep(0, _SpawnPointSize, dist);
                    
                    // Add pulsing effect
                    float pulse = (sin(time * _PulseSpeed + i * 0.5) * 0.5 + 0.5);
                    pointMask *= pulse;
                    
                    points += pointMask;
                }
                
                return saturate(points) * _SpawnPointGlow;
            }
            
            // Create swirl pattern
            float3 CreateSwirlPattern(float2 uv, float time)
            {
                float2 center = float2(0.5, 0.5);
                float2 toCenter = uv - center;
                float dist = length(toCenter);
                
                // Create spiral effect
                float angle = atan2(toCenter.y, toCenter.x);
                float spiral = angle + dist * _SwirlTightness + time * _SwirlSpeed;
                
                // Apply swirl distortion
                float2 swirlUV = uv + sin(spiral) * _SwirlStrength * (1.0 - dist) * 0.1;
                
                // Sample swirl texture
                fixed4 swirlSample = tex2D(_SwirlTexture, swirlUV * _SwirlTexture_ST.xy + _SwirlTexture_ST.zw);
                
                // Create radial fade
                float radialFade = 1.0 - smoothstep(0.0, 0.5, dist);
                
                return swirlSample.rgb * radialFade;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.pos).xy;
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y;
                float2 uv = i.uv;
                
                // Portal state logic
                float portalVisibility = 0;
                float transitionProgress = _StateTransition;
                
                if(_PortalState < 0.5) // Closed
                {
                    portalVisibility = 0;
                }
                else if(_PortalState < 1.5) // Opening
                {
                    portalVisibility = transitionProgress;
                }
                else if(_PortalState < 2.5) // Open
                {
                    portalVisibility = 1;
                }
                else // Closing
                {
                    portalVisibility = 1.0 - transitionProgress;
                }
                
                // Early exit for completely closed portal
                if(portalVisibility <= 0.001)
                {
                    return fixed4(0, 0, 0, 0);
                }
                
                // Apply rotation
                float rotationAngle = time * _RotationSpeed;
                float2 rotatedUV = RotateUV(uv, rotationAngle);
                
                // Distance from center for various effects
                float distanceFromCenter = DistanceToCenter(uv);
                
                // Create main portal effects
                float3 swirlPattern = CreateSwirlPattern(rotatedUV, time);
                float pentagonRing = CreatePentagonRing(uv);
                float spawnPoints = CreateSpawnPoints(uv, time);
                
                // Base portal texture
                fixed4 mainTex = tex2D(_MainTex, rotatedUV);
                fixed4 noiseTex = tex2D(_NoiseTexture, uv + time * _ScrollSpeed);
                
                // Combine portal patterns
                fixed4 portalBase = mainTex * noiseTex;
                portalBase.rgb += swirlPattern;
                
                // Apply portal color
                fixed4 finalColor = portalBase * _Color;
                
                // Add pentagon ring
                finalColor.rgb += pentagonRing * _RingGlow * _Color.rgb;
                
                // Add spawn points
                finalColor.rgb += spawnPoints * _SpawnPointColor.rgb;
                
                // Pulsing effect
                float pulse = (sin(time * _PulseSpeed) * 0.5 + 0.5) * _PulseIntensity + (1.0 - _PulseIntensity);
                
                // Radial falloff (stronger in center for open portal)
                float radialMask = 1.0 - smoothstep(0.0, 1.0, distanceFromCenter);
                radialMask = pow(radialMask, 0.5);
                
                // Apply opening/closing transition
                if(_PortalState < 1.5 || _PortalState >= 2.5) // Opening or Closing
                {
                    float dissolveMask = step(1.0 - portalVisibility, noiseTex.r);
                    radialMask *= dissolveMask;
                    
                    // Edge glow during transition
                    float edgeDistance = abs(noiseTex.r - (1.0 - portalVisibility)) / _DissolveEdgeWidth;
                    float edgeGlow = (1.0 - saturate(edgeDistance)) * dissolveMask;
                    finalColor.rgb += edgeGlow * _DissolveEdgeColor.rgb;
                }
                
                // Emission
                fixed3 emission = finalColor.rgb * _EmissionColor.rgb * _EmissionIntensity;
                finalColor.rgb += emission;
                
                // Apply intensity and effects
                finalColor.rgb *= _Intensity * pulse * portalVisibility;
                finalColor.a *= radialMask * _Alpha * portalVisibility;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}