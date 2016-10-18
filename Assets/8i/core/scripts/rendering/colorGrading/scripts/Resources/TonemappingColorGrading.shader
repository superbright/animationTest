Shader "Hidden/TonemappingColorGrading"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE

        #pragma vertex vert_img
        #pragma fragmentoption ARB_precision_hint_fastest
        #pragma target 3.0

    ENDCG

    SubShader
    {
        ZTest Always Cull Off ZWrite Off
        Fog { Mode off }

        // Lut generator
        Pass
        {
            CGPROGRAM

                #pragma fragment frag_lut_gen
                #include "TonemappingColorGrading.cginc"

                sampler2D _UserLutTex;
                half4 _UserLutParams;

                half4 _Lift;
                half4 _Gamma;
                half4 _Gain;

                half _Contrast;
                half _Vibrance;
                half3 _HSV;
                half3 _ChannelMixerRed;
                half3 _ChannelMixerGreen;
                half3 _ChannelMixerBlue;
                sampler2D _CurveTex;
                half _Contribution;

                half3 rgb_to_hsv(half3 c)
                {
                    half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                    half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
                    half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
                    half d = q.x - min(q.w, q.y);
                    half e = 1.0e-10;
                    return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
                }

                half3 hsv_to_rgb(half3 c)
                {
                    half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                    half3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
                }

                // CG's fmod() is not the same as GLSL's mod() with negative values, we'll use our own
                inline half gmod(half x, half y)
                {
                    return x - y * floor(x / y);
                }

                half4 frag_lut_gen(v2f_img i) : SV_Target
                {
                    half3 neutral_lut = tex2D(_MainTex, i.uv).rgb;
                    half3 final_lut = neutral_lut;

                    // User lut + contrib
                    half3 user_luted = apply_lut(_UserLutTex, final_lut, _UserLutParams.xyz);
                    final_lut = lerp(final_lut, user_luted, _UserLutParams.w);

                    // Lift/gamma/gain
                    half3 whitepoint = half3(1,1,1);
                    half3 blackpoint = half3(0,0,0);
                    half3 offset = half3(0,0,0);

                    final_lut.r = pow(final_lut.r*((_Gain.r - _Lift.r) / (whitepoint.r - blackpoint.r)) + _Lift.r + offset.r - blackpoint.r * ((_Gain.r - _Lift.r) / (whitepoint.r - blackpoint.r)), 1.0f / _Gamma.r);
                    final_lut.g = pow(final_lut.g*((_Gain.g - _Lift.g) / (whitepoint.g - blackpoint.g)) + _Lift.g + offset.g - blackpoint.g * ((_Gain.g - _Lift.g) / (whitepoint.g - blackpoint.g)), 1.0f / _Gamma.g);
                    final_lut.b = pow(final_lut.b*((_Gain.b - _Lift.b) / (whitepoint.b - blackpoint.b)) + _Lift.b + offset.b - blackpoint.b * ((_Gain.b - _Lift.b) / (whitepoint.b - blackpoint.b)), 1.0f / _Gamma.b);

                    // Hue/saturation/value
                    half3 hsv = rgb_to_hsv(final_lut);
                    hsv.x = gmod(hsv.x + _HSV.x, 1.0);
                    hsv.yz *= _HSV.yz;
                    final_lut = hsv_to_rgb(hsv);

                    // Contrast
                    final_lut = saturate((final_lut - 0.5) * _Contrast + 0.5);

                    // Vibrance
                    half sat = max(final_lut.r, max(final_lut.g, final_lut.b)) - min(final_lut.r, min(final_lut.g, final_lut.b));
                    final_lut = lerp(Luminance(final_lut).xxx, final_lut, (1.0 + (_Vibrance * (1.0 - (sign(_Vibrance) * sat)))));

                    // Color mixer
                    final_lut = (final_lut.rrr * _ChannelMixerRed) + (final_lut.ggg * _ChannelMixerGreen) + (final_lut.bbb * _ChannelMixerBlue);

                    // Curves
                    half mr = tex2D(_CurveTex, half2(final_lut.r, 0.5)).a;
                    half mg = tex2D(_CurveTex, half2(final_lut.g, 0.5)).a;
                    half mb = tex2D(_CurveTex, half2(final_lut.b, 0.5)).a;
                    final_lut = half3(mr, mg, mb);
                    half r = tex2D(_CurveTex, half2(final_lut.r, 0.5)).r;
                    half g = tex2D(_CurveTex, half2(final_lut.g, 0.5)).g;
                    half b = tex2D(_CurveTex, half2(final_lut.b, 0.5)).b;
                    final_lut = half3(r, g, b);

                    return half4(final_lut, 1.0);
                }

            ENDCG
        }

        // Color Grading
        Pass
        {
            CGPROGRAM
                #pragma multi_compile __ GAMMA_COLORSPACE
                #pragma multi_compile __ ENABLE_COLOR_GRADING
                #pragma fragment frag_tcg
                #include "TonemappingColorGrading.cginc"
            ENDCG
        }
    }
}
