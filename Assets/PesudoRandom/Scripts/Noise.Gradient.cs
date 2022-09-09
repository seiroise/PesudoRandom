using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public partial class Noise
{
	public interface IGradient
	{
		float4 Evaluate(SmallXXHash4 hash, float4 x);
		float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y);
		float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z);
		float4 EvaluateAfterInterpolation(float4 value);
	}

	/// <summary>
	/// 最終的な評価値を絶対値にして返す。
	/// Ken PerlinがこのパターンをTurbulence(乱流)と呼んだことに由来
	/// </summary>
	/// <typeparam name="G"></typeparam>
	public struct Turbulence<G> : IGradient where G : struct, IGradient
	{
		public float4 Evaluate(SmallXXHash4 hash, float4 x)
		{
			return default(G).Evaluate(hash, x);
		}

		public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y)
		{
			return default(G).Evaluate(hash, x, y);
		}

		public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z)
		{
			return default(G).Evaluate(hash, x, y, z);
		}

		public float4 EvaluateAfterInterpolation(float4 value)
		{
			return abs(default(G).EvaluateAfterInterpolation(value));
		}
	}

	/// <summary>
	/// 二乗を返す
	/// </summary>
	/// <typeparam name="G"></typeparam>
	public struct Power2<G> : IGradient where G : struct, IGradient
	{
		public float4 Evaluate(SmallXXHash4 hash, float4 x)
		{
			return default(G).Evaluate(hash, x);
		}

		public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y)
		{
			return default(G).Evaluate(hash, x, y);
		}

		public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z)
		{
			return default(G).Evaluate(hash, x, y, z);
		}

		public float4 EvaluateAfterInterpolation(float4 value)
		{
			float4 v = default(G).EvaluateAfterInterpolation(value);
			return v * v;
		}
	}

	/// <summary>
	/// 常に勾配が1の勾配ノイズ = value noise
	/// </summary>
	public struct Value : IGradient
	{
		public float4 Evaluate(SmallXXHash4 hash, float4 x)
		{
			// valueノイズの場合は値に関わらず常に定数を返す。
			return hash.Floats01A * 2f - 1f;
		}

		public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y)
		{
			return hash.Floats01A * 2f - 1f;
		}

		public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z)
		{
			return hash.Floats01A * 2f - 1f;
		}

		public float4 EvaluateAfterInterpolation(float4 value)
		{
			return value;
		}
	}

	/// <summary>
	/// 勾配が常に1ではない勾配ノイズ = perlin noise
	/// </summary>
	public struct Perlin : IGradient
	{
		public float4 Evaluate(SmallXXHash4 hash, float4 x)
		{
			return 2f * hash.Floats01A * select(-x, x, ((uint4)hash & (1 << 8)) == 0);
		}

		public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y)
		{
			// 原点を中心とした菱形を作成し、原点からその辺へのベクトルを勾配とする。
			float4 gx = hash.Floats01A * 2f - 1f;
			// yを-0.5 ~ 0.5の範囲に変換
			float4 gy = 0.5f - abs(gx);
			// -1 < gx < -0.5, 0.5 < gx < 1の範囲にあるものを正しい範囲に変形する。
			// ちょっとややこしい。gx = 0.6なら-0.1になり、gx = -0.6なら+0.1になる。
			gx -= floor(gx + 0.5f);
			return (gx * x + gy * y) * (2f / 0.53528f);
		}

		public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z)
		{
			float4 gx = hash.Floats01A * 2f - 1f, gy = hash.Floats01D * 2f - 1f;
			float4 gz = 1f - abs(gx) - abs(gy);
			float4 offset = max(-gz, 0f);
			gx += select(-offset, offset, gx < 0f);
			gy += select(-offset, offset, gy < 0f);
			return (gx * x + gy * y + gz * z) * (1f / 0.56290f);
		}

		public float4 EvaluateAfterInterpolation(float4 value)
		{
			return value;
		}
	}
}
