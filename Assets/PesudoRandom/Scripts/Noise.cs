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
	[System.Serializable]
	public struct Settings
	{
		public int seed;
		[Min(1)]
		public int frequency;
		[Range(1, 6)]
		public int octaves;
		[Range(2, 4)]
		public int lacunarity;
		[Range(0f, 1f)]
		public float persistence;

		public static Settings Default => new Settings
		{
			frequency = 4,
			octaves = 1,
			lacunarity = 2,
			persistence = 0.5f
		};
	}

	public interface INoise
	{
		float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency);
	}

	/// <summary>
	/// それぞれのノイズを出力するJobのデリゲート
	/// </summary>
	/// <param name="positions"></param>
	/// <param name="noise"></param>
	/// <param name="settings"></param>
	/// <param name="trs"></param>
	/// <param name="resolution"></param>
	/// <param name="dependency"></param>
	/// <returns></returns>
	public delegate JobHandle ScheduleDelegate(
		NativeArray<float3x4> positions, NativeArray<float4> noise, Settings settings, SpaceTRS trs, int resolution, JobHandle dependency
	);

	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	public struct Job<N> : IJobFor where N : struct, INoise
	{
		[ReadOnly]
		public NativeArray<float3x4> positions;

		[WriteOnly]
		public NativeArray<float4> noise;

		public Settings settings;
		public float3x4 domainTRS;

		public void Execute(int i)
		{
			// 座標はxyzxyzxyzxyz...として入力されるのでfloat3x4の形式になっていることに注意
			// ベクトル計算するためにはxxxxyyyyzzzzとする必要があるのでtransposeして4x3に変更している。
			float4x3 position = domainTRS.TransformVectors(transpose(positions[i]));
			var hash = SmallXXHash4.Seed(settings.seed);
			int frequency = settings.frequency;
			float amplitude = 1f;
			float amplitudeSum = 0f;
			float4 sum = 0f;

			for(int o = 0; o < settings.octaves; o++)
			{
				sum += amplitude * default(N).GetNoise4(position, hash + o, frequency);
				amplitudeSum += amplitude;
				frequency *= settings.lacunarity;
				amplitude *= settings.persistence;
			}
			noise[i] = sum / amplitudeSum;
		}

		public static JobHandle ScheduleParallel(
			NativeArray<float3x4> positions, NativeArray<float4> noise, Settings settings, SpaceTRS trs, int resolution, JobHandle dependency
		)
		{
			return new Job<N>
			{
				positions = positions,
				noise = noise,
				settings = settings,
				domainTRS = trs.Matrix
			}.ScheduleParallel(positions.Length, resolution, dependency);
		}
	}
}
