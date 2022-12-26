using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct BiomeData : IComponentData
{
    public NativeText BiomeName;
    public NativeArray<EFeature> Features;
    public float3 MinNoiseValues;
    public float3 MaxNoiseValues;
    public float ExtraTerrainNoiseScale;
    public bool TeleportSafe;
}

public struct EFeature
{
    public Entity FeaturePrefab;
    public float PercentChanceToSpawn;
    public int Danger;
    public bool IsTerrain;
}