using Unity.Mathematics;

[System.Serializable]
public struct SpaceTRS
{
	public float3 translation, rotation, scale;

	public float3x4 Matrix
	{
		get
		{
			float4x4 m = float4x4.TRS(translation, quaternion.EulerZXY(math.radians(rotation)), scale);
			// 行列の4行目は常に0001なので、3x4行列を使用する。
			return math.float3x4(m.c0.xyz, m.c1.xyz, m.c2.xyz, m.c3.xyz);
		}
	}
}