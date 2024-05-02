using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Fractal : MonoBehaviour
{
    [BurstCompile(CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor
    {
        public float spinAngleDelta;
        public float scale;
        [ReadOnly]
        public NativeArray<FractalPart> parents;

        public NativeArray<FractalPart> parts;

        [WriteOnly]
        public NativeArray<Matrix4x4> matrices;
        public void Execute(int i) {
            FractalPart parent = parents[i / 5];
            FractalPart part = parts[i];
            part.spinAngle += spinAngleDelta;
            part.worldRotation =
                parent.worldRotation *
                (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
            part.worldPosition =
                parent.worldPosition +
                parent.worldRotation * (1.5f * scale * part.direction);
            parts[i] = part;

            matrices[i] = Matrix4x4.TRS(
                part.worldPosition, part.worldRotation, scale * Vector3.one
            );
        }
    }

    [SerializeField, Range(1, 8)]
    int depth = 4;

    [SerializeField]
    Mesh mesh;

    [SerializeField]
    Material material;

    static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };

    ComputeBuffer[] matricesBuffers;

    static readonly int matricesId = Shader.PropertyToID("_Matrices");

    static MaterialPropertyBlock propertyBlock;

    NativeArray<FractalPart>[] parts;

    NativeArray<Matrix4x4>[] matrices;

    //Fractal CreateChild(Vector3 direction, Quaternion rotation)
    //{
    //    Fractal child = Instantiate(this);
    //    child.depth = depth - 1;
    //    child.transform.localPosition = 0.75f * direction;
    //    child.transform.localRotation = rotation;
    //    child.transform.localScale = 0.5f * Vector3.one;
    //    return child;
    //}

    void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<Matrix4x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        int stride = 16 * 4;
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<Matrix4x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
        }
        //float scale = 1f;
        parts[0][0] = CreatePart(0);
        for (int li = 1; li < parts.Length; li++)
        {
            //scale *= 0.5f;
            NativeArray<FractalPart> levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }
        propertyBlock ??= new MaterialPropertyBlock();
    }
    private void OnDisable()
    {
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }
        parts = null;
        matrices = null;
        matricesBuffers = null;
    }
    void OnValidate()
    {
        if (parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    FractalPart CreatePart(int childIndex)
    {
        //var go = new GameObject("Fractal Part L" + levelIndex + " C" + childIndex);
        //go.transform.localScale = scale * Vector3.one;
        //go.transform.SetParent(transform, false);
        //go.AddComponent<MeshFilter>().mesh = mesh;
        //go.AddComponent<MeshRenderer>().material = material;

        return new FractalPart
        {
            direction = directions[childIndex],
            rotation = rotations[childIndex] //,
                                             //transform = go.transform
        };
    }

    // Start is called before the first frame update
    void Start()
    {
       
        //name = "Fractal " + depth;

        //if (depth <= 1)
        //{
        //    return;
        //}

        //Fractal childA = CreateChild(Vector3.up, Quaternion.identity);
        //Fractal childB = CreateChild(Vector3.right, Quaternion.Euler(0f, 0f, -90f));
        //Fractal childC = CreateChild(Vector3.left, Quaternion.Euler(0f, 0f, 90f));
        //Fractal childD = CreateChild(Vector3.forward, Quaternion.Euler(90f, 0f, 0f));
        //Fractal childE = CreateChild(Vector3.back, Quaternion.Euler(-90f, 0f, 0f));

        //childA.transform.SetParent(transform, false);
        //childB.transform.SetParent(transform, false);
        //childC.transform.SetParent(transform, false);
        //childD.transform.SetParent(transform, false);
        //childE.transform.SetParent(transform, false);
    }

    // Update is called once per frame
    void Update()
    {
        //Quaternion deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);
        float spinAngleDelta = 22.5f * Time.deltaTime;

        FractalPart rootPart = parts[0][0];
        //rootPart.rotation *= deltaRotation;
        rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation =
            transform.rotation *
            (rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f));
        rootPart.worldPosition = transform.position;
        parts[0][0] = rootPart;
        float objectScale = transform.lossyScale.x;
        matrices[0][0] = Matrix4x4.TRS(
            rootPart.worldPosition, rootPart.worldRotation, objectScale * Vector3.one
        );

        float scale = objectScale;
        JobHandle jobHandle = default;
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            jobHandle = new UpdateFractalLevelJob
            {
                spinAngleDelta = spinAngleDelta,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.Schedule(parts[li].Length, jobHandle);
            //NativeArray<FractalPart> parentParts = parts[li - 1];
            //NativeArray<FractalPart> levelParts = parts[li];
            //NativeArray<Matrix4x4> levelMatrices = matrices[li];
            //for (int fpi = 0; fpi < parts[li].Length; fpi++) {
            //	job.Execute(fpi);
            //}
        }
        jobHandle.Complete();

        var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            propertyBlock.SetBuffer(matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(
                mesh, 0, material, bounds, buffer.count, propertyBlock
            );
        }
    }

    struct FractalPart
    {
        public Vector3 direction, worldPosition;
        public Quaternion rotation, worldRotation;
        public float spinAngle;
        //public Transform transform;
    }
    //FractalPart[][] parts;

    //Matrix4x4[][] matrices;
}
