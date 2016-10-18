Shader "Hidden/HVRRender_Simple"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Overlay("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	struct v2f {
		float4 pos : SV_POSITION;
		half2 uv : TEXCOORD0;
	};

	sampler2D _MainTex;
	sampler2D _Overlay;
	sampler2D_float _CameraDepthTexture;
	sampler2D_float _FrameBufferDepthTexture;
	int isDeferred;

	v2f vert(appdata_img v)
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord;
		return o;
	}

	half4 frag(v2f i) : SV_Target
	{
		half4 mainc = tex2D(_MainTex, i.uv);
		float cameraDepth = tex2D(_CameraDepthTexture, float2(i.uv.x, i.uv.y)).x;

		i.uv.y = 1.0 - i.uv.y;

		half4 overlayc = tex2D(_Overlay, i.uv);
		float frameDepth = tex2D(_FrameBufferDepthTexture, i.uv).x;

		if(cameraDepth > frameDepth){
			return overlayc;
		}
		else{
			return mainc;
		}
	}
	ENDCG

	SubShader
	{
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
	Fallback off
}

