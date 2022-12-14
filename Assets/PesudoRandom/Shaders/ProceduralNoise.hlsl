#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
// ノイズ[-1, 1]の範囲に分布
StructuredBuffer<float> _Noise;
StructuredBuffer<float3> _Positions, _Normals;
#endif

float4 _Config;

void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	unity_ObjectToWorld = 0.0;
	// 平行移動
	unity_ObjectToWorld._m03_m13_m23_m33 = float4(
		_Positions[unity_InstanceID],
		1.0
	);
	unity_ObjectToWorld._m03_m13_m23 += 
		_Config.z * _Noise[unity_InstanceID] * _Normals[unity_InstanceID];
	// 拡大縮小
	unity_ObjectToWorld._m00_m11_m22 = _Config.y;// *_Noise[unity_InstanceID];
#endif
}

float3 GetNoiseColor()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	float noise = _Noise[unity_InstanceID];
	return noise < 0.0 ? float3(-noise, 0.0, 0.0) : noise;
#else
	return 1.0;
#endif
}

void ShaderGraphFunction_float(float3 In, out float3 Out, out float3 Color)
{
	Out = In;
	Color = GetNoiseColor();
}

void ShaderGraphFunction_half(half3 In, out half3 Out, out half3 Color)
{
	Out = In;
	Color = GetNoiseColor();
}

void GetNoise_float(out float Out)
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)	
	Out = _Noise[unity_InstanceID];
#else
	Out = 0.0;
#endif
}

void GetNoise_half(out half Out)
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	Out = _Noise[unity_InstanceID];
#else
	Out = 0.0;
#endif
}
