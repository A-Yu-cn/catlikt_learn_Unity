using UnityEngine;
using static UnityEngine.Mathf;
public static class FunctionLibrary {

    public static float Wave(float x, float z, float t) {
        return Sin(PI * (x + t));
        // return Mathf.Sin(Mathf.PI * (x + t));  使用了using static UnityEngine.Mathf; 无需显式说明方法和变量的类型
    }

    public static float MultiWave(float x, float z, float t)
    {
        float y = Sin(PI * (x + 0.5f * t));
        // y += Sin(2f * PI * (x + t)) / 2f;
        // return y / 1.5f;    // 使用除法在复杂计算的地方开销比乘法大，而简单的 1f/2f这样的常量表达式可以编译简化，而2/3无法十进制表示因此还是保持除法形式让编译器优化
        y += Sin(2f * PI * (x + t)) * (1f / 2f);
        return y * (2f / 3f);
    }

    public static float Ripple(float x, float z, float t)
    {
        float d = Abs(x);
        float y = Sin(4f * PI * d - t);
        return y / (1f + 10f * d);
    }

    public delegate float Function(float x, float z, float t);

    public enum FunctionName { Wave, MultiWave, Ripple }
    
    static Function[] functions = { Wave, MultiWave, Ripple };

    public static Function GetFunction(FunctionName name)
    {
        return functions[(int)name];
    }
}