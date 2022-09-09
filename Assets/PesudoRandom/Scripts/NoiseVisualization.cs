using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Noise;

public class NoiseVisualization : Visualization
{
	static int noiseId = Shader.PropertyToID("_Noise");

	public enum NoiseType
	{
		GridViewer,
		Pattern1,
		Value,
		ValueTurbulence,
		ValuePower2,
		Perlin,
		PerlinTurbulence,
		PerlinPower2,
		VoronoiWorleyF1,
		VoronoiWorleyF2,
		VoronoiWorleyF2MinusF1,
		VoronoiChebyshevF1,
		VoronoiChebyshevF2,
		VoronoiChebyshevF2MinusF1,
		VoronoiManhattanF1,
		VoronoiManhattanF2,
		VoronoiManhattanF2MinusF1,
	}

	public enum NoiseDimensions
	{
		One = 1,
		Two = 2,
		Three = 3,
	}

	public static ScheduleDelegate[,] noiseJobs =
	{
		{
			Job<GridViewer1D<LatticeNormal>>.ScheduleParallel,
			Job<GridViewer1D<LatticeTiling>>.ScheduleParallel,
			Job<GridViewer2D<LatticeNormal>>.ScheduleParallel,
			Job<GridViewer2D<LatticeTiling>>.ScheduleParallel,
			Job<GridViewer3D<LatticeNormal>>.ScheduleParallel,
			Job<GridViewer3D<LatticeTiling>>.ScheduleParallel,
		},
		{
			Job<Pattern1<LatticeNormal>>.ScheduleParallel,
			Job<Pattern1<LatticeTiling>>.ScheduleParallel,
			Job<Pattern1<LatticeNormal>>.ScheduleParallel,
			Job<Pattern1<LatticeTiling>>.ScheduleParallel,
			Job<Pattern1<LatticeNormal>>.ScheduleParallel,
			Job<Pattern1<LatticeTiling>>.ScheduleParallel,
		},
		{
			Job<Lattice1D<LatticeNormal, Value>>.ScheduleParallel,
			Job<Lattice1D<LatticeTiling, Value>>.ScheduleParallel,
			Job<Lattice2D<LatticeNormal, Value>>.ScheduleParallel,
			Job<Lattice2D<LatticeTiling, Value>>.ScheduleParallel,
			Job<Lattice3D<LatticeNormal, Value>>.ScheduleParallel,
			Job<Lattice3D<LatticeTiling, Value>>.ScheduleParallel
		},
		{
			Job<Lattice1D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice1D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice2D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice2D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice3D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice3D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel
		},
		{
			Job<Lattice1D<LatticeNormal, Power2<Value>>>.ScheduleParallel,
			Job<Lattice1D<LatticeTiling, Power2<Value>>>.ScheduleParallel,
			Job<Lattice2D<LatticeNormal, Power2<Value>>>.ScheduleParallel,
			Job<Lattice2D<LatticeTiling, Power2<Value>>>.ScheduleParallel,
			Job<Lattice3D<LatticeNormal, Power2<Value>>>.ScheduleParallel,
			Job<Lattice3D<LatticeTiling, Power2<Value>>>.ScheduleParallel
		},
		{
			Job<Lattice1D<LatticeNormal, Perlin>>.ScheduleParallel,
			Job<Lattice1D<LatticeTiling, Perlin>>.ScheduleParallel,
			Job<Lattice2D<LatticeNormal, Perlin>>.ScheduleParallel,
			Job<Lattice2D<LatticeTiling, Perlin>>.ScheduleParallel,
			Job<Lattice3D<LatticeNormal, Perlin>>.ScheduleParallel,
			Job<Lattice3D<LatticeTiling, Perlin>>.ScheduleParallel
		},
		{
			Job<Lattice1D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice1D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice2D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice2D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice3D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice3D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel
		},
		{
			Job<Lattice1D<LatticeNormal, Power2<Perlin>>>.ScheduleParallel,
			Job<Lattice1D<LatticeTiling, Power2<Perlin>>>.ScheduleParallel,
			Job<Lattice2D<LatticeNormal, Power2<Perlin>>>.ScheduleParallel,
			Job<Lattice2D<LatticeTiling, Power2<Perlin>>>.ScheduleParallel,
			Job<Lattice3D<LatticeNormal, Power2<Perlin>>>.ScheduleParallel,
			Job<Lattice3D<LatticeTiling, Power2<Perlin>>>.ScheduleParallel
		},
		{
			Job<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Worley, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Worley, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Worley, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Worley, F1>>.ScheduleParallel
		},
		{
			Job<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Worley, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Worley, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Worley, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Worley, F2>>.ScheduleParallel
		},
		{
			Job<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel
		},
		{
			Job<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Chebyshev, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Chebyshev, F1>>.ScheduleParallel
		},
		{
			Job<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Chebyshev, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Chebyshev, F2>>.ScheduleParallel
		},
		{
			Job<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Chebyshev, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Chebyshev, F2MinusF1>>.ScheduleParallel
		},
		{
			Job<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Manhattan, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Manhattan, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Manhattan, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Manhattan, F1>>.ScheduleParallel
		},
		{
			Job<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Manhattan, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Manhattan, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Manhattan, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Manhattan, F2>>.ScheduleParallel
		},
		{
			Job<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Manhattan, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Manhattan, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Manhattan, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Manhattan, F2MinusF1>>.ScheduleParallel
		}
	};

	[SerializeField]
	NoiseType noiseType;

	[SerializeField]
	NoiseDimensions dimensions;

	[SerializeField]
	bool tiling = false;

	[SerializeField]
	Noise.Settings settings = Settings.Default;

	[SerializeField]
	SpaceTRS domain = new SpaceTRS
	{
		scale = 8f
	};

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
		noiseJobs[(int)noiseType, (int)dimensions * 2 - (tiling ? 1 : 2)](positions, noise, settings, domain, resolution, handle).Complete();
		noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
	}
}
