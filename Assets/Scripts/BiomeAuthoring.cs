using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

public class BiomeAuthoring : MonoBehaviour
{
    public string BiomeName;
    public GFeature[] Features;
    public float3 MinNoiseValues;
    public float3 MaxNoiseValues;
    public float ExtraTerrainNoiseScale;
    public bool TeleportSafe;
}

public class BiomeBaker : Baker<BiomeAuthoring>
{
    public override void Bake(BiomeAuthoring authoring)
    {
        if (authoring.Features != null)
        {
            NativeArray<EFeature> ConvertedFeatures = new NativeArray<EFeature>(authoring.Features.Length, Allocator.Persistent);

            for (int i = 0; i < authoring.Features.Length; i++)
            {
                ConvertedFeatures[i] = new EFeature
                {
                    FeaturePrefab = GetEntity(authoring.Features[i].FeaturePrefab),
                    PercentChanceToSpawn = authoring.Features[i].PercentChanceToSpawn,
                    Danger = authoring.Features[i].Danger,
                    IsTerrain = authoring.Features[i].IsTerrain
                };
            }

            AddComponent(new BiomeData
            {
                BiomeName = new NativeText(authoring.BiomeName, Allocator.Persistent),
                Features = ConvertedFeatures, //does this neeed to be disposed?
                MinNoiseValues = authoring.MinNoiseValues,
                MaxNoiseValues = authoring.MaxNoiseValues,
                ExtraTerrainNoiseScale = authoring.ExtraTerrainNoiseScale,
                TeleportSafe = authoring.TeleportSafe
            });
        }
        else
        {
            Debug.Log("Features is null?????");
        }
    }
}

[System.Serializable]
public struct GFeature
{
    public GameObject FeaturePrefab;
    public float PercentChanceToSpawn;
    public int Danger;
    public bool IsTerrain;
}
