cbuffer cbPerObject {
	float4x4 gWorldViewProj;
}

struct VertexIn {
	float3 PosL : POSITION;
	float4 Color : COLOR;
};

struct VertexOut {
	float4 PosH : SV_POSITION;
	float4 Color : COLOR;
};

VertexOut VShader(VertexIn vin)
{
	VertexOut vout;
	vout.PosH = mul(float4(vin.PosL, 1.0f), gWorldViewProj);
	vout.Color = vin.Color;
	
	return vout;
}

float4 PShader(VertexOut pin) : SV_Target
{
	return pin.Color;
}

technique11 ColorTech {
	pass P0{
		SetVertexShader( CompileShader( vs_4_0, VShader()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader( ps_4_0, PShader()));
	}
}