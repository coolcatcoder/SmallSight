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

    public Color ColourSpawn;
    public float MaxDistance;
}

public class BiomeBaker : Baker<BiomeAuthoring>
{
    public override void Bake(BiomeAuthoring authoring)
    {
        if (authoring.Features != null)
        {
            AddComponent(new BiomeData
            {
                BiomeName = new NativeText(authoring.BiomeName, Allocator.Persistent),
                //Features = ConvertedFeatures,
                MinNoiseValues = authoring.MinNoiseValues,
                MaxNoiseValues = authoring.MaxNoiseValues,
                ExtraTerrainNoiseScale = authoring.ExtraTerrainNoiseScale,
                TeleportSafe = authoring.TeleportSafe,

                ColourSpawn = authoring.ColourSpawn,
                MaxDistance = authoring.MaxDistance
            });

            var FeatureBuffer = AddBuffer<BiomeFeature>();
            for (int i = 0; i < authoring.Features.Length; i++)
            {
                FeatureBuffer.Add(new BiomeFeature
                {
                    FeaturePrefab = GetEntity(authoring.Features[i].FeaturePrefab),
                    PercentChanceToSpawn = authoring.Features[i].PercentChanceToSpawn,
                    Danger = authoring.Features[i].Danger,
                    IsTerrain = authoring.Features[i].IsTerrain,
                    MinNoiseValue = authoring.Features[i].MinNoiseValue,
                    MaxNoiseValue = authoring.Features[i].MaxNoiseValue
                });
            }

        }
        else
        {
            Debug.Log("Features is null?????");
        }
    }
}

public struct BiomeData : IComponentData
{
    public NativeText BiomeName;
    public float3 MinNoiseValues;
    public float3 MaxNoiseValues;
    public float ExtraTerrainNoiseScale;
    public bool TeleportSafe;

    public Color ColourSpawn;
    public float MaxDistance;
}

[InternalBufferCapacity(0)]
public struct BiomeFeature : IBufferElementData
{
    public Entity FeaturePrefab;
    public float PercentChanceToSpawn;
    public int Danger;
    public bool IsTerrain;
    public float MinNoiseValue;
    public float MaxNoiseValue;
}

//[System.Serializable]
//public struct EFeature
//{
//    public Entity FeaturePrefab;
//    public float PercentChanceToSpawn;
//    public int Danger;
//    public bool IsTerrain;
//    public float MinNoiseValue;
//    public float MaxNoiseValue;
//}

[System.Serializable]
public struct GFeature
{
    public GameObject FeaturePrefab;
    public float PercentChanceToSpawn;
    public int Danger;
    public bool IsTerrain;
    public float MinNoiseValue;
    public float MaxNoiseValue;
}
