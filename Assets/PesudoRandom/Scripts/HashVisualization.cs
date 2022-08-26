using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class HashVisualization : MonoBehaviour
{
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	struct HashJob : IJobFor
	{
		[WriteOnly]
		public NativeArray<uint> hashes;

		public void Execute(int i)
		{
			hashes[i] = (uint)i;
		}
	}

	static int hashesId = Shader.PropertyToID("_Hashes");
	static int configId = Shader.PropertyToID("_Config");

	[SerializeField]
	Mesh instanceMesh;

	[SerializeField]
	Material material;

	[SerializeField, Range(1, 512)]
	int resolution = 16;

	NativeArray<uint> hashes;
	GraphicsBuffer hashesBuffer;
	MaterialPropertyBlock propertyBlock;

	void OnEnable()
	{
		// ハッシュ値に対応する配列を作成する
		int length = resolution * resolution;
		hashes = new NativeArray<uint>(length, Allocator.Persistent);
		hashesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, 4);

		// Jobを作成して完了まで待機
		new HashJob
		{
			hashes = hashes
		}.ScheduleParallel(hashes.Length, resolution, default).Complete();

		// 可視化用にGraphicsBufferにハッシュの値を設定しMaterialPropertyBlockに再設定する。
		hashesBuffer.SetData(hashes);
		propertyBlock ??= new MaterialPropertyBlock();
		propertyBlock.SetBuffer(hashesId, hashesBuffer);
		propertyBlock.SetVector(configId, new Vector4(resolution, 1f / resolution));
	}

	private void OnDisable()
	{
		// 確保していた資源の確保
		hashes.Dispose();
		hashesBuffer.Release();
		hashesBuffer = default;
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
	}
}
