using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using float3x4 = Unity.Mathematics.float3x4;
using quaternion = Unity.Mathematics.quaternion;

using Random = UnityEngine.Random;
public class Fractal : MonoBehaviour
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor
    {
        //public float spinAngleDelta;
        public float deltaTime;
        public float scale;
        [ReadOnly]
        public NativeArray<FractalPart> parents;

        public NativeArray<FractalPart> parts;

        [WriteOnly]
        public NativeArray<float3x4> matrices;
        public void Execute(int i) {
            FractalPart parent = parents[i / 5];
            FractalPart part = parts[i];
            part.spinAngle += part.spinVelocity * deltaTime;

            float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up());
            float3 sagAxis = cross(up(), upAxis);
            sagAxis = normalize(sagAxis);
            float sagMagnitude = length(sagAxis);
            quaternion baseRotation;
            if (sagMagnitude > 0f)
            {
                sagAxis /= sagMagnitude;
                quaternion sagRotation =
                    quaternion.AxisAngle(sagAxis, part.maxSagAngle * PI * 0.25f * sagMagnitude);
                baseRotation = mul(sagRotation, parent.worldRotation);
            }
            else
            {
                baseRotation = parent.worldRotation;
            }
            part.worldRotation = mul(baseRotation,
                mul(part.rotation, quaternion.RotateY(part.spinAngle))
            );
            part.worldPosition =
                (float3)parent.worldPosition +
                //mul(parent.worldRotation, (1.5f * scale * part.direction));
                mul(part.worldRotation, float3(0f, 1.5f * scale, 0f));
            parts[i] = part;
            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    }

    [SerializeField, Range(3, 8)]
    int depth = 4;

    [SerializeField]
    Mesh mesh, leafMesh;

    [SerializeField]
    Material material;

    [SerializeField]
    //Gradient gradient;
    Gradient gradientA, gradientB;

    [SerializeField]
    Color leafColorA, leafColorB;

    [SerializeField, Range(0f, 90f)]
    float maxSagAngleA = 15f, maxSagAngleB = 25f;

    [SerializeField, Range(0f, 90f)]
    float spinSpeedA = 20f, spinSpeedB = 25f;

    [SerializeField, Range(0f, 1f)]
    float reverseSpinChance = 0.25f;

    //static float3[] directions = {
    //    up(), right(), left(), forward(), back()
    //};

    static quaternion[] rotations = {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };

    ComputeBuffer[] matricesBuffers;

    static readonly int
        colorAId = Shader.PropertyToID("_ColorA"),
        colorBId = Shader.PropertyToID("_ColorB"),
        matricesId = Shader.PropertyToID("_Matrices"),
        sequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");

    static MaterialPropertyBlock propertyBlock;

    NativeArray<FractalPart>[] parts;

    NativeArray<float3x4>[] matrices;

    Vector4[] sequenceNumbers;
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
        matrices = new NativeArray<float3x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        sequenceNumbers = new Vector4[depth];
        int stride = 12 * 4;
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
            sequenceNumbers[i] =
                new Vector4(Random.value, Random.value, Random.value, Random.value);
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
        sequenceNumbers = null;
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
            maxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
            //direction = directions[childIndex],
            rotation = rotations[childIndex],
            spinVelocity =
            (Random.value < reverseSpinChance ? -1f : 1f) *
            radians(Random.Range(spinSpeedA, spinSpeedB))//,
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
        //float spinAngleDelta = 0.125f * PI * Time.deltaTime;
        float deltaTime = Time.deltaTime;
        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += rootPart.spinVelocity * deltaTime;

        
        //rootPart.rotation *= deltaRotation;
        //rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation = mul(transform.rotation,
             mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle))
         );
        rootPart.worldPosition = transform.position;
        parts[0][0] = rootPart;
        float objectScale = transform.lossyScale.x;
        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
        matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);

        float scale = objectScale;
        JobHandle jobHandle = default;
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            jobHandle = new UpdateFractalLevelJob
            {
                //spinAngleDelta = spinAngleDelta,
                deltaTime = deltaTime,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.ScheduleParallel(parts[li].Length, 5, jobHandle);
            //NativeArray<FractalPart> parentParts = parts[li - 1];
            //NativeArray<FractalPart> levelParts = parts[li];
            //NativeArray<Matrix4x4> levelMatrices = matrices[li];
            //for (int fpi = 0; fpi < parts[li].Length; fpi++) {
            //	job.Execute(fpi);
            //}
        }
        jobHandle.Complete();

        var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        int leafIndex = matricesBuffers.Length - 1;
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            Color colorA, colorB;
            Mesh instanceMesh;
            if (i == leafIndex)
            {
                colorA = leafColorA;
                colorB = leafColorB;
                instanceMesh = leafMesh;
            }
            else
            {
                float gradientInterpolator = i / (matricesBuffers.Length - 2f);
                colorA = gradientA.Evaluate(gradientInterpolator);
                colorB = gradientB.Evaluate(gradientInterpolator);
                instanceMesh = mesh;
            }
            propertyBlock.SetColor(colorAId, colorA);
            propertyBlock.SetColor(colorBId, colorB);
            propertyBlock.SetBuffer(matricesId, buffer);
            propertyBlock.SetVector(sequenceNumbersId, sequenceNumbers[i]);
            Graphics.DrawMeshInstancedProcedural(
                instanceMesh, 0, material, bounds, buffer.count, propertyBlock
            );
        }
    }

    struct FractalPart
    {
        public Vector3 worldPosition;
        public Quaternion rotation, worldRotation;
        public float maxSagAngle, spinAngle, spinVelocity;
        //public Transform transform;
    }
    //FractalPart[][] parts;

    //Matrix4x4[][] matrices;
}
