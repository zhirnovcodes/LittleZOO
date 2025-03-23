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
    public float MinSize;
    public float MaxSize;
    public float MinWholeness;
    public float MaxWholeness;
    public float GrowthSpeed;

    // Aging parameters
    public float AgingFunctionSpan;
    public float AgingFunctionHeight;

    // Edible parameters
    public float MaxNutrition;

    // Reproduction parameters
    public float ReproductionFunctionSpan;
    public float ReproductionFunctionHeight;
    public float ReproductionInterval;
    public float ReproductiveChance;

    // Advertising values
    public float2 AdvertisedFullness;
    public float2 AdvertisedEnergy;
}
