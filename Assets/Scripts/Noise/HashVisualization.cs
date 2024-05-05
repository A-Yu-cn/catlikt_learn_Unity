using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public class HashVisualization : MonoBehaviour
{

	[System.Serializable]
	public struct SpaceTRS
	{

		public float3 translation, rotation, scale;
		public float3x4 Matrix
		{
			get
			{
				float4x4 m = Unity.Mathematics.float4x4.TRS(
					translation, Unity.Mathematics.quaternion.EulerZXY(radians(rotation)), scale
				);
				return math.float3x4(m.c0.xyz, m.c1.xyz, m.c2.xyz, m.c3.xyz);
			}
		}
	}
	[SerializeField]
	SpaceTRS domain = new SpaceTRS
	{
		scale = 8f
	};


	public enum Shape { Plane, Sphere, Torus }

	static Shapes.ScheduleDelegate[] shapeJobs = {
		Shapes.Job<Shapes.Plane>.ScheduleParallel,
		Shapes.Job<Shapes.Sphere>.ScheduleParallel,
		Shapes.Job<Shapes.Torus>.ScheduleParallel
	};

	[SerializeField]
	Shape shape;


	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	struct HashJob : IJobFor
	{
		[ReadOnly]
		public NativeArray<float3x4> positions;
		[WriteOnly]
		public NativeArray<uint4> hashes;

		//public int resolution;

		public SmallXXHash4 hash;

		//public float invResolution;

		public float3x4 domainTRS;

		public void Execute(int i)
		{
			//float vf = math.floor(invResolution * i + 0.00001f);
			//float uf = invResolution * (i - resolution * vf + 0.5f) - 0.5f;
			//vf = invResolution * (vf + 0.5f) - 0.5f;


			float4x3 p = TransformPositions(domainTRS, transpose(positions[i]));

			int4 u = (int4)floor(p.c0);
			int4 v = (int4)floor(p.c1);
			int4 w = (int4)floor(p.c2);


			//int u = (int)floor(uf * 32 / 4);
			//int v = (int)floor(vf * 32 / 4);


			//var hash = new SmallXXHash(0).Eat(u).Eat(v);
			//hash.Eat(u);
			//hash.Eat(v);
			hashes[i] = hash.Eat(u).Eat(v).Eat(w);
			//hashes[i] = (uint)(frac(u * v * 0.381f) * 255f);
		}

		float4x3 TransformPositions(float3x4 trs, float4x3 p) => float4x3(
			trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x,
			trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y,
			trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z
		);
	}
	static int
		hashesId = Shader.PropertyToID("_Hashes"),
		positionsId = Shader.PropertyToID("_Positions"),
		normalsId = Shader.PropertyToID("_Normals"),
		configId = Shader.PropertyToID("_Config");
		
	[SerializeField]
	int seed;

	//[SerializeField, Range(-2f, 2f)]
	//float verticalOffset = 1f;

	[SerializeField, Range(-0.5f, 0.5f)]
	float displacement = 0.1f;

	[SerializeField]
	Mesh instanceMesh;

	[SerializeField]
	Material material;

	[SerializeField, Range(1, 512)]
	int resolution = 16;

	[SerializeField, Range(0.1f, 10f)]
	float instanceScale = 2f;

	NativeArray<uint4> hashes;

	NativeArray<float3x4> positions, normals;

	ComputeBuffer hashesBuffer, positionsBuffer, normalsBuffer;

	MaterialPropertyBlock propertyBlock;

	bool isDirty;
	Bounds bounds;
	void OnEnable()
	{
		isDirty = true;

		int length = resolution * resolution;
		length /= 4 + (length & 1);
		hashes = new NativeArray<uint4>(length, Allocator.Persistent);
		positions = new NativeArray<float3x4>(length, Allocator.Persistent);
		normals = new NativeArray<float3x4>(length, Allocator.Persistent);
		hashesBuffer = new ComputeBuffer(length * 4, 4);
		positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);
		normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

		//JobHandle handle = Shapes.Job.ScheduleParallel(positions, resolution, transform.localToWorldMatrix, default);

		//new HashJob
		//{
		//	positions = positions,
		//	hashes = hashes,
		//	//resolution = resolution,
		//	//invResolution = 1f / resolution,
		//	hash = SmallXXHash.Seed(seed),
		//	domainTRS = domain.Matrix
		//}.ScheduleParallel(hashes.Length, resolution, handle).Complete();

		//hashesBuffer.SetData(hashes);
		//positionsBuffer.SetData(positions);

		propertyBlock ??= new MaterialPropertyBlock();
		propertyBlock.SetBuffer(hashesId, hashesBuffer);
		propertyBlock.SetBuffer(positionsId, positionsBuffer);
		propertyBlock.SetBuffer(normalsId, normalsBuffer);
		propertyBlock.SetVector(configId, new Vector4(
			resolution, instanceScale / resolution, displacement
		));
	}

	void OnDisable()
	{
		hashes.Dispose();
		positions.Dispose();
		normals.Dispose();
		hashesBuffer.Release();
		positionsBuffer.Release();
		normalsBuffer.Release();
		hashesBuffer = null;
		positionsBuffer = null;
		normalsBuffer = null;
	}

	void OnValidate()
	{
		if (hashesBuffer != null && enabled)
		{
			OnDisable();
			OnEnable();
		}
	}

	void Update()
	{
		if (isDirty || transform.hasChanged)
		{
			isDirty = false;
			transform.hasChanged = false;

			JobHandle handle = shapeJobs[(int)shape](
				positions, normals, resolution, transform.localToWorldMatrix, default
			);

			new HashJob
			{
				positions = positions,
				hashes = hashes,
				hash = SmallXXHash.Seed(seed),
				domainTRS = domain.Matrix
			}.ScheduleParallel(hashes.Length, resolution, handle).Complete();

			hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
			positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
			normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));

			bounds = new Bounds(
				transform.position,
				float3(2f * cmax(abs(transform.lossyScale)) + displacement)
			);
		}
		Graphics.DrawMeshInstancedProcedural(
			instanceMesh, 0, material, bounds,
			resolution * resolution, propertyBlock
		);
	}

}