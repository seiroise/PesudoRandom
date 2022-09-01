using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public static class Shapes
{

	/// <summary>
	/// 形状の列挙
	/// </summary>
	public enum Shape { Plane, Sphere, Torus, OctahedronSphere }

	/// <summary>
	/// それぞれの形状を出力するJobのデリゲート
	/// </summary>
	/// <param name="positions">出力座標配列</param>
	/// <param name="normals">出力法線配列</param>
	/// <param name="resolution">解像度</param>
	/// <param name="trs">座標変換行列</param>
	/// <param name="dependency">依存JobHandle</param>
	/// <returns></returns>
	public delegate JobHandle ScheduleDelegate(
		NativeArray<float3x4> positions, NativeArray<float3x4> normals,
		int resolution, float4x4 trs, JobHandle dependency
	);

	/// <summary>
	/// enum Shapeに対応したJob配列
	/// </summary>
	public static ScheduleDelegate[] shapeJobs =
	{
		Job4<Plane>.ScheduleParallel,
		Job4<Sphere>.ScheduleParallel,
		Job4<Torus>.ScheduleParallel,
		Job4<OctahedronSphere>.ScheduleParallel
	};

	/// <summary>
	/// ４つの座標と法線の構造体
	/// </summary>
	public struct Point4
	{
		public float4x3 positions, normals;
	}

	/// <summary>
	/// 形状計算用のインターフェイス
	/// </summary>
	public interface IShape
	{
		Point4 GetPoint4(int i, float resolution, float invResolution);
	}

	/// <summary>
	/// インデックスからuvを計算する
	/// </summary>
	/// <param name="i"></param>
	/// <param name="resolution"></param>
	/// <param name="invResolution"></param>
	/// <returns></returns>
	public static float4x2 IndexTo4UV(int i, float resolution, float invResolution)
	{
		float4 i4 = i * 4f + float4(0f, 1f, 2f, 3f);
		float4x2 uv;
		// -0.5,0.5の間の値に収める。
		uv.c1 = floor(i4 * invResolution + 0.00001f);
		uv.c0 = invResolution * (i4 - uv.c1 * resolution + 0.5f);
		uv.c1 = invResolution * (uv.c1 + 0.5f);
		return uv;
	}

	/// <summary>
	/// uvから平面の座標と砲戦を計算するJob
	/// </summary>
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	public struct Job : IJobFor
	{
		[WriteOnly]
		NativeArray<float3> positions, normals;

		public float resolution, invResolution;
		public float3x4 positionTRS;

		public void Execute(int i)
		{
			float2 uv;
			// -0.5,0.5の間の値に収める。
			uv.y = floor(i * invResolution + 0.00001f);
			uv.x = invResolution * (i - uv.y * resolution + 0.5f) - 0.5f;
			uv.y = invResolution * (uv.y + 0.5f) - 0.5f;

			// 座標変換
			positions[i] = mul(positionTRS, float4(uv.x, 0f, uv.y, 1f));
			normals[i] = mul(positionTRS, float4(0f, 1f, 0f, 1f));
		}

		public static JobHandle ScheduleParallel(
			NativeArray<float3> positions, NativeArray<float3> normals, int resolution, float4x4 trs, JobHandle dependency
		)
		{
			return new Job
			{
				positions = positions,
				normals = normals,
				resolution = resolution,
				invResolution = 1f / resolution,
				positionTRS = float3x4(trs.c0.xyz, trs.c1.xyz, trs.c2.xyz, trs.c3.xyz)
			}.ScheduleParallel(positions.Length, resolution, dependency);
		}
	}

	/// <summary>
	/// ベクトル化したJob
	/// </summary>
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	public struct Job4<S> : IJobFor where S : struct, IShape
	{
		[WriteOnly]
		NativeArray<float3x4> positions, normals;

		public float resolution, invResolution;
		public float3x4 positionTRS;
		public float3x4 normalTRS;

		public void Execute(int i)
		{
			Point4 p = default(S).GetPoint4(i, resolution, invResolution);

			// 座標変換
			positions[i] = transpose(positionTRS.TransformVectors(p.positions));
			float3x4 n = transpose(normalTRS.TransformVectors(p.normals, 0f));
			normals[i] = float3x4(normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3));
		}

		public static JobHandle ScheduleParallel(
			NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution, float4x4 trs, JobHandle dependency
		)
		{
			return new Job4<S>
			{
				positions = positions,
				normals = normals,
				resolution = resolution,
				invResolution = 1f / resolution,
				positionTRS = trs.Get3x4(),
				normalTRS = transpose(inverse(trs)).Get3x4()
			}.ScheduleParallel(positions.Length, resolution, dependency);
		}
	}

	/// <summary>
	/// (0,1,0)を法線とする平面
	/// </summary>
	public struct Plane : IShape
	{
		public Point4 GetPoint4(int i, float resolution, float invResolution)
		{
			float4x2 uv = IndexTo4UV(i, resolution, invResolution);
			return new Point4
			{
				positions = float4x3(uv.c0 - 0.5f, 0f, uv.c1 - 0.5f),
				normals = float4x3(0f, 1f, 0f)
			};
		}
	}

	/// <summary>
	/// UV球
	/// </summary>
	public struct Sphere : IShape
	{
		public Point4 GetPoint4(int i, float resolution, float invResolution)
		{
			// uvのuを緯度,vを軽度としたuv球にマッピングする。
			float4x2 uv = IndexTo4UV(i, resolution, invResolution);

			// 緯度
			float r = 0.5f;
			float4 s = r * sin(PI * uv.c1);

			Point4 p;
			p.positions.c0 = s * sin(2f * PI * uv.c0);
			p.positions.c1 = r * cos(PI * uv.c1);
			p.positions.c2 = s * cos(2f * PI * uv.c0);
			p.normals = p.positions;
			return p;
		}
	}

	/// <summary>
	/// 八面体球
	/// </summary>
	public struct OctahedronSphere : IShape
	{
		public Point4 GetPoint4(int i, float resolution, float invResolution)
		{
			float4x2 uv = IndexTo4UV(i, resolution, invResolution);

			Point4 p;
			// x
			p.positions.c0 = uv.c0 - 0.5f;
			// y
			p.positions.c1 = uv.c1 - 0.5f;
			// z
			// x,y = 0の地点がz = 0.5になるようにオフセットする
			p.positions.c2 = 0.5f - abs(p.positions.c0) - abs(p.positions.c1);
			// -z方向に折りたたむため、z=-0.5に近いほど、x,yをオフセットするような係数を求める。
			float4 offset = max(-p.positions.c2, 0f);
			p.positions.c0 += select(-offset, offset, p.positions.c0 < 0f);
			p.positions.c1 += select(-offset, offset, p.positions.c1 < 0f);
			float4 scale = 0.5f * rsqrt(p.positions.c0 * p.positions.c0 + p.positions.c1 * p.positions.c1 + p.positions.c2 * p.positions.c2);
			p.positions.c0 *= scale;
			p.positions.c1 *= scale;
			p.positions.c2 *= scale;
			// normal
			p.normals = p.positions;
			return p;
		}
	}

	/// <summary>
	/// UVトーラス
	/// </summary>
	public struct Torus : IShape
	{
		public Point4 GetPoint4(int i, float resolution, float invResolution)
		{
			// uvのuを大きな円の円周、vを小さな円の円周とする。
			float4x2 uv = IndexTo4UV(i, resolution, invResolution);

			float r1 = 0.375f;
			float r2 = 0.125f;
			float4 s = r1 + r2 * cos(2f * PI * uv.c1);

			Point4 p;
			// x
			p.positions.c0 = s * sin(2f * PI * uv.c0);
			// y
			p.positions.c1 = r2 * sin(2f * PI * uv.c1);
			// z
			p.positions.c2 = s * cos(2f * PI * uv.c0);
			p.normals = p.positions;
			p.normals.c0 -= r1 * sin(2f * PI * uv.c0);
			p.normals.c2 -= r1 * cos(2f * PI * uv.c0);
			return p;
		}
	}
}