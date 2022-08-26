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
		// �n�b�V���l�ɑΉ�����z����쐬����
		int length = resolution * resolution;
		hashes = new NativeArray<uint>(length, Allocator.Persistent);
		hashesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, 4);

		// Job���쐬���Ċ����܂őҋ@
		new HashJob
		{
			hashes = hashes
		}.ScheduleParallel(hashes.Length, resolution, default).Complete();

		// �����p��GraphicsBuffer�Ƀn�b�V���̒l��ݒ肵MaterialPropertyBlock�ɍĐݒ肷��B
		hashesBuffer.SetData(hashes);
		propertyBlock ??= new MaterialPropertyBlock();
		propertyBlock.SetBuffer(hashesId, hashesBuffer);
		propertyBlock.SetVector(configId, new Vector4(resolution, 1f / resolution));
	}

	private void OnDisable()
	{
		// �m�ۂ��Ă��������̊m��
		hashes.Dispose();
		hashesBuffer.Release();
		hashesBuffer = default;
	}

	private void OnValidate()
	{
		// OnValidate��(Inspector�̕ύX��)�ȂǂɗL���ɂȂ��Ă���ꍇ�́A
		// �����̊J���ƍĊm�ۂ��s���B
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
