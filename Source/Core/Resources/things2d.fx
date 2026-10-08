// Things 2D rendering shader
// Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com

// Vertex input data
struct VertexData
{
    float3 pos		: POSITION;
    float4 color	: COLOR0;
    float2 uv		: TEXCOORD0;
};

// Pixel input data
struct PixelData
{
    float4 pos		: POSITION;
    float4 color	: COLOR0;
    float2 uv		: TEXCOORD0;
};

// Render settings
// w = transparency
float4 rendersettings;

// Transform settings
float4x4 transformsettings;

// Filter settings (point or linear, set from the program)
dword filtersettings;

// Texture1 input
texture texture1
<
    string UIName = "Texture1";
    string ResourceType = "2D";
>;

// Texture sampler settings for sprite rendering
sampler2D texture1sprite = sampler_state
{
	Texture = <texture1>;
	MagFilter = filtersettings;
	MinFilter = filtersettings;
	MipFilter = filtersettings;
	AddressU = Clamp;
	AddressV = Clamp;
	MipMapLodBias = 0.0f;
};

// Transformation
PixelData vs_transform(VertexData vd)
{
	PixelData pd = (PixelData)0;
	pd.pos = mul(float4(vd.pos, 1.0f), transformsettings);
	pd.color = vd.color;
	pd.uv = vd.uv;
	return pd;
}

// Pixel shader for sprite drawing
float4 ps_sprite(PixelData pd) : COLOR
{
	// Take this pixel's color
	float4 c = tex2D(texture1sprite, pd.uv);
	
	// Modulate it by selection color
	if(pd.color.a > 0)
	{
		return float4((c.r + pd.color.r) / 2.0f, (c.g + pd.color.g) / 2.0f, (c.b + pd.color.b) / 2.0f, c.a * rendersettings.w * pd.color.a);
	}

	// Or leave it as it is
	return float4(c.rgb, c.a * rendersettings.w);
}

// Pixel shader for sprite drawing, multiplied by the vertex color (used for Nightmare things)
float4 ps_sprite_tint(PixelData pd) : COLOR
{
	float4 c = tex2D(texture1sprite, pd.uv);
	return float4(c.rgb * pd.color.rgb, c.a * rendersettings.w);
}

// Pixel shader for solid colored shapes (used for the thing boxes and direction arrows)
float4 ps_solid(PixelData pd) : COLOR
{
	return float4(pd.color.rgb, pd.color.a * rendersettings.w);
}

// Technique for shader model 2.0
technique SM20
{
	pass p0
	{
	    VertexShader = compile vs_2_0 vs_transform();
	    PixelShader = compile ps_2_0 ps_sprite();
	}

	pass p1
	{
	    VertexShader = compile vs_2_0 vs_transform();
	    PixelShader = compile ps_2_0 ps_sprite_tint();
	}

	pass p2
	{
	    VertexShader = compile vs_2_0 vs_transform();
	    PixelShader = compile ps_2_0 ps_solid();
	}
}
