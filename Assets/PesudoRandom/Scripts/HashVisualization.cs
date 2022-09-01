using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public class HashVisualization : Visualization
{
	/// <summary>
	/// /ハッシュ計算Job
	/// </summary>
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	struct HashJob : IJobFor
	{
		[WriteOnly]
		public NativeArray<uint> hashes;
		[ReadOnly]
		public NativeArray<float3> positions;

		public SmallXXHash hash;
		public float3x4 domainTRS;

		public void Execute(int i)
		{
			float3 p = mul(domainTRS, float4(positions[i], 1));

			int u = (int)floor(p.x);
			int v = (int)floor(p.y);
			int w = (int)floor(p.z);

			hashes[i] = hash.Eat(u).Eat(v).Eat(w);
		}
	}

	/// <summary>
	/// HashJobのベクトル化対応版
	/// </summary>
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	struct HashJob4 : IJobFor
	{
		[WriteOnly]
		public NativeArray<uint4> hashes;
		[ReadOnly]
		public NativeArray<float3x4> positions;

		public SmallXXHash4 hash;
		public float3x4 domainTRS;

		public void Execute(int i)
		{
			// x,y,zそれぞれの要素ごとにベクトル化するので、行列を転置して
			// それぞれの0列目にx, 1列目にy, 2列目にzが来るようにする。
			float4x3 p = domainTRS.TransformVectors(transpose(positions[i]));

			int4 u = (int4)floor(p.c0);
			int4 v = (int4)floor(p.c1);
			int4 w = (int4)floor(p.c2);

			hashes[i] = hash.Eat(u).Eat(v).Eat(w);
		}
	}

	static int hashesId = Shader.PropertyToID("_Hashes");

	[SerializeField]
	int seed;

	[SerializeField]
	SpaceTRS domain = new SpaceTRS
	{
		scale = 8f
	};

	NativeArray<uint4> hashes;
	GraphicsBuffer hashesBuffer;

	protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
	{
		hashes = new NativeArray<uint4>(dataLength, Allocator.Persistent);
		hashesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, dataLength * 4, 4);
		propertyBlock.SetBuffer(hashesId, hashesBuffer);
	}

	protected override void DisableVisualization()
	{
		hashes.Dispose();
		hashesBuffer.Release();
		hashesBuffer = default;
	}

	protected override void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle)
	{
		new HashJob4
		{
			positions = positions,
			hashes = hashes,
			hash = SmallXXHash4.Seed(seed),
			domainTRS = domain.Matrix
		}.ScheduleParallel(hashes.Length, resolution, handle).Complete();
		hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
	}
}
