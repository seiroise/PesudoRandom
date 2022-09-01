using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class NoiseVisualization : Visualization
{
	static int noiseId = Shader.PropertyToID("_Noise");

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
		handle.Complete();
		noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
	}
}
