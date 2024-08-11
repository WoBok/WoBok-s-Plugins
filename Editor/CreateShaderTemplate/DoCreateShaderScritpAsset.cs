using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

public class DoCreateShaderScritpAsset : EndNameEditAction
{
    const string URP_Shader = @"
Shader ""#PATH#"" {
    Properties {
        _BaseMap (""Albedo"", 2D) = ""white"" { }
        _BaseColor (""Color"", Color) = (1, 1, 1, 1)
    }

    SubShader {
        Tags { ""RenderPipeline"" = ""UniversalPipeline"" }

        Pass {
            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings {
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 positionCS : SV_POSITION;
            };
            
            sampler2D _BaseMap;
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            CBUFFER_END

            Varyings Vertex(Attributes input) {

                Varyings output;
                
                output.positionCS = mul(UNITY_MATRIX_MVP, input.positionOS);
                
                output.normalWS = normalize(mul(input.normalOS, (float3x3)unity_WorldToObject));

                output.uv = input.texcoord.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target {

                half4 albedo = tex2D(_BaseMap, input.uv);

                half4 diffuse = albedo * (dot(input.normalWS, normalize(_MainLightPosition.xyz)) * 0.5 + 0.5);

                return diffuse * _BaseColor;
            }
            ENDHLSL
        }
    }
}
";
    const string URP_PBR_Shader = @"
Shader ""#PATH#"" {
    Properties {
        _BaseMap (""Albedo"", 2D) = ""white"" { }
        _BaseColor (""Color"", Color) = (1, 1, 1, 1)

        [Header(PBR)]
        [Space(5)]
        _Smoothness (""Smoothness"", Range(0, 1)) = 0
        _Metallic (""Metallic"", Range(0, 1)) = 0
    }

    SubShader {
        Tags { ""RenderPipeline"" = ""UniversalPipeline"" }

        Pass {
            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl""

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
            };

            struct Varyings {
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 positionCS : SV_POSITION;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 3);
            };
            
            sampler2D _BaseMap;
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            float _Smoothness;
            float _Metallic;
            CBUFFER_END

            Varyings Vertex(Attributes input) {
                Varyings output;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                return output;
            }

            void InitializeInputData(Varyings input, out InputData inputData) {
                inputData = (InputData)0;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, input.normalWS);
            }

            void InitializeSurfaceData(float2 uv, out SurfaceData surfaceData) {
                surfaceData = (SurfaceData)0;
                half4 albedo = tex2D(_BaseMap, uv);
                surfaceData.albedo = albedo.rgb * _BaseColor.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1;
                surfaceData.alpha = albedo.a * _BaseColor.a;
            }

            half4 Fragment(Varyings input) : SV_Target {
                InputData inputData;
                InitializeInputData(input, inputData);

                SurfaceData surfaceData;
                InitializeSurfaceData(input.uv, surfaceData);

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }
    }
}
";
    const string URP_DOTS_Shader = @"
Shader ""#PATH#"" {
    Properties {
        _BaseMap (""Albedo"", 2D) = ""white"" { }
        _BaseColor (""Color"", Color) = (1, 1, 1, 1)
    }

    SubShader {
        Tags { ""RenderPipeline"" = ""UniversalPipeline"" }

        Pass {
            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
            #include_with_pragmas ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl""

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED

                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _BaseMap_ST)
                UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                static float4 unity_DOTS_Sampled_BaseMap_ST;
                static half4 unity_DOTS_Sampled_BaseColor;

                void SetupDOTSLitMaterialPropertyCaches() {
                    unity_DOTS_Sampled_BaseMap_ST = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseMap_ST);
                    unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
                }

                #undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
                #define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSLitMaterialPropertyCaches()

                #define _BaseMap_ST     unity_DOTS_Sampled_BaseMap_ST
                #define _BaseColor      unity_DOTS_Sampled_BaseColor

            #endif

            Varyings Vertex(Attributes input) {

                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target {

                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 diffuse = albedo * (dot(input.normalWS, normalize(_MainLightPosition.xyz)) * 0.5 + 0.5);

                return diffuse * _BaseColor;
            }
            ENDHLSL
        }
    }
}
";
    public override void Action(int instanceId, string pathName, string resourceFile)
    {
        Object o = CreateScriptAssetFromTemplate(pathName, resourceFile);
        if (o != null)
            ProjectWindowUtil.ShowCreatedAsset(o);
    }
    internal static Object CreateScriptAssetFromTemplate(string pathName, string resourceFile)
    {
        string fileName = Path.GetFileNameWithoutExtension(pathName);
        fileName = fileName.Replace('_', ' ');
        string templateText;
        switch (resourceFile)
        {
            case "URP_Shader":
                templateText = URP_Shader;
                break;
            case "URP_PBR_Shader":
                templateText = URP_PBR_Shader;
                break;
            case "URP_DOTS_Shader":
                templateText = URP_DOTS_Shader;
                break;
            default:
                templateText = URP_Shader;
                break;
        }
        templateText = templateText.Replace("#PATH#", $"URP Shader/{fileName}");
        string templateContent = SetLineEndings(templateText, EditorSettings.lineEndingsForNewScripts);
        string fullPath = Path.GetFullPath(pathName);
        File.WriteAllText(fullPath, templateContent);
        AssetDatabase.ImportAsset(pathName);
        return AssetDatabase.LoadAssetAtPath(pathName, typeof(Object));
    }
    internal static string SetLineEndings(string content, LineEndingsMode lineEndingsMode)
    {
        content = Regex.Replace(content, "\\r\\n?|\\n", lineEndingsMode switch
        {
            LineEndingsMode.OSNative => (Application.platform != RuntimePlatform.WindowsEditor) ? "\n" : "\r\n",
            LineEndingsMode.Unix => "\n",
            LineEndingsMode.Windows => "\r\n",
            _ => "\n",
        });
        return content;
    }
}