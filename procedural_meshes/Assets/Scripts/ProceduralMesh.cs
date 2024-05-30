using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour {
	Mesh mesh;

	[System.NonSerialized]
	Vector3[] vertices, normals;

	[System.NonSerialized]
	Vector4[] tangents;

	[System.NonSerialized]
	int[] triangles;

	public enum MaterialMode { Flat, Ripple, LatLonMap, CubeMap }

	[SerializeField]
	MaterialMode material;

	[SerializeField]
	Material[] materials;

	static MeshJobScheduleDelegate[] jobs = {
		MeshJob<SquareGrid, SingleStream>.ScheduleParallel,
		MeshJob<SharedSquareGrid, SingleStream>.ScheduleParallel,
		MeshJob<SharedTringleGrid, SingleStream>.ScheduleParallel,
		MeshJob<PointHexagonGrid, SingleStream>.ScheduleParallel,
		MeshJob<FlatHexagonGrid, SingleStream>.ScheduleParallel,
		MeshJob<CubeSphere, SingleStream>.ScheduleParallel,
		MeshJob<SharedCubeSphere, PositionStream>.ScheduleParallel,
		MeshJob<UVSphere, SingleStream>.ScheduleParallel,
	};

	public enum MeshType
	{
		SquareGrid, SharedSquareGrid, SharedTringleGrid, SharedHexagonGrid, FlatHexagonGrid, CubeSphere, SharedCubeSphere, UVSphere
	};

	[SerializeField]
	MeshType meshType;

	[System.Flags]
	public enum MeshOptimizationMode
	{
		Nothing = 0, ReorderIndices = 1, ReorderVertices = 0b10
	}

	[SerializeField]
	MeshOptimizationMode meshOptimization;

	[SerializeField, Range(1, 50)]
	int resolution = 1;

	[System.Flags]
	public enum GizmoMode
	{
		Nothing = 0, Vertices = 1, Normals = 0b10, Tangents = 0b100, Triangles = 0b1000
	}

	[SerializeField]
	GizmoMode gizmos;


	void Awake()
	{
		mesh = new Mesh
		{
			name = "Procedural Mesh"
		};
		// GenerateMesh();
		GetComponent<MeshFilter>().mesh = mesh;
	}

	void OnValidate() => enabled = true;

	void GenerateMesh() {
		Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
		Mesh.MeshData meshData = meshDataArray[0];

		jobs[(int)meshType](mesh, meshData, resolution, default).Complete();

		Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
	}

	void Update()
	{
		GenerateMesh();
		enabled = false;

		vertices = null;
		normals = null;
		tangents = null;
		GetComponent<MeshRenderer>().material = materials[(int)material];
	}

	void OnDrawGizmos()
	{
		if (gizmos == GizmoMode.Nothing || mesh == null)
		{
			return;
		}

		bool drawVertices = (gizmos & GizmoMode.Vertices) != 0;
		bool drawNormals = (gizmos & GizmoMode.Normals) != 0;
		bool drawTangents = (gizmos & GizmoMode.Tangents) != 0;
		bool drawTriangles = (gizmos & GizmoMode.Triangles) != 0;

		if (vertices == null)
		{
			vertices = mesh.vertices;
		}
		if (drawNormals && normals == null)
		{
			drawNormals = mesh.HasVertexAttribute(VertexAttribute.Normal);
			if (drawNormals)
			{
				normals = mesh.normals;
			}
		}
		if (drawTangents && tangents == null)
		{
			drawTangents = mesh.HasVertexAttribute(VertexAttribute.Tangent);
			if (drawTangents)
			{
				tangents = mesh.tangents;
			}
		}
		if (drawTriangles && triangles == null)
		{
			triangles = mesh.triangles;
		}

		Transform t = transform;
		for (int i = 0; i < vertices.Length; i++)
		{
			Vector3 position = t.TransformPoint(vertices[i]);
			if (drawVertices)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawSphere(position, 0.02f);
			}
			if (drawNormals)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawRay(position, t.TransformDirection(normals[i]) * 0.2f);
			}
			if (drawTangents)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawRay(position, t.TransformDirection(tangents[i]) * 0.2f);
			}
		}

		if (drawTriangles)
		{
			float colorStep = 1f / (triangles.Length - 3);
			for (int i = 0; i < triangles.Length; i += 3)
			{
				float c = i * colorStep;
				Gizmos.color = new Color(c, 0f, c);
				Gizmos.DrawSphere(
					t.TransformPoint((
						vertices[triangles[i]] +
						vertices[triangles[i + 1]] +
						vertices[triangles[i + 2]]
					) * (1f / 3f)),
					0.02f
				);
			}
		}
	}
}