#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<uint> _Hashes;
StructuredBuffer<float3> _Positions;
#endif

float4 _Config;

void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	// 全体で-0.5,+0.5の範囲に収まるように行列の内容を更新する
	// 誤差の関係でfloorをすると1に丸まってほしいものが0になってしまうことがあるので、小さなバイアスを加える。
	//float v = floor(_Config.y * unity_InstanceID + 0.000001);
	//float u = unity_InstanceID - _Config.x * v;

	unity_ObjectToWorld = 0.0;
	// 平行移動
	unity_ObjectToWorld._m03_m13_m23_m33 = float4(
		_Positions[unity_InstanceID],
		1.0
	);
	unity_ObjectToWorld._m13 += _Config.z * ((1.0 / 255.0) * (_Hashes[unity_InstanceID] >> 24) - 0.5);
	// 拡大縮小
	unity_ObjectToWorld._m00_m11_m22 = _Config.y;
#endif
}

float3 GetHashColor()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	uint hash = _Hashes[unity_InstanceID];
	// 255周期でグラデーション
	return (1.0 / 255.0) * float3(
		hash & 255,
		(hash >> 8) & 255,
		(hash >> 16) & 255);
#else
	return 1.0;
#endif
}

void ShaderGraphFunction_float(float3 In, out float3 Out, out float3 Color)
{
	Out = In;
	Color = GetHashColor();
}

void ShaderGraphFunction_half(half3 In, out half3 Out, out half3 Color)
{
	Out = In;
	Color = GetHashColor();
}