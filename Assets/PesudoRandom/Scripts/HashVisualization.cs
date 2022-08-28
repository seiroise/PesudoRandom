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

		float4x3 TransformPositions(float3x4 trs, float4x3 p) => float4x3(
			trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x,
			trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y,
			trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z
		);

		public void Execute(int i)
		{
			// x,y,zそれぞれの要素ごとにベクトル化するので、行列を転置して
			// それぞれの0列目にx, 1列目にy, 2列目にzが来るようにする。
			float4x3 p = TransformPositions(domainTRS, transpose(positions[i]));

			int4 u = (int4)floor(p.c0);
			int4 v = (int4)floor(p.c1);
			int4 w = (int4)floor(p.c2);

			hashes[i] = hash.Eat(u).Eat(v).Eat(w);
		}
	}

	static int hashesId = Shader.PropertyToID("_Hashes");
	static int configId = Shader.PropertyToID("_Config");
	static int positionsId = Shader.PropertyToID("_Positions");
	static int normalsId = Shader.PropertyToID("_Normals");

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

	[SerializeField, Range(-0.5f, 0.5f)]
	float displacement = 0.1f;

	[SerializeField]
	bool vectorize = false;

	NativeArray<uint4> hashes;
	NativeArray<float3x4> positions, normals;
	GraphicsBuffer hashesBuffer, positionsBuffer, normalsBuffer;
	MaterialPropertyBlock propertyBlock;

	bool isDirty;
	Bounds bounds;

	void OnEnable()
	{
		isDirty = true;
		// ハッシュ値に対応する配列を作成する
		int length = resolution * resolution;
		length = length / 4 + (length & 1);
		hashes = new NativeArray<uint4>(length, Allocator.Persistent);
		positions = new NativeArray<float3x4>(length, Allocator.Persistent);
		normals = new NativeArray<float3x4>(length, Allocator.Persistent);

		hashesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length * 4, 4);
		positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length * 4, 3 * 4);
		normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length * 4, 3 * 4);

		// 可視化用にGraphicsBufferにハッシュの値を設定しMaterialPropertyBlockに再設定する。
		propertyBlock ??= new MaterialPropertyBlock();
		propertyBlock.SetBuffer(hashesId, hashesBuffer);
		propertyBlock.SetBuffer(positionsId, positionsBuffer);
		propertyBlock.SetBuffer(normalsId, normalsBuffer);
		propertyBlock.SetVector(configId, new Vector4(
			resolution, 1f / resolution, displacement / resolution
		));
	}

	private void OnDisable()
	{
		// 確保していた資源の確保
		hashes.Dispose();
		positions.Dispose();
		normals.Dispose();
		hashesBuffer.Release();
		positionsBuffer.Release();
		normalsBuffer.Release();
		hashesBuffer = default;
		positionsBuffer = default;
		normalsBuffer = default;
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
		if(isDirty || transform.hasChanged)
		{
			// 再計算が必要な変更が起きている。
			isDirty = false;
			transform.hasChanged = false;

			var sw = System.Diagnostics.Stopwatch.StartNew();
			if(vectorize)
			{
				// ベクトル化可能な場合
				JobHandle handle = Shapes.Job4.ScheduleParallel(
					positions, normals, resolution, transform.localToWorldMatrix, default
				);
				handle.Complete();
				new HashJob4
				{
					positions = positions,
					hashes = hashes,
					hash = SmallXXHash.Seed(seed),
					domainTRS = domain.Matrix
				}.ScheduleParallel(hashes.Length, resolution, handle).Complete();
			}
			else
			{
				// ベクトル化しない場合
				NativeArray<float3> positionsFloat3 = positions.Reinterpret<float3>(3 * 4 * 4);
				NativeArray<float3> normalsFloat3 = normals.Reinterpret<float3>(3 * 4 * 4);
				JobHandle handle = Shapes.Job.ScheduleParallel(
					positionsFloat3, normalsFloat3, resolution, transform.localToWorldMatrix, default
				);
				handle.Complete();
				new HashJob
				{
					positions = positionsFloat3,
					hashes = hashes.Reinterpret<uint>(4 * 4),
					hash = SmallXXHash.Seed(seed),
					domainTRS = domain.Matrix
				}.ScheduleParallel(hashes.Length * 4, resolution, handle).Complete();
			}
			sw.Stop();
			Debug.Log($"{sw.ElapsedMilliseconds}({sw.ElapsedTicks})");

			// 可視化用にGraphicsBufferにハッシュの値を設定しMaterialPropertyBlockに再設定する。
			hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
			positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
			normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));

			// 描画範囲を更新
			bounds = new Bounds(transform.position, float3(2f * cmax(abs(transform.lossyScale)) + displacement));
		}

		Graphics.DrawMeshInstancedProcedural(
			instanceMesh, 0, material, bounds,
			resolution * resolution, propertyBlock
		);
	}
}
