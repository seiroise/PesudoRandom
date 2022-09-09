using Unity.Mathematics;

using static Unity.Mathematics.math;

public partial class Noise
{

	/// <summary>
	/// いわゆるVoronoi関数のためのインタフェース
	/// </summary>
	public interface IVoronoiFunction
	{
		float4 Evaluate(float4x2 minima);
	}

	/// <summary>
	/// 最近傍点までの距離を返すVoronoi関数
	/// </summary>
	public struct F1 : IVoronoiFunction
	{
		public float4 Evaluate(float4x2 minima)
		{
			return minima.c0;
		}
	}

	/// <summary>
	/// 二番目に近い点までの距離を返すVoronoi関数
	/// </summary>
	public struct F2 : IVoronoiFunction
	{
		public float4 Evaluate(float4x2 minima)
		{
			return minima.c1;
		}
	}

	/// <summary>
	/// 二番目に近い点から一番目に近い点の距離を引いたものを返すVoronoi関数
	/// </summary>
	public struct F2MinusF1 : IVoronoiFunction
	{
		public float4 Evaluate(float4x2 minima)
		{
			return minima.c1 - minima.c0;
		}
	}
}