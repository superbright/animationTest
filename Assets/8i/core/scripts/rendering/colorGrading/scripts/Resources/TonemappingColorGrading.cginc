#include "UnityCG.cginc"

sampler2D _MainTex;
half4 _MainTex_TexelSize;

half _Exposure;
sampler2D _LutTex;
half4 _LutParams;

sampler2D _LumTex;
half _AdaptationSpeed;
half _MiddleGrey;
half _AdaptationMin;
half _AdaptationMax;

inline half LinToPerceptual(half3 color)
{
    half lum = Luminance(color);
    return log(max(lum, 0.001));
}

inline half PerceptualToLin(half f)
{
    return exp(f);
}

half4 frag_log(v2f_img i) : SV_Target
{
    half sum = 0.0;
    sum += LinToPerceptual(tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(-1,-1)).rgb);
    sum += LinToPerceptual(tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2( 1, 1)).rgb);
    sum += LinToPerceptual(tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(-1, 1)).rgb);
    sum += LinToPerceptual(tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2( 1,-1)).rgb);
    half avg = sum / 4.0;
    return half4(avg, avg, avg, avg);
}

half4 frag_exp(v2f_img i) : SV_Target
{
    half sum = 0.0;
    sum += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(-1,-1)).x;
    sum += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2( 1, 1)).x;
    sum += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2( 1,-1)).x;
    sum += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(-1, 1)).x;
    half avg = PerceptualToLin(sum / 4.0);
    return half4(avg, avg, avg, saturate(0.0125 * _AdaptationSpeed));
}

half3 apply_lut(sampler2D tex, half3 uv, half3 scaleOffset)
{
    uv.z *= scaleOffset.z;
    half shift = floor(uv.z);
    uv.xy = uv.xy * scaleOffset.z * scaleOffset.xy + 0.5 * scaleOffset.xy;
    uv.x += shift * scaleOffset.y;
    uv.xyz = lerp(tex2D(tex, uv.xy).rgb, tex2D(tex, uv.xy + half2(scaleOffset.y, 0)).rgb, uv.z - shift);
    return uv;
}

half4 frag_tcg(v2f_img i) : SV_Target
{
    half4 color = tex2D(_MainTex, i.uv);

#if GAMMA_COLORSPACE
    color.rgb = GammaToLinearSpace(color.rgb);
#endif

#if ENABLE_COLOR_GRADING
    // LUT color grading
    half3 color_corrected = apply_lut(_LutTex, saturate(color.rgb), _LutParams.xyz);
    color.rgb = lerp(color.rgb, color_corrected, _LutParams.w);
#endif

#if GAMMA_COLORSPACE
    color.rgb = LinearToGammaSpace(color.rgb);
#endif

    return color;
}
