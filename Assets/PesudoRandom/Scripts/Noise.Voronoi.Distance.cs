using Unity.Mathematics;

using static Unity.Mathematics.math;

public partial class Noise
{
	public interface IVoronoiDistance
	{
		float4 GetDistance(float4 x);
		float4 GetDistance(float4 x, float4 y);
		float4 GetDistance(float4 x, float4 y, float4 z);

		float4x2 Finalize1D(float4x2 minima);
		float4x2 Finalize2D(float4x2 minima);
		float4x2 Finalize3D(float4x2 minima);
	}

	/// <summary>
	/// ユークリッド距離メトリックに基づき[0,1]にクランプを行う
	/// </summary>
	public struct Worley : IVoronoiDistance
	{
		public float4 GetDistance(float4 x)
		{
			return abs(x);
		}

		public float4 GetDistance(float4 x, float4 y)
		{
			// 平方根の計算はFinalizeで行う。
			return x * x + y * y;
		}

		public float4 GetDistance(float4 x, float4 y, float4 z)
		{
			// 平方根の計算はFinalizeで行う。
			return x * x + y * y + z * z;
		}

		public float4x2 Finalize1D(float4x2 minima)
		{
			return minima;
		}

		public float4x2 Finalize2D(float4x2 minima)
		{
			minima.c0 = sqrt(min(minima.c0, 1f));
			minima.c1 = sqrt(min(minima.c1, 1f));
			return minima;
		}

		public float4x2 Finalize3D(float4x2 minima)
		{
			return Finalize2D(minima);
		}
	}

	/// <summary>
	/// それぞれの軸の最大値が距離になる。
	/// </summary>
	public struct Chebyshev : IVoronoiDistance
	{
		public float4 GetDistance(float4 x)
		{
			return abs(x);
		}

		public float4 GetDistance(float4 x, float4 y)
		{
			return max(abs(x), abs(y));
		}

		public float4 GetDistance(float4 x, float4 y, float4 z)
		{
			return max(max(abs(x), abs(y)), abs(z));
		}

		public float4x2 Finalize1D(float4x2 minima)
		{
			return minima;
		}

		public float4x2 Finalize2D(float4x2 minima)
		{
			return minima;
		}

		public float4x2 Finalize3D(float4x2 minima)
		{
			return minima;
		}
	}

	/// <summary>
	/// マンハッタン距離による評価
	/// </summary>
	public struct Manhattan : IVoronoiDistance
	{
		public float4 GetDistance(float4 x)
		{
			return abs(x);
		}

		public float4 GetDistance(float4 x, float4 y)
		{
			return abs(x) + abs(y);
		}

		public float4 GetDistance(float4 x, float4 y, float4 z)
		{
			return abs(x) + abs(y) + abs(z);
		}

		public float4x2 Finalize1D(float4x2 minima)
		{
			return minima;
		}

		public float4x2 Finalize2D(float4x2 minima)
		{
			return minima / 2f;
		}

		public float4x2 Finalize3D(float4x2 minima)
		{
			return minima / 3f;
		}
	}
}