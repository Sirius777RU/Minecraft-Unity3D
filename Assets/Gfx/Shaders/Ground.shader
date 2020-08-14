Shader "Custom/Ground"
{
    Properties
    {
        _MainTex ("Color (RGB) Alpha (A)", 2D) = "white" {}
        //_Glossiness ("Smoothness", Range(0,1)) = 0.5
        //_Metallic ("Metallic", Range(0,1)) = 0.0
        _Cutout("Cutout", Range(0,1)) = .95               
        _Thickness ("Thickness", 2D) = "bump" {}
        
        _Emission ("Emission", 2D) = "black" {}     
        
        _EmissionStrength ("Emission Strength", float) = 0
		_Power ("Subsurface Power", Float) = 1.0
		_Distortion ("Subsurface Distortion", Float) = 0.0
		_Scale ("Subsurface Scale", Float) = 0.5
		_SubColor ("Subsurface Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "LightMode" = "Standart" "Queue" = "AlphaTest" "RenderType"="TransparentCutout" }
        Cull off
        ZWrite on
 

        CGPROGRAM
        #pragma surface surf Translucent alphatest:_Cutout addshadow fullforwardshadows nometa noforwardadd 
        #pragma target 3.0
        
        #include "UnityCG.cginc"

        sampler2D _MainTex, _Thickness, _Emission; 

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _SubColor;
        float _Scale, _Power, _Distortion, _EmissionStrength;

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Gloss = tex2D(_Thickness, IN.uv_MainTex).r;
            o.Emission = c.rgb * (tex2D(_Emission, IN.uv_MainTex));
            o.Alpha = c.a;
        }
        
        inline float4 LightingTranslucent (SurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten)
		{
			viewDir = normalize ( viewDir );
			lightDir = normalize ( lightDir );

			// Translucency.
			half3 transLightDir = lightDir + s.Normal * _Distortion;
			float transDot = pow ( max (0, dot (viewDir, -transLightDir ) ), _Power ) * _Scale;
			fixed3 transLight = (atten * 2) * ( transDot ) * s.Alpha * _SubColor.rgb;
			fixed3 transAlbedo = s.Albedo * _LightColor0.rgb * transLight;

			// Regular BlinnPhong.
			half3 h = (lightDir + viewDir);
			fixed diff = max (0, dot (s.Normal, lightDir));
			float nh = max (0, dot (s.Normal, h));
			fixed3 diffAlbedo = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * _SpecColor.rgb) * (atten * 2);

            float thick = s.Gloss;

			// Add the two together.
			float4 c;
			c.rgb = diffAlbedo + (transAlbedo * thick) + (s.Emission * _EmissionStrength);
			c.a = _LightColor0.a * atten;
			
			
			return c;
		}

        ENDCG
    }
    FallBack off
}
