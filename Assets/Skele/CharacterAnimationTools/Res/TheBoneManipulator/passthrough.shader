Shader "Custom/passthrough" {
Properties {
	_MainTex ("Particle Texture", 2D) = "white" {}
	_Factor ("Soft Particles Factor", Float) = 0.2
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask RGB
	ZTest Always
	Cull Off
	Lighting Off
	ZWrite Off
	Fog { Mode Off }

	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_particles

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			fixed4 _TintColor;
			
			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float4 projPos : TEXCOORD1;
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				
				o.projPos = ComputeScreenPos (o.vertex);
				//COMPUTE_EYEDEPTH(o.projPos.z);
				o.projPos.z = COMPUTE_DEPTH_01;
				
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				
				return o;
			}

			sampler2D _CameraDepthTexture;
			float _Factor;
			
			fixed4 frag (v2f i) : COLOR
			{
				float sceneZ = Linear01Depth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
				float partZ = i.projPos.z - 0.00005;
				if( sceneZ < partZ )
				{
//					i.color.a = saturate( i.color.a - _Factor * (partZ - sceneZ) );
					i.color.a *= _Factor;
				}
				
				return i.color * tex2D(_MainTex, i.texcoord);// * i.color.a;
			}
			ENDCG 
		}
	}
}
}
