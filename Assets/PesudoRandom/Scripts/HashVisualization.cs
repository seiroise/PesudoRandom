using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public class HashVisualization : MonoBehaviour
{
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

	static int hashesId = Shader.PropertyToID("_Hashes");
	static int configId = Shader.PropertyToID("_Config");
	static int positionsId = Shader.PropertyToID("_Positions");

	[SerializeField]
	Mesh instanceMesh;

	[SerializeField]
	Material material;

	[SerializeField]
	int seed;

	[SerializeField]
	SpaceTRS domain = new SpaceTRS
	{
		scale = 8f
	};

	[SerializeField, Range(1, 512)]
	int resolution = 16;

	[SerializeField, Range(-2f, 2f)]
	float verticalOffset = 1f;

	NativeArray<uint> hashes;
	NativeArray<float3> positions;
	GraphicsBuffer hashesBuffer;
	GraphicsBuffer positionsBuffer;
	MaterialPropertyBlock propertyBlock;

	void OnEnable()
	{
		// ハッシュ値に対応する配列を作成する
		int length = resolution * resolution;
		hashes = new NativeArray<uint>(length, Allocator.Persistent);
		positions = new NativeArray<float3>(length, Allocator.Persistent);
		hashesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, 4);
		positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, 3 * 4);

		// Jobを作成して完了まで待機
		JobHandle handle = Shapes.Job.ScheduleParallel(positions, resolution, default);

		new HashJob
		{
			hashes = hashes,
			positions = positions,
			hash = SmallXXHash.Seed(seed),
			domainTRS = domain.Matrix
		}.ScheduleParallel(hashes.Length, resolution, handle).Complete();

		// 可視化用にGraphicsBufferにハッシュの値を設定しMaterialPropertyBlockに再設定する。
		hashesBuffer.SetData(hashes);
		positionsBuffer.SetData(positions);
		propertyBlock ??= new MaterialPropertyBlock();
		propertyBlock.SetBuffer(hashesId, hashesBuffer);
		propertyBlock.SetBuffer(positionsId, positionsBuffer);
		propertyBlock.SetVector(configId, new Vector4(
			resolution, 1f / resolution, verticalOffset / resolution
		));
	}

	private void OnDisable()
	{
		// 確保していた資源の確保
		hashes.Dispose();
		positions.Dispose();
		hashesBuffer.Release();
		positionsBuffer.Release();
		hashesBuffer = default;
		positionsBuffer = default;
	}

	private void OnValidate()
	{
		// OnValidate時(Inspectorの変更時)などに有効になっている場合は、
		// 資源の開放と再確保を行う。
		if(hashesBuffer != default && enabled)
		{
			OnDisable();
			OnEnable();
		}
	}

	private void Update()
	{
		Graphics.DrawMeshInstancedProcedural(
			instanceMesh, 0, material, new Bounds(Vector3.zero, Vector3.one),
			hashes.Length, propertyBlock
		);
	}
}
