using UnityEngine;

public class MathUtils
{

    public static float ViscositySmoothingKernal(float radius, float dist)
    {
        if (dist >= radius) return 0;

        float volume = Mathf.PI * Mathf.Pow(radius, 8) / 4;
        float value = Mathf.Max(0, radius * radius - dist * dist);
        return value * value * value / volume;
    }
    
    public static float SmoothingKernel(float radius, float dist)
    {
        if (dist >= radius) return 0;

        float volume = Mathf.PI * Mathf.Pow(radius, 4) / 6; // pre-calculated volume of smoothing function to normalize density
        return (radius - dist) * (radius - dist) / volume;
    }

    public static float SmoothingKernalDerivative(float radius, float dist)
    {
        if (dist >= radius) return 0;

        float scale = 12 / (Mathf.Pow(radius, 4) * Mathf.PI);
        return (dist - radius) * scale;
    }

    public static float ConvertRange(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        return ((value - oldMin) * (newMax - newMin) / (oldMax - oldMin)) + newMin;
    }

}
