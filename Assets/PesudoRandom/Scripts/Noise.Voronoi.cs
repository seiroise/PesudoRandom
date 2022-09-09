using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public partial class Noise
{
	/// <summary>
	/// 近傍点までの距離を求める。
	/// </summary>
	/// <param name="minima">記録している最小値</param>
	/// <param name="distances">比較する距離</param>
	/// <returns></returns>
	static float4 UpdateVoronoiMinima(float4 minima, float4 distances)
	{
		return select(minima, distances, distances < minima);
	}

	/// <summary>
	/// 近傍点までの距離を求める。
	/// </summary>
	/// <param name="minima">入力距離、一列目(c0)が最近傍点までの距離、２列目c1が二番目に近い点までの距離</param>
	/// <param name="distances">比較する距離</param>
	/// <returns></returns>
	static float4x2 UpdateVoronoiMinima(float4x2 minima, float4 distances)
	{
		bool4 newMinimum = distances < minima.c0;
		minima.c1 = select(select(minima.c1, distances, distances < minima.c1), minima.c0, newMinimum);
		minima.c0 = select(minima.c0, distances, newMinimum);

		return minima;
	}

	/// <summary>
	/// 1次元Voronoiノイズ
	/// </summary>
	/// <typeparam name="L"></typeparam>
	public struct Voronoi1D<L, D, F> : INoise
		where L : struct, ILattice
		where D : struct, IVoronoiDistance
		where F : struct, IVoronoiFunction
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			var d = default(D);
			var x = l.GetLatticeSpan4(positions.c0, frequency);

			float4x2 minima = 2f;
			for(int u = -1; u <= 1; u++)
			{
				SmallXXHash4 h = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
				minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01A + u - x.g0));
			}
			return default(F).Evaluate(d.Finalize1D(minima));
		}
	}

	/// <summary>
	/// 2次元(XZ平面)Voronoiノイズ
	/// </summary>
	/// <typeparam name="L"></typeparam>
	public struct Voronoi2D<L, D, F> : INoise
		where L : struct, ILattice
		where D : struct, IVoronoiDistance
		where F : struct, IVoronoiFunction
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			var d = default(D);
			var x = l.GetLatticeSpan4(positions.c0, frequency);
			var z = l.GetLatticeSpan4(positions.c2, frequency);

			float4x2 minima = 2f;
			for(int u = -1; u <= 1; u++)
			{
				SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
				float4 xOffset = u - x.g0;
				for(int v = -1; v <= 1; v++)
				{
					SmallXXHash4 h = hx.Eat(l.ValidateSingleStep(z.p0 + v, frequency));
					float4 zOffset = v - z.g0;
					minima = UpdateVoronoiMinima(
						minima,
						d.GetDistance(h.Floats01A + xOffset, h.Floats01B + zOffset)
					);
					minima = UpdateVoronoiMinima(
						minima,
						d.GetDistance(h.Floats01C + xOffset, h.Floats01D + zOffset)
					);
				}
			}
			return default(F).Evaluate(d.Finalize2D(minima));
		}
	}

	/// <summary>
	/// 3次元Voronoiノイズ
	/// </summary>
	/// <typeparam name="L"></typeparam>
	public struct Voronoi3D<L, D, F> : INoise
		where L : struct, ILattice
		where D : struct, IVoronoiDistance
		where F : struct, IVoronoiFunction
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			var d = default(D);
			var x = l.GetLatticeSpan4(positions.c0, frequency);
			var y = l.GetLatticeSpan4(positions.c1, frequency);
			var z = l.GetLatticeSpan4(positions.c2, frequency);

			// 27近傍のスパンを調べ、最近傍点を調べる
			float4x2 minima = 2f;
			for(int u = -1; u <= 1; u++)
			{
				SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
				float4 xOffset = u - x.g0;
				for(int v = -1; v <= 1; v++)
				{
					SmallXXHash4 hy = hx.Eat(l.ValidateSingleStep(y.p0 + v, frequency));
					float4 yOffset = v - y.g0;
					for(int w = -1; w <= 1; w++)
					{
						SmallXXHash4 h = hy.Eat(l.ValidateSingleStep(z.p0 + w, frequency));
						float4 zOffset = w - z.g0;
						minima = UpdateVoronoiMinima(
							minima,
							d.GetDistance(
								h.GetBitsAsFloats01(5, 0) + xOffset,
								h.GetBitsAsFloats01(5, 5) + yOffset,
								h.GetBitsAsFloats01(5, 10) + zOffset)
						);
						minima = UpdateVoronoiMinima(
							minima,
							d.GetDistance(
								h.GetBitsAsFloats01(5, 15) + xOffset,
								h.GetBitsAsFloats01(5, 20) + yOffset,
								h.GetBitsAsFloats01(5, 25) + zOffset)
						);
					}
				}
			}
			return default(F).Evaluate(d.Finalize3D(minima));
		}
	}
}
