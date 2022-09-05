using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public static partial class Noise
{
	/// <summary>
	/// ノイズのパターン1
	/// </summary>
	public struct Pattern1 : INoise
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
		{
			// 本当はp0としないといけないところをp0.xとすることで、市松模様のような面白いパターンが見られる。
			int4 p0 = (int4)floor(positions.c0);
			return hash.Eat(p0.x).Floats01A * 2f - 1f;
		}
	}

	/// <summary>
	/// 1次元の格子パターン
	/// </summary>
	public struct Lattice1D<G> : INoise where G : struct, IGradient
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
		{
			LatticeSpan4 x = GetLatticeSpan4(positions.c0);
			var g = default(G);

			// 勾配をそのまま出力
			// return g.Evaluate(hash.Eat(x.p0), x.g0);
			// return g.Evaluate(hash.Eat(x.p1), x.g1);

			return lerp(
				g.Evaluate(hash.Eat(x.p0), x.g0),
				g.Evaluate(hash.Eat(x.p1), x.g1),
				x.t);
			// float4 v = lerp(hash.Eat(x.p0).Floats01A, hash.Eat(x.p1).Floats01A, x.t) * 2f - 1f;
			// return v;
		}
	}

	/// <summary>
	/// 2次元の格子パターン
	/// </summary>
	public struct Lattice2D<G> : INoise where G : struct, IGradient
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
		{
			LatticeSpan4 x = GetLatticeSpan4(positions.c0);
			LatticeSpan4 z = GetLatticeSpan4(positions.c2);
			SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);

			var g = default(G);
			float4 v = lerp(
				lerp(
					g.Evaluate(h0.Eat(z.p0), x.g0, z.g0),
					g.Evaluate(h0.Eat(z.p1), x.g0, z.g1),
					z.t),
				lerp(
					g.Evaluate(h1.Eat(z.p0), x.g1, z.g0),
					g.Evaluate(h1.Eat(z.p1), x.g1, z.g1),
					z.t),
				x.t
			);
			return v;
		}
	}

	public struct Lattice3D<G> : INoise where G : struct, IGradient
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
		{
			LatticeSpan4 x = GetLatticeSpan4(positions.c0);
			LatticeSpan4 y = GetLatticeSpan4(positions.c1);
			LatticeSpan4 z = GetLatticeSpan4(positions.c2);

			SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);
			SmallXXHash4
				h00 = h0.Eat(y.p0), h01 = h0.Eat(y.p1),
				h10 = h1.Eat(y.p0), h11 = h1.Eat(y.p1);

			var g = default(G);
			float4 v =
				lerp(   // X軸
					lerp(   // Y軸
						lerp(   // Z軸
							g.Evaluate(h00.Eat(z.p0), x.g0, y.g0, z.g0),
							g.Evaluate(h00.Eat(z.p1), x.g0, y.g0, z.g1),
							z.t),
						lerp(   // Z軸
							g.Evaluate(h01.Eat(z.p0), x.g0, y.g1, z.g0),
							g.Evaluate(h01.Eat(z.p1), x.g0, y.g1, z.g1),
							z.t),
						y.t
					),
					lerp(
						lerp(
							g.Evaluate(h10.Eat(z.p0), x.g1, y.g0, z.g0),
							g.Evaluate(h10.Eat(z.p1), x.g1, y.g0, z.g1),
							z.t),
						lerp(
							g.Evaluate(h11.Eat(z.p0), x.g1, y.g1, z.g0),
							g.Evaluate(h11.Eat(z.p1), x.g1, y.g1, z.g1),
							z.t),
						y.t
					),
					x.t
				);
			return v;
		}
	}

	/// <summary>
	/// ４つの要素による２点のハッシュ値とその間の補間値
	/// </summary>
	struct LatticeSpan4
	{
		public int4 p0, p1;
		public float4 g0, g1;
		public float4 t;
	}

	static LatticeSpan4 GetLatticeSpan4(float4 coordinates)
	{
		float4 points = floor(coordinates);
		LatticeSpan4 span;
		span.p0 = (int4)points;
		span.p1 = span.p0 + 1;
		span.g0 = coordinates - span.p0;
		span.g1 = span.g0 - 1f;
		span.t = coordinates - points;

		// 三次補間(smoothstep)
		// span.t = span.t * span.t * (span.t * -2f + 3f);
		// 補間には5次関数を利用する。
		span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
		return span;
	}
}
