using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace dotNES
{
	public static class Shaders
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct Parameters
		{
			public Vector4 Diffuse;
			public Matrix WorldViewProjection;
		}

		public static class Position
		{
			private static VertexShader vertexShader;
			private static PixelShader pixelShader;
			private static InputLayout layout;

			private static string source = @"
cbuffer data :register(b0)
{
	float4 diffuse;
	float4x4 worldViewProjection;
};

Texture2D textureMap;
SamplerState textureSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

struct VS_IN
{
	float4 position : POSITION;
	float4 normal : NORMAL;
	float2 texCoord : TEXCOORD;
};

struct PS_IN
{
	float4 position : SV_POSITION;
	float4 normal : NORMAL;
	float2 texCoord : TEXCOORD;
};

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.position = mul(worldViewProjection,input.position);

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	float4 output = diffuse;

	return output;
}
";

			public static void Load(Device device)
			{
				var vertexShaderByteCode = ShaderBytecode.Compile(source, "VS", "vs_5_0");
				vertexShader = new VertexShader(device, vertexShaderByteCode);
				pixelShader = new PixelShader(device, ShaderBytecode.Compile(source, "PS", "ps_5_0"));

				layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new InputElement[]
				{
					new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0, 0),
					new InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 12, 0),
					new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 24, 0)
				});
			}

			public static void Apply(DeviceContext context)
			{
				context.InputAssembler.InputLayout = layout;

				context.VertexShader.Set(vertexShader);
				context.PixelShader.Set(pixelShader);
				context.GeometryShader.Set(null);
				context.DomainShader.Set(null);
				context.HullShader.Set(null);
			}
		}

		public static class NormalTexture
		{
			private static VertexShader vertexShader;
			private static PixelShader pixelShader;
			private static InputLayout layout;

			private static string source = @"
cbuffer data : register(b0)
{
	float4 diffuse;
	float4x4 worldViewProjection;
};

Texture2D textureMap;
SamplerState textureSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

struct VS_IN
{
	float4 position : POSITION;
	float4 normal : NORMAL;
	float2 texCoord : TEXCOORD;
};

struct PS_IN
{
	float4 position : SV_POSITION;
	float4 normal : NORMAL;
	float2 texCoord : TEXCOORD;
};

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.position = mul(worldViewProjection,input.position);

	output.normal.xyz = mul((float3x3)worldViewProjection, input.normal.xyz);
	output.normal.w = 1;

	output.texCoord = input.texCoord;

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	float4 output = diffuse * textureMap.Sample(textureSampler, input.texCoord);

	float3 light = { 5, 9, 1 };

	light = normalize(light);

	output.rgb += dot(light, input.normal.xyz) * 0.2;
	
	return output;
}
";

			public static void Load(Device device)
			{
				var vertexShaderByteCode = ShaderBytecode.Compile(source, "VS", "vs_5_0");
				vertexShader = new VertexShader(device, vertexShaderByteCode);
				pixelShader = new PixelShader(device, ShaderBytecode.Compile(source, "PS", "ps_5_0"));

				layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new InputElement[]
				{
					new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0, 0),
					new InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 12, 0),
					new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 24, 0)
				});
			}

			public static void Apply(DeviceContext context)
			{
				context.InputAssembler.InputLayout = layout;

				context.VertexShader.Set(vertexShader);
				context.PixelShader.Set(pixelShader);
				context.GeometryShader.Set(null);
				context.DomainShader.Set(null);
				context.HullShader.Set(null);
			}
		}

		public static class Normal
		{
			private static VertexShader vertexShader;
			private static PixelShader pixelShader;
			private static InputLayout layout;

			private static string source = @"
cbuffer data : register(b0)
{
	float4 diffuse;
	float4x4 worldViewProjection;
};

struct VS_IN
{
	float4 position : POSITION;
	float4 normal : NORMAL;
	float2 texCoord : TEXCOORD;
};

struct PS_IN
{
	float4 position : SV_POSITION;
	float4 normal : NORMAL;
	float2 texCoord : TEXCOORD;
};

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.position = mul(worldViewProjection,input.position);

	output.normal.xyz = mul((float3x3)worldViewProjection, input.normal.xyz);
	output.normal.w = 1;

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	float4 output = diffuse;

	float3 light = { 5, 9, 1 };

	light = normalize(light);

	output.rgb *= (dot(light, input.normal.xyz) * 0.5) + 0.5;
	
	return output;
}
";

			public static void Load(Device device)
			{
				var vertexShaderByteCode = ShaderBytecode.Compile(source, "VS", "vs_5_0");
				vertexShader = new VertexShader(device, vertexShaderByteCode);
				pixelShader = new PixelShader(device, ShaderBytecode.Compile(source, "PS", "ps_5_0"));

				layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new InputElement[]
				{
					new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0, 0),
					new InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 12, 0),
					new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 24, 0)
				});
			}

			public static void Apply(DeviceContext context)
			{
				context.InputAssembler.InputLayout = layout;

				context.VertexShader.Set(vertexShader);
				context.PixelShader.Set(pixelShader);
				context.GeometryShader.Set(null);
				context.DomainShader.Set(null);
				context.HullShader.Set(null);
			}
		}
	}
}
