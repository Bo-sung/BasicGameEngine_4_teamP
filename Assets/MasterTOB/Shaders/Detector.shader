Shader "URP/Detector"
{
    Properties
    {
        [Header(Base Properties)]
        [Space] _BaseColorMap ("Base Color", 2D) = "white" {}
        _BaseColorStrength ("Base Color Strength", Range(0, 1)) = 1.0
        _OpacityMap ("Opacity Mask", 2D) = "white" {}
        _OpacityStrength ("Opacity Strength", Range(0, 1)) = 1.0
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {} // 이름 변경
        _BumpStrength ("Bump Strength", Range(0, 1)) = 1.0
        _AOMap ("AO", 2D) = "white" {}
        _AOStrength ("AO Strength", Range(0, 1)) = 1.0
        _MetallicMap ("Metallic", 2D) = "black" {}
        _MetallicStrength ("Metallic Strength", Range(0, 1)) = 1.0
        _RoughnessMap ("Roughness", 2D) = "white" {}
        _RoughnessStrength ("Roughness Strength", Range(0, 1)) = 1.0
        _DisplacementMap ("Displacement", 2D) = "black" {}
        _GlowMap ("Glow", 2D) = "black" {}
        _GlowStrength ("Glow Strength", Range(0, 1)) = 1.0
        _BlendMap ("Blend", 2D) = "white" {}
        _BlendStrength ("Blend Strength", Range(0, 1)) = 1.0
        [Toggle(_DOUBLE_SIDED_ON)] _DoubleSided ("Double Sided", Float) = 0

        [Header(Rain Properties)]
        [Space] [Normal] _Rain_drops ("Rain", 2D) = "bump" {}
        [Normal] _Rain_static ("Rain static", 2D) = "bump" {}
        _Rain_intensity ("Rain intensity", Range(0, 10)) = 0
        _Rainspeed ("Rain speed", Range(0, 40)) = 0
        _Raintiling ("Rain tiling", Vector) = (0, 0, 0, 0)
        _RainRotation ("Rain Rotation", Range(0, 360)) = 0

        [Header(Material Properties)]
        [Space] _Metallicshift ("Metallic shift", Range(0, 2)) = 1.0
        _Occlusionshift ("Occlusion shift", Range(0, 2)) = 1.0
        _Smoothness ("Smoothness", Range(0, 5)) = 1.0

        [Header(GUI Layer)]
        [Space] _GUI_2_Flipbook ("GUI_2_Flipbook", 2D) = "white" {}
        _GUIPanOffset ("GUI Pan Offset", Vector) = (0, 0, 0, 0)
        _GUIScale ("GUI Scale", Range(0.01, 1.0)) = 0.5
        _GUIRotation ("GUI Rotation", Range(0, 360)) = 0
        _GUIColor ("GUI Color", Color) = (1, 1, 1, 1)
        _GUIAnimSpeed ("GUI Anim Speed", Range(0, 50)) = 25
        _Emissionintensity ("Emission intensity", Range(0, 3)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200
        
        // Double sided rendering setup
        Cull [_DoubleSided]
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColorMap_ST;
            float _BaseColorStrength;
            float _OpacityStrength;
            float _AlphaCutoff;
            float _BumpStrength;
            float _AOStrength;
            float _MetallicStrength;
            float _RoughnessStrength;
            float _GlowStrength;
            float _BlendStrength;
            half _DoubleSided;
            float _Rain_intensity;
            float _Rainspeed;
            float2 _Raintiling;
            float _RainRotation;
            float _Metallicshift;
            float _Occlusionshift;
            float _Smoothness;
            float4 _GUIPanOffset;
            float _GUIScale;
            float _GUIRotation;
            float4 _GUIColor;
            float _GUIAnimSpeed;
            float _Emissionintensity;
        CBUFFER_END

        // Texture declarations
        TEXTURE2D(_BaseColorMap); SAMPLER(sampler_BaseColorMap);
        TEXTURE2D(_OpacityMap); SAMPLER(sampler_OpacityMap);
        TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap); // 이름 변경
        TEXTURE2D(_AOMap); SAMPLER(sampler_AOMap);
        TEXTURE2D(_MetallicMap); SAMPLER(sampler_MetallicMap);
        TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_DisplacementMap); SAMPLER(sampler_DisplacementMap);
        TEXTURE2D(_GlowMap); SAMPLER(sampler_GlowMap);
        TEXTURE2D(_BlendMap); SAMPLER(sampler_BlendMap);
        TEXTURE2D(_Rain_drops); SAMPLER(sampler_Rain_drops);
        TEXTURE2D(_Rain_static); SAMPLER(sampler_Rain_static);
        TEXTURE2D(_GUI_2_Flipbook); SAMPLER(sampler_GUI_2_Flipbook);
        
        // Blending normals function
        float3 BlendNormalsURP(float3 n1, float3 n2)
        {
            return normalize(float3(n1.xy + n2.xy, n1.z * n2.z));
        }
        
        ENDHLSL
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _DOUBLE_SIDED_ON
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                float4 fogFactorAndVertexLight : TEXCOORD7;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Transform position from object to world space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                // Store position, normal, tangent, bitangent
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.bitangentWS = normalInput.bitangentWS;
                
                // Get view direction and pass UV coordinates
                output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseColorMap);
                
                // Shadow and lighting
                output.shadowCoord = GetShadowCoord(vertexInput);
                float3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                output.fogFactorAndVertexLight = float4(fogFactor, vertexLight);
                
                return output;
            }
            
            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                // Setup the input surface data for lighting calculations
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.viewDirectionWS = SafeNormalize(input.viewDirWS);
                inputData.shadowCoord = input.shadowCoord;
                
                // Sample base textures
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv);
                float opacity = SAMPLE_TEXTURE2D(_OpacityMap, sampler_OpacityMap, input.uv).r;
                float4 ao = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, input.uv);
                float4 glow = SAMPLE_TEXTURE2D(_GlowMap, sampler_GlowMap, input.uv);
                float metallic = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv).r;
                float roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r;
                
                // Handle normal mapping with double-sided support
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                normalTS = lerp(float3(0, 0, 1), normalTS, _BumpStrength);
                
                // Handle double-sided normals
                #if defined(_DOUBLE_SIDED_ON)
                    if (!isFrontFace)
                    {
                        normalTS.z = -normalTS.z;
                    }
                #endif

                // Transform normal from tangent to world space
                float sgn = input.tangentWS.w;
                float3 bitangentWS = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                float3 normalWS = TransformTangentToWorld(normalTS, float3x3(input.tangentWS.xyz, bitangentWS.xyz, input.normalWS.xyz));
                normalWS = normalize(normalWS);

                // Rain effect
                float2 uv_Rain = input.uv * _Raintiling;
                float angle = _RainRotation * 3.14159 / 180.0;
                float2x2 rotationMatrix = float2x2(cos(angle), -sin(angle), sin(angle), cos(angle));
                uv_Rain = mul(rotationMatrix, uv_Rain);
                
                float fbtotaltiles = 5.0 * 5.0;
                float fbcolsoffset = 1.0 / 5.0;
                float fbrowsoffset = 1.0 / 5.0;
                float fbspeed = _Time.y * _Rainspeed;
                float2 fbtiling = float2(fbcolsoffset, fbrowsoffset);
                float fbcurrenttileindex = round(fmod(fbspeed, fbtotaltiles));
                fbcurrenttileindex += (fbcurrenttileindex < 0) ? fbtotaltiles : 0;
                float fblinearindextox = round(fmod(fbcurrenttileindex, 5.0));
                float fboffsetx = fblinearindextox * fbcolsoffset;
                float fblinearindextoy = round(fmod((fbcurrenttileindex - fblinearindextox) / 5.0, 5.0));
                fblinearindextoy = (5.0 - 1) - fblinearindextoy;
                float fboffsety = fblinearindextoy * fbrowsoffset;
                float2 fboffset = float2(fboffsetx, fboffsety);
                half2 fbuv = frac(uv_Rain) * fbtiling + fboffset;
                
                float3 rainDynamic = UnpackNormalScale(SAMPLE_TEXTURE2D(_Rain_drops, sampler_Rain_drops, fbuv), _Rain_intensity);
                float2 rainStaticUV = uv_Rain * float2(40.0, 10.0);
                float3 rainStatic = UnpackNormal(SAMPLE_TEXTURE2D(_Rain_static, sampler_Rain_static, rainStaticUV));
                
                // Apply rain normals
                float rainBlend = input.normalWS.y; // Use world normal for rain blending
                float3 rainNormalTS = lerp(rainDynamic, rainStatic, rainBlend);
                float3 finalNormalTS = BlendNormalsURP(normalTS, rainNormalTS);
                float3 finalNormalWS = TransformTangentToWorld(finalNormalTS, float3x3(input.tangentWS.xyz, bitangentWS.xyz, input.normalWS.xyz));
                finalNormalWS = normalize(finalNormalWS);
                
                // Set the final normal
                inputData.normalWS = finalNormalWS;
                
                // Additional input data for lighting
                inputData.fogCoord = input.fogFactorAndVertexLight.x;
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                inputData.bakedGI = 0; // No baked GI 
                // Consider SampleSH for proper ambient lighting
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
                
                // GUI Layer effect
                float3 emission = glow.rgb * _GlowStrength;
                float2 uv_GUI = input.uv - _GUIPanOffset.xy;
                float guiAngle = _GUIRotation * 3.14159 / 180.0;
                float2x2 rotationMatrixGUI = float2x2(cos(guiAngle), -sin(guiAngle), sin(guiAngle), cos(guiAngle));
                uv_GUI = mul(rotationMatrixGUI, uv_GUI) + _GUIPanOffset.xy;
                float quadSize = _GUIScale;
                float2 quadUV = float2(
                    saturate((uv_GUI.x - _GUIPanOffset.x + 0.5 * quadSize) / quadSize),
                    saturate((uv_GUI.y - _GUIPanOffset.y + 0.5 * quadSize) / quadSize)
                );
                
                if (quadUV.x >= 0 && quadUV.x <= 1 && quadUV.y >= 0 && quadUV.y <= 1)
                {
                    float fbtotaltilesGUI = 4.0 * 4.0;
                    float fbcolsoffsetGUI = 1.0 / 4.0;
                    float fbrowsoffsetGUI = 1.0 / 4.0;
                    float fbspeedGUI = _Time.y * _GUIAnimSpeed;
                    float2 fbtilingGUI = float2(fbcolsoffsetGUI, fbrowsoffsetGUI);
                    float fbcurrenttileindexGUI = round(fmod(fbspeedGUI, fbtotaltilesGUI));
                    fbcurrenttileindexGUI += (fbcurrenttileindexGUI < 0) ? fbtotaltilesGUI : 0;
                    float fblinearindextoxGUI = round(fmod(fbcurrenttileindexGUI, 4.0));
                    float fboffsetxGUI = fblinearindextoxGUI * fbcolsoffsetGUI;
                    float fblinearindextoyGUI = round(fmod((fbcurrenttileindexGUI - fblinearindextoxGUI) / 4.0, 4.0));
                    fblinearindextoyGUI = (4.0 - 1) - fblinearindextoyGUI;
                    float fboffsetyGUI = fblinearindextoyGUI * fbrowsoffsetGUI;
                    float2 fboffsetGUI = float2(fboffsetxGUI, fboffsetyGUI);
                    half2 fbuvGUI = quadUV * fbtilingGUI + fboffsetGUI;
                    
                    float guiEmission = SAMPLE_TEXTURE2D(_GUI_2_Flipbook, sampler_GUI_2_Flipbook, fbuvGUI).a * _Emissionintensity;
                    emission += guiEmission * _GUIColor.rgb;
                }
                
                // Setup surface data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor.rgb * _BaseColorStrength;
                surfaceData.specular = float3(0.0, 0.0, 0.0);
                surfaceData.metallic = metallic * _MetallicStrength * _Metallicshift;
                surfaceData.smoothness = _Smoothness * (1.0 - roughness * _RoughnessStrength);
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emission;
                surfaceData.occlusion = lerp(1.0, ao.r * _Occlusionshift, _AOStrength);
                surfaceData.alpha = opacity * _OpacityStrength;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;
                
                // Alpha clipping
                clip(surfaceData.alpha - _AlphaCutoff);
                
                // Calculate final color with Universal Lighting
                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, inputData.fogCoord);
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // ShadowCaster Pass for shadows
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_DoubleSided]

            HLSLPROGRAM
            #pragma target 2.0
            
            // Material keywords
            #pragma shader_feature_local_fragment _DOUBLE_SIDED_ON
            
            // GPU Instancing
            #pragma multi_compile_instancing
            
            // This is used during shadow map generation to differentiate between directional and punctual light shadows
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        
        // DepthOnly Pass
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_DoubleSided]

            HLSLPROGRAM
            #pragma target 2.0
            
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            // Material Keywords
            #pragma shader_feature_local_fragment _DOUBLE_SIDED_ON
            
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}