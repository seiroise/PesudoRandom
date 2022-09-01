using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public static partial class Noise
{
	public interface INoise
	{
		float4 GetNoise4(float4x3 positions, SmallXXHash4 hash);
	}

	/// <summary>
	/// それぞれのノイズを出力するJobのデリゲート
	/// </summary>
	/// <param name="positions"></param>
	/// <param name="noise"></param>
	/// <param name="seed"></param>
	/// <param name="trs"></param>
	/// <param name="resolution"></param>
	/// <param name="dependency"></param>
	/// <returns></returns>
	public delegate JobHandle ScheduleDelegate(
		NativeArray<float3x4> positions, NativeArray<float4> noise, int seed, SpaceTRS trs, int resolution, JobHandle dependency
	);

	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	public struct Job<N> : IJobFor where N : struct, INoise
	{
		[ReadOnly]
		public NativeArray<float3x4> positions;

		[WriteOnly]
		public NativeArray<float4> noise;

		public SmallXXHash4 hash;
		public float3x4 domainTRS;

		public void Execute(int i)
		{
			// 座標はxyzxyzxyzxyz...として入力されるのでfloat3x4の形式になっていることに注意
			// ベクトル計算するためにはxxxxyyyyzzzzとする必要があるのでtransposeして4x3に変更している。
			noise[i] = default(N).GetNoise4(domainTRS.TransformVectors(transpose(positions[i])), hash);
		}

		public static JobHandle ScheduleParallel(
			NativeArray<float3x4> positions, NativeArray<float4> noise, int seed, SpaceTRS trs, int resolution, JobHandle dependency
		)
		{
			return new Job<N>
			{
				positions = positions,
				noise = noise,
				hash = SmallXXHash4.Seed(seed),
				domainTRS = trs.Matrix
			}.ScheduleParallel(positions.Length, resolution, dependency);
		}
	}
}
