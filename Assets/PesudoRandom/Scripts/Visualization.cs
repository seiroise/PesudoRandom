using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

/// <summary>
/// 可視化の基底クラス
/// </summary>
public abstract class Visualization : MonoBehaviour
{
	static int configId = Shader.PropertyToID("_Config");
	static int positionsId = Shader.PropertyToID("_Positions");
	static int normalsId = Shader.PropertyToID("_Normals");

	[SerializeField]
	Mesh instanceMesh;

	[SerializeField]
	Material material;

	[SerializeField, Range(1, 512)]
	int resolution = 16;

	[SerializeField, Range(-100f, 100f)]
	float displacement = 0.1f;

	[SerializeField, Range(0.1f, 10f)]
	float instanceScale = 2f;

	[SerializeField]
	Shapes.Shape shape = Shapes.Shape.Sphere;

	NativeArray<float3x4> positions, normals;
	GraphicsBuffer positionsBuffer, normalsBuffer;
	MaterialPropertyBlock propertyBlock;

	bool isDirty;
	Bounds bounds;

	void OnEnable()
	{
		isDirty = true;
		// ハッシュ値に対応する配列を作成する
		int length = resolution * resolution;
		length = length / 4 + (length & 1);
		positions = new NativeArray<float3x4>(length, Allocator.Persistent);
		normals = new NativeArray<float3x4>(length, Allocator.Persistent);

		positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length * 4, 3 * 4);
		normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length * 4, 3 * 4);

		// 可視化用にGraphicsBufferにハッシュの値を設定しMaterialPropertyBlockに再設定する。
		propertyBlock ??= new MaterialPropertyBlock();
		EnableVisualization(length, propertyBlock);
		propertyBlock.SetBuffer(positionsId, positionsBuffer);
		propertyBlock.SetBuffer(normalsId, normalsBuffer);
		propertyBlock.SetVector(configId, new Vector4(
			resolution, instanceScale / resolution, displacement / resolution
		));
	}

	private void OnDisable()
	{
		// 確保していた資源の確保
		positions.Dispose();
		normals.Dispose();
		positionsBuffer.Release();
		normalsBuffer.Release();
		positionsBuffer = default;
		normalsBuffer = default;

		DisableVisualization();
	}

	private void OnValidate()
	{
		// OnValidate時(Inspectorの変更時)などに有効になっている場合は、
		// 資源の開放と再確保を行う。
		if(positionsBuffer != default && enabled)
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
			// 形状に対応する座標と法線を出力
			// その結果を派生クラスに渡す。
			JobHandle handle = Shapes.shapeJobs[(int)shape](positions, normals, resolution, transform.localToWorldMatrix, default);
			UpdateVisualization(positions, resolution, handle);
			//new HashJob4
			//{
			//	positions = positions,
			//	hashes = hashes,
			//	hash = SmallXXHash.Seed(seed),
			//	domainTRS = domain.Matrix
			//}.ScheduleParallel(hashes.Length, resolution, handle).Complete();
			if(!handle.IsCompleted)
			{
				handle.Complete();
			}

			sw.Stop();
			Debug.Log($"{sw.ElapsedMilliseconds}({sw.ElapsedTicks})");

			// 可視化用にGraphicsBufferにハッシュの値を設定しMaterialPropertyBlockに再設定する。
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

	protected abstract void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock);
	protected abstract void DisableVisualization();

	protected abstract void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle);
}
