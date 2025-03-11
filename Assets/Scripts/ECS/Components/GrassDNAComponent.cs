using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// New component that consolidates all grass parameters
public struct GrassDNAComponent : IComponentData
{
    public Entity Prefab;

    // Growing parameters
    public float3 MinSize;
    public float3 MaxSize;
    public float MaxWholeness;
    public float GrowthSpeed;

    // Aging parameters
    public float AgingFunctionSpan;
    public float AgingFunctionHeight;

    // Edible parameters
    public float MaxNutrition;

    // Reproduction parameters
    public float ReproductionFunctionFactor;
    public float ReproductionFunctionHeight;
    public float ReproductionInterval;

    // Random seed for this grass
    public uint RandomSeed;
}
