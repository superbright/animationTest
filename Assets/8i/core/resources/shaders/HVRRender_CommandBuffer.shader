Shader "Hidden/8i/HVRRender_CommandBuffer" {
	Properties{
		_oCOL("Offscreen Color", 2D) = "" {}
		_oDEP("Offscreen Depth", 2D) = "" {}

		_FadeEnabled("Fade Enabled", Int) = 0
		_FadeColor("Fade Color", COLOR) = (0, 0, 0, 0)
		_FadeValue("Fade Value", Float) = 0.0
		_FadeHeightMin("Height Fade Min", Float) = 0.0
		_FadeHeightMax("Height Fade Max", Float) = 0.0
	}

	CGINCLUDE
		#include "UnityStandardCore.cginc"

		struct ia_out
		{
			float4 vertex : POSITION;
		};

		struct vs_out
		{
			float4 vertex	: SV_POSITION;
			float4 spos		: TEXCOORD0;
		};

		vs_out vert(ia_out v)
		{
			vs_out o;
			o.vertex = v.vertex;
			o.spos = ComputeScreenPos(v.vertex);
			return o;
		}

		struct gbuffer_out
		{
			half4 diffuse           : SV_Target0; // RT0: diffuse color (rgb), occlusion (a)
			half4 spec_smoothness   : SV_Target1; // RT1: spec color (rgb), smoothness (a)
			//half4 normal            : SV_Target2; // RT2: normal (rgb), --unused, very low precision-- (a) 
			half4 emission          : SV_Target3; // RT3: emission (rgb), --unused-- (a)
			float depth : SV_Depth;
		};

		uniform sampler2D _oCOL;
		uniform sampler2D _oDEP;

		int g_hdr;

		int _FadeEnabled;
		float4 _FadeColor;
		uniform float _FadeValue;
		uniform float _FadeHeightMin;
		uniform float _FadeHeightMax;

		uniform float4x4 _ProjectInverse;
		uniform float4x4 _ViewProjectInverse;

		float BinaryDither4x4(float value, float2 sceneUVs)
		{
			float4x4 mtx = float4x4(
				float4(1, 9, 3, 11) / 17.0,
				float4(13, 5, 15, 7) / 17.0,
				float4(4, 12, 2, 10) / 17.0,
				float4(16, 8, 14, 6) / 17.0
				);
			float2 px = floor(_ScreenParams.xy * sceneUVs);
			int xSmp = fmod(px.x, 4);
			int ySmp = fmod(px.y, 4);
			float4 xVec = 1 - saturate(abs(float4(0, 1, 2, 3) - xSmp));
			float4 yVec = 1 - saturate(abs(float4(0, 1, 2, 3) - ySmp));
			float4 pxMult = float4(dot(mtx[0], yVec), dot(mtx[1], yVec), dot(mtx[2], yVec), dot(mtx[3], yVec));
			return round(value + dot(pxMult, xVec));
		}

		float4 DepthToWPOS(float depth, float2 uv)
		{
			// Returns World Position of a pixel from clip-space depth map..
			//float depth = tex2D(_oDEP, uv);
			// H is the viewport position at this pixel in the range -1 to 1.

			depth = depth * 2 - 1;

#if UNITY_UV_STARTS_AT_TOP
			uv.y = 1.0 - uv.y;
#endif

			float4 H = float4((uv.x) * 2 - 1, (uv.y) * 2 - 1, depth, 1.0);
			float4 D = mul(_ViewProjectInverse, H);
			D /= D.w;

			return D;
		}

		gbuffer_out frag_gbuffer(vs_out v)
		{
#if UNITY_UV_STARTS_AT_TOP
			v.spos.y = 1.0 - v.spos.y;
#endif

			half4 col = tex2D(_oCOL, v.spos.xy);
			half4 spec = half4(0, 0, 0, 0);
			float dep = tex2D(_oDEP, v.spos.xy);
			half4 WPOS;
			half4 normal;

			if (_FadeEnabled == 1)
			{
				WPOS = DepthToWPOS(dep, v.spos.xy);

				float alpha = saturate((WPOS.y - _FadeHeightMin) / (_FadeHeightMax - _FadeHeightMin));
				alpha = alpha - clamp(_FadeValue, 0, 1);
				alpha = clamp(alpha, 0, 1);

				col = lerp(lerp(col, _FadeColor, _FadeColor.a), col, alpha);
				clip(BinaryDither4x4(alpha - 1.5, v.spos.xy));
			}

			//Calculate Normals
			normal = half4(normalize(cross(ddy(WPOS.xyz), ddx(WPOS.xyz))), 1);

			//Move the normals into the correct range
			normal = normal * 0.5 + 0.5;

			//Always set this to 0. Is apparently an occlusion value.
			col.a = 1;

			half4 emission = g_hdr ? col : exp2(-col);

			gbuffer_out o;

			o.diffuse = col;
			o.spec_smoothness = spec;
			o.emission = emission;
			//o.normal			= normal;		//Uncomment when we finally have normals as a source texture
			o.depth = dep;

			return o;
		}

		ENDCG

		SubShader
		{
			Fog{ Mode Off }					// no fog in g-buffers pass
			Cull Off							// Render both front and back facing polygons.
			ZTest Less      					// Renders without drawing over the skybox
			//ZWrite On							// Default is on
			//Lighting Off

			Pass
			{
				Name "DEFERRED"

				CGPROGRAM
				#pragma target 3.0
				#pragma vertex vert
				#pragma fragment frag_gbuffer 
				ENDCG
			}
		}
		Fallback "Diffuse"
}
