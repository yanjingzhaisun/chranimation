Shader "Custom/TransDouble" {
Properties {
	_FrontColor ("Main Color", Color) = (1,1,1,0.3)
	_BackColor ("Back Color", Color) = (0.5, 0.5, 0.5, 0.3)
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	Alphatest Greater 0
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha
	
		
	// back side
	Pass {
	    Cull Front
	    
	    CGPROGRAM
        
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
        
        float4 _BackColor;
        
        struct v2f {
            float4 pos : SV_POSITION;            
        };
        
        v2f vert (appdata_base v) {
            v2f o;
            o.pos = mul (UNITY_MATRIX_MVP, v.vertex);            
            return o;
        }
        
        float4 frag(v2f i) : Color {
            return _BackColor;
        }
        ENDCG	
		
	}
	
	// front side
	Pass {
	    Cull Back
	    
	    CGPROGRAM
        
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
        
        float4 _FrontColor;
        
        struct v2f {
            float4 pos : SV_POSITION;            
        };
        
        v2f vert (appdata_base v) {
            v2f o;
            o.pos = mul (UNITY_MATRIX_MVP, v.vertex);            
            return o;
        }
        
        float4 frag(v2f i) : Color {
            return _FrontColor;
        }
        ENDCG	
	}
}

}