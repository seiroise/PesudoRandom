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
		Lattice1D,
		Lattice2D,
		Lattice3d,
	}

	public static ScheduleDelegate[] noiseJobs =
	{
		Job<Pattern1>.ScheduleParallel,
		Job<Lattice1D>.ScheduleParallel,
		Job<Lattice2D>.ScheduleParallel,
		Job<Lattice3D>.ScheduleParallel
	};

	[SerializeField]
	NoiseType noiseType;

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
		noiseJobs[(int)noiseType](positions, noise, seed, domain, resolution, handle).Complete();
		noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
	}
}
