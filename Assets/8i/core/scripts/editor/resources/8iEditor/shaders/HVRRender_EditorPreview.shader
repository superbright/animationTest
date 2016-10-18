Shader "Hidden/HVRRender_EditorPreview"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}

		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f i) : SV_Target
			{
				// On D3D when AA is used, the main texture and scene depth texture
				// will come out in different vertical orientations.
				// So flip sampling of the texture when that is the case (main texture
				// texel size will have negative Y).

#if UNITY_UV_STARTS_AT_TOP
				i.uv.y = 1 - i.uv.y;
#endif

				return  tex2D(_MainTex, i.uv);
			}
				ENDCG
		}
	}
}
