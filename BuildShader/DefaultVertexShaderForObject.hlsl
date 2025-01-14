////////////////////////////////////////////////////////////////////////////////////////////////////
//
// オブジェクト用頂点シェーダー
//
////////////////////////////////////////////////////////////////////////////////////////////////////

#include "VS_INPUT.hlsli"
#include "DefaultVS_OUTPUT.hlsli"
#include "GlobalParameters.hlsli"

VS_OUTPUT main(VS_INPUT input)
{
    VS_OUTPUT Out = (VS_OUTPUT) 0;

	// 頂点法線
    Out.Normal = normalize(mul(input.Normal, (float3x3) g_WorldMatrix));

	// 位置
    Out.Position = mul(input.Position, g_WorldMatrix); // ワールド変換

	// カメラとの相対位置
    Out.Eye = (g_CameraPosition - mul(input.Position, g_WorldMatrix)).xyz;

	// ディフューズ色計算
    Out.Color.rgb = g_DiffuseColor.rgb;
    Out.Color.a = g_DiffuseColor.a;
    Out.Color = saturate(Out.Color); // 0〜1 に丸める

    Out.Tex = input.Tex;

    if (g_UseSphereMap)
    {
		// スフィアマップテクスチャ座標
        float2 NormalWV = mul(float4(Out.Normal, 0), g_ViewMatrix).xy;
        Out.SpTex.x = NormalWV.x * 0.5f + 0.5f;
        Out.SpTex.y = NormalWV.y * -0.5f + 0.5f;
    }
    else
    {
        Out.SpTex.x = 0.0f;
        Out.SpTex.y = 0.0f;
    }

    return Out;
}
