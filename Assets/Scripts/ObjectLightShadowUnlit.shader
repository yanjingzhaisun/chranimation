// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/ObjectLightShadowUnlit" {
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {}
    _BaseColor("BaseColor", Color) = (1, 1, 1, 1)
    _BaseColorDarkness("BaseColorDarkness", Float) = 0.2
        _LightAffectRange("LightAffectRange", FLoat) = 1
    }
        SubShader{
        Tags{ "Queue" = "Geometry" "RenderType" = "Opaque" "MyTag" = "Object"}
        Pass{
        Tags{ "LightMode" = "ForwardBase" } // pass for 
                                            // 4 vertex lights, ambient light & first pixel light
        Blend SrcAlpha OneMinusSrcAlpha // use alpha blending

        CGPROGRAM
#pragma multi_compile_fwdbase 
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_fog_exp2
#pragma fragmentoption ARB_precision_hint_fastest

#include "UnityCG.cginc" 
#include "AutoLight.cginc"

        uniform float4 _LightColor0;
    // color of light source (from "Lighting.cginc")

    // User-specified properties
    float4 _BaseColor;
    float _BaseColorDarkness;
    float _LightAffectRange;
    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _ShadowColor = (0.54, 0.42, 0.46, 1);

    struct vertexInput {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
    };
    struct vertexOutput {
        float4 pos : SV_POSITION;
        float4 posWorld : TEXCOORD0;
        float3 vertexLighting : TEXCOORD2;
        half2 texcoord : TEXCOORD1;
        LIGHTING_COORDS(3, 4)
    };

    vertexOutput vert(vertexInput input)
    {
        vertexOutput output;

        float4x4 modelMatrix = unity_ObjectToWorld;
        float4x4 modelMatrixInverse = unity_WorldToObject;

        output.posWorld = mul(modelMatrix, input.vertex);
        output.pos = mul(UNITY_MATRIX_MVP, input.vertex);

        // Diffuse reflection by four "vertex lights"            
        output.vertexLighting = float3(0.0, 0.0, 0.0);
        output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
#ifdef VERTEXLIGHT_ON
        for (int index = 0; index < 4; index++)
        {
            float4 lightPosition = float4(unity_4LightPosX0[index],
                unity_4LightPosY0[index],
                unity_4LightPosZ0[index], 1.0);

            float3 vertexToLightSource =
                lightPosition.xyz - output.posWorld.xyz;
            float3 lightDirection = normalize(vertexToLightSource);
            float squaredDistance =
                dot(vertexToLightSource, vertexToLightSource);
            float attenuation = 1.0 / (1.0 +
                unity_4LightAtten0[index] * squaredDistance);
            float3 diffuseReflection = attenuation
                * unity_LightColor[index].rgb;
            output.vertexLighting =
                output.vertexLighting + diffuseReflection;
        }
#endif
        TRANSFER_VERTEX_TO_FRAGMENT(output);
        return output;
    }

    float4 frag(vertexOutput input) : COLOR
    {
        fixed atten = LIGHT_ATTENUATION(input);	// Light attenuation + shadows.
        float3 viewDirection = normalize(
            _WorldSpaceCameraPos - input.posWorld.xyz);
    float3 lightDirection;
    float attenuation;
    fixed4 col = tex2D(_MainTex, input.texcoord) * _BaseColor;
    if (0.0 == _WorldSpaceLightPos0.w) // directional light?
    {
        attenuation = 1.0; // no attenuation
        lightDirection =
            normalize(_WorldSpaceLightPos0.xyz);
    }
    else // point or spot light
    {
        float3 vertexToLightSource =
            _WorldSpaceLightPos0.xyz - input.posWorld.xyz;
        float distance = length(vertexToLightSource) / _LightAffectRange;
        attenuation = 8.0 / distance; // linear attenuation 
        lightDirection = normalize(vertexToLightSource);
    }

    float3 ambientLighting =
        UNITY_LIGHTMODEL_AMBIENT.rgb;

    float3 diffuseReflection =
        attenuation * _LightColor0.rgb;

    return float4(col * _BaseColorDarkness + ambientLighting * col * _BaseColorDarkness
        + diffuseReflection * col * _BaseColorDarkness * atten - (1-_ShadowColor) * (1 - atten), col.a);
    }
        ENDCG
    }

        Pass{
        Tags{ "LightMode" = "ForwardAdd" }
        // pass for additional light sources
        Blend One One // additive blending 

        CGPROGRAM

#pragma vertex vert  
#pragma fragment frag 
#pragma multi_compile_fwdadd_fullshadows
#pragma fragmentoption ARB_fog_exp2
#pragma fragmentoption ARB_precision_hint_fastest

#include "UnityCG.cginc" 
#include "AutoLight.cginc"
        uniform float4 _LightColor0;
    // color of light source (from "Lighting.cginc")

    // User-specified properties
    float4 _BaseColor;
    float _BaseColorDarkness;
    float _LightAffectRange;
    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _ShadowColor = (0.54, 0.42, 0.46, 1);

    struct vertexInput {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
    };
    struct vertexOutput {
        float4 pos : SV_POSITION;
        float4 posWorld : TEXCOORD0;
        float2 texcoord: TEXCOORD1;
        LIGHTING_COORDS(2, 3)
    };

    vertexOutput vert(vertexInput v)
    {
        vertexOutput output;

        float4x4 modelMatrix = unity_ObjectToWorld;
        float4x4 modelMatrixInverse = unity_WorldToObject;

        output.posWorld = mul(modelMatrix, v.vertex);
        output.pos = mul(UNITY_MATRIX_MVP, v.vertex);
        output.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        TRANSFER_VERTEX_TO_FRAGMENT(output);
        return output;
    }

    float4 frag(vertexOutput input) : COLOR
    {
        fixed atten = LIGHT_ATTENUATION(input);	// Light attenuation + shadows.
        float3 viewDirection = normalize(
            _WorldSpaceCameraPos.xyz - input.posWorld.xyz);
    float3 lightDirection;
    float attenuation;
    fixed4 col = tex2D(_MainTex, input.texcoord) * _BaseColor;
    if (0.0 == _WorldSpaceLightPos0.w) // directional light?
    {
        attenuation = 1.0; // no attenuation
        lightDirection =
            normalize(_WorldSpaceLightPos0.xyz);
    }
    else // point or spot light
    {
        float3 vertexToLightSource =
            _WorldSpaceLightPos0.xyz - input.posWorld.xyz;
        float distance = length(vertexToLightSource) / _LightAffectRange;
        attenuation = 8.0 / distance; // linear attenuation 
        lightDirection = normalize(vertexToLightSource);
    }

    float3 diffuseReflection =
        attenuation * _LightColor0.rgb;


    return float4(col * diffuseReflection * _BaseColorDarkness * atten - (1 - _ShadowColor) * (1-atten) , 1.0);
    // no ambient lighting in this pass
    }

        ENDCG
    }

    }
        Fallback "Specular"
}
