Shader "Custom/Transparent Lines" {

	Properties{
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MainTex("Texture", 2D) = "white" {}
	}

		Subshader{

		Tags {"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			 }
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

			Pass {

			CGPROGRAM

	#pragma vertex vert
	#pragma fragment frag 

	#include "UnityCG.cginc"

		float4 _Color;
		sampler2D _MainTex;
		float4 _MainTex_ST;

		struct VertexData {
			float4 position : POSITION;
			float2 uv : TEXCOORD0;
			float4 color : COLOR;

		};

		struct Interpolators {
			float4 color : COLOR;
			float4 position : SV_POSITION;
			float2 uv : TEXCOORD0;
					};

		Interpolators vert(VertexData v) {
			Interpolators i;
			i.position = UnityObjectToClipPos(v.position);
			i.uv = TRANSFORM_TEX(v.uv, _MainTex);
			i.color = v.color * _Color;
			return i;
						}
		float4 frag(Interpolators i) : SV_TARGET{
			float4 color = tex2D(_MainTex, i.uv) * i.color;
			return color;

						}
  			ENDCG

						}

	}

}