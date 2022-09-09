using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public static partial class Noise
{
	/// <summary>
	/// ４つの要素による２点のハッシュ値とその間の補間値
	/// </summary>
	public struct LatticeSpan4
	{
		public int4 p0, p1;
		public float4 g0, g1;
		public float4 t;
	}

	/// <summary>
	/// 格子インタフェース
	/// </summary>
	public interface ILattice
	{
		public LatticeSpan4 GetLatticeSpan4(float4 coordinate, int frequency);

		public int4 ValidateSingleStep(int4 points, int frequency);
	}

	/// <summary>
	/// 通常の格子
	/// </summary>
	public struct LatticeNormal : ILattice
	{
		public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
		{
			coordinates *= frequency;
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

		public int4 ValidateSingleStep(int4 points, int frequency)
		{
			// タイリングしていないので、そのままの値を返す
			return points;
		}
	}

	/// <summary>
	/// 繰り返しのある格子
	/// </summary>
	public struct LatticeTiling : ILattice
	{
		public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
		{
			coordinates *= frequency;
			float4 points = floor(coordinates);
			LatticeSpan4 span;
			span.p0 = (int4)points;
			// span.p1 = span.p0 + 1;
			span.g0 = coordinates - span.p0;
			span.g1 = span.g0 - 1f;
			span.t = coordinates - points;

			// 整数剰余の計算はベクトル化されないので、なるべくベクトル化される、除算と乗算で対応する。
			// frequencyでの除算(実際にはfrequencyの逆数による乗算)は精度の問題があるので事前にceilをしてintにしておく。
			span.p0 -= (int4)ceil(points / frequency) * frequency;
			span.p0 = select(span.p0, span.p0 + frequency, span.p0 < 0);
			// span.p0は常に整数値のみを取るので、span.p0 + 1 == frequencyならばp1を0にする。
			// 剰余算はベクトル化出来ないので。
			span.p1 = span.p0 + 1;
			span.p1 = select(span.p1, 0, span.p1 == frequency);

			// 三次補間(smoothstep)
			// span.t = span.t * span.t * (span.t * -2f + 3f);
			// 補間には5次関数を利用する。
			span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
			return span;
		}

		public int4 ValidateSingleStep(int4 points, int frequency)
		{
			return select(select(points, 0, points == frequency), frequency - 1, points == -1);
		}
	}

	/// <summary>
	/// 一次元格子ノイズの面白かったパターンその１
	/// </summary>
	public struct Pattern1<L> : INoise where L : struct, ILattice
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			// 本当はp0としないといけないところをp0.xとすることで、市松模様のような面白いパターンが見られる。
			var x = default(L).GetLatticeSpan4(positions.c0, frequency);
			return hash.Eat(x.p0).Floats01A * 2f - 1f;
		}
	}

	/// <summary>
	/// 格子確認用
	/// </summary>
	/// <typeparam name="L"></typeparam>
	public struct GridViewer1D<L> : INoise where L : struct, ILattice
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			var x = l.GetLatticeSpan4(positions.c0, frequency);
			return select(-1f, 1f, x.g0 < 0.02f);
		}
	}

	/// <summary>
	/// 格子確認用
	/// </summary>
	/// <typeparam name="L"></typeparam>
	public struct GridViewer2D<L> : INoise where L : struct, ILattice
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			var x = l.GetLatticeSpan4(positions.c0, frequency);
			var z = l.GetLatticeSpan4(positions.c2, frequency);
			return select(-1f, 1f, x.g0 < 0.02f | z.g0 < 0.02f);
		}
	}

	/// <summary>
	/// 格子確認用
	/// </summary>
	/// <typeparam name="L"></typeparam>
	public struct GridViewer3D<L> : INoise where L : struct, ILattice
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			var x = l.GetLatticeSpan4(positions.c0, frequency);
			var y = l.GetLatticeSpan4(positions.c1, frequency);
			var z = l.GetLatticeSpan4(positions.c2, frequency);
			return select(-1f, 1f, x.g0 < 0.02f | y.g0 < 0.02f | z.g0 < 0.02f);
		}
	}

	/// <summary>
	/// 1次元の格子ノイズ
	/// </summary>
	public struct Lattice1D<L, G> : INoise
		where L : struct, ILattice
		where G : struct, IGradient
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			LatticeSpan4 x = default(L).GetLatticeSpan4(positions.c0, frequency);
			var g = default(G);

			float4 v = lerp(
				g.Evaluate(hash.Eat(x.p0), x.g0),
				g.Evaluate(hash.Eat(x.p1), x.g1),
				x.t);
			return g.EvaluateAfterInterpolation(v);
		}
	}

	/// <summary>
	/// 2次元の格子ノイズ
	/// </summary>
	public struct Lattice2D<L, G> : INoise
		where L : struct, ILattice
		where G : struct, IGradient
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency);
			LatticeSpan4 z = l.GetLatticeSpan4(positions.c2, frequency);
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
			return g.EvaluateAfterInterpolation(v);
		}
	}

	/// <summary>
	/// 3次元の格子ノイズ
	/// </summary>
	/// <typeparam name="L"></typeparam>
	/// <typeparam name="G"></typeparam>
	public struct Lattice3D<L, G> : INoise
		where L : struct, ILattice
		where G : struct, IGradient
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency);
			LatticeSpan4 y = l.GetLatticeSpan4(positions.c1, frequency);
			LatticeSpan4 z = l.GetLatticeSpan4(positions.c2, frequency);

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
			return g.EvaluateAfterInterpolation(v);
		}
	}
}
