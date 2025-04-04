using Unity.Mathematics;
using Unity.Transforms;

public static class MathExtentions
{

    public static bool IsBeating(ref Random random, float chance)
    {
        return random.NextFloat(0, 1) <= chance;
    }

    public static float GetRandom100(ref Random random)
    {
        return random.NextFloat(0, 100);
    }

    public static float GetRandomVariation(ref Random random, float oldValue, float2 deviation)
    {
        return random.NextFloat(deviation.x, deviation.y) * oldValue;
    }

    public static int GetRandomVariation(ref Random random, int2 valueRange)
    {
        return random.NextInt(valueRange.x, valueRange.y);
    }

    public static float2 GetRandomVariationWithDeviation(ref Random random, float2 oldValue, float2 deviation)
    {
        return random.NextFloat(deviation.x, deviation.y) * oldValue;
    }

    public static float GetRandomVariation(ref Random random, float2 minMax)
    {
        return random.NextFloat(minMax.x, minMax.y);
    }

    public static float2 GetRandomVariation(ref Random random, float2 min, float2 max)
    {
        var x = random.NextFloat(min.x, min.y);
        var y = random.NextFloat(max.x, max.y);
        return new float2(math.min(x, y), math.max(x, y));
    }
}
