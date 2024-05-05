using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public abstract class Visualization : MonoBehaviour
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

	public enum Shape { Plane, Sphere, Torus }

	static Shapes.ScheduleDelegate[] shapeJobs = {
		Shapes.Job<Shapes.Plane>.ScheduleParallel,
		Shapes.Job<Shapes.Sphere>.ScheduleParallel,
		Shapes.Job<Shapes.Torus>.ScheduleParallel
	};

	[SerializeField]
	Shape shape;


	static int
		positionsId = Shader.PropertyToID("_Positions"),
		normalsId = Shader.PropertyToID("_Normals"),
		configId = Shader.PropertyToID("_Config");


	[SerializeField]
	Mesh instanceMesh;

	[SerializeField]
	Material material;

	[SerializeField, Range(1, 512)]
	int resolution = 16;

	[SerializeField, Range(-0.5f, 0.5f)]
	float displacement = 0.1f;

	[SerializeField, Range(0.1f, 10f)]
	float instanceScale = 2f;

	NativeArray<float3x4> positions, normals;

	ComputeBuffer positionsBuffer, normalsBuffer;

	MaterialPropertyBlock propertyBlock;

	bool isDirty;
	Bounds bounds;

	protected abstract void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock);

	protected abstract void DisableVisualization();

	protected abstract void UpdateVisualization(
		NativeArray<float3x4> positions, int resolution, JobHandle handle
	);


	void OnEnable()
	{
		isDirty = true;

		int length = resolution * resolution;
		length /= 4 + (length & 1);
		positions = new NativeArray<float3x4>(length, Allocator.Persistent);
		normals = new NativeArray<float3x4>(length, Allocator.Persistent);
		positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);
		normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

		propertyBlock ??= new MaterialPropertyBlock();
		EnableVisualization(length, propertyBlock);
		propertyBlock.SetBuffer(positionsId, positionsBuffer);
		propertyBlock.SetBuffer(normalsId, normalsBuffer);
		propertyBlock.SetVector(configId, new Vector4(
			resolution, instanceScale / resolution, displacement
		));
	}

	void OnDisable()
	{
		positions.Dispose();
		normals.Dispose();
		positionsBuffer.Release();
		normalsBuffer.Release();
		positionsBuffer = null;
		normalsBuffer = null;
		DisableVisualization();
	}

	void OnValidate()
	{
		if (positionsBuffer != null && enabled)
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

			UpdateVisualization(
				positions, resolution,
				shapeJobs[(int)shape](
					positions, normals, resolution, transform.localToWorldMatrix, default
				)
			);

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