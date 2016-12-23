Shader "Custom/InvColor" {
	Properties {
		_Color ("Tint Color", Color) = (1,1,1,1)
	}
	Category {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
				
		SubShader {
		
			Pass {
			
    			Blend One One
        		BlendOp Sub
        		Cull Off
        		ZWrite Off
        					
    			CGPROGRAM
    			#pragma vertex vert
    			#pragma fragment frag			
    
    			#include "UnityCG.cginc"
    			
    			fixed4 _Color;
    			
    			struct appdata_t {
    				float4 vertex : POSITION;				
    			};
    
    			struct v2f {
    				float4 vertex : POSITION;
    				fixed4 color : COLOR;
    			};
    			
    			v2f vert (appdata_t v)
    			{
    				v2f o;
    				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);				
    				o.color = _Color;
    				return o;
    			}
    			
    			fixed4 frag (v2f i) : COLOR
    			{
    				return i.color;
    			}
    			ENDCG 
		    }
		  }
	} 
	FallBack "Diffuse"
}
