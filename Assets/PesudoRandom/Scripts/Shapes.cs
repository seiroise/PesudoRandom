using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public static class Shapes
{

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
	public static float4x2 IndexToU4UV(int i, float resolution, float invResolution)
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
		) {
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

		float4x3 TransformVectors(float3x4 trs, float4x3 p, float w = 1f) => float4x3(
			trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x * w,
			trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y * w,
			trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z * w
		);

		public void Execute(int i)
		{	
			Point4 p = default(S).GetPoint4(i, resolution, invResolution);

			// 座標変換
			positions[i] = transpose(TransformVectors(positionTRS, p.positions));
			float3x4 n = transpose(TransformVectors(positionTRS, p.normals, 0f));
			normals[i] = float3x4(normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3));
		}

		public static JobHandle ScheduleParallel(
			NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution, float4x4 trs, JobHandle dependency
		) {
			return new Job4<S>
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
	/// (0,1,0)を法線とする平面
	/// </summary>
	public struct Plane : IShape
	{
		public Point4 GetPoint4 (int i, float resolution, float invResolution)
		{
			float4x2 uv = IndexToU4UV(i, resolution, invResolution);
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
			float4x2 uv = IndexToU4UV(i, resolution, invResolution);

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
	/// UVトーラス
	/// </summary>
	public struct Torus : IShape
	{
		public Point4 GetPoint4(int i, float resolution, float invResolution)
		{
			// uvのuを大きな円の円周、vを小さな円の円周とする。
			float4x2 uv = IndexToU4UV(i, resolution, invResolution);

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