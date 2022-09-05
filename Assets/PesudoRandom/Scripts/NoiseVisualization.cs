using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Noise;

public class NoiseVisualization : Visualization
{
	static int noiseId = Shader.PropertyToID("_Noise");

	public enum NoiseType
	{
		Pattern1,
		Value,
		Perlin,
	}

	public enum NoiseDimensions
	{
		One = 1,
		Two = 2,
		Three = 3
	}

	public static ScheduleDelegate[,] noiseJobs =
	{
		{
			Job<Pattern1>.ScheduleParallel,
			Job<Pattern1>.ScheduleParallel,
			Job<Pattern1>.ScheduleParallel,
		},
		{
			Job<Lattice1D<Value>>.ScheduleParallel,
			Job<Lattice2D<Value>>.ScheduleParallel,
			Job<Lattice3D<Value>>.ScheduleParallel
		},
		{
			Job<Lattice1D<Perlin>>.ScheduleParallel,
			Job<Lattice2D<Perlin>>.ScheduleParallel,
			Job<Lattice3D<Perlin>>.ScheduleParallel
		}
	};

	[SerializeField]
	NoiseType noiseType;

	[SerializeField]
	NoiseDimensions dimensions;

	[SerializeField]
	int seed;

	[SerializeField]
	SpaceTRS domain = new SpaceTRS
	{
		scale = 8f
	};

	NativeArray<float4> noise;
	GraphicsBuffer noiseBuffer;

	protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
	{
		noise = new NativeArray<float4>(dataLength, Allocator.Persistent);
		noiseBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, dataLength * 4, 4);
		propertyBlock.SetBuffer(noiseId, noiseBuffer);
	}

	protected override void DisableVisualization()
	{
		noise.Dispose();
		noiseBuffer.Release();
		noiseBuffer = default;
	}

	protected override void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle)
	{
		noiseJobs[(int)noiseType, (int)dimensions - 1](positions, noise, seed, domain, resolution, handle).Complete();
		noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
	}
}
