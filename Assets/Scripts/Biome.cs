using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class Biome : MonoBehaviour
{
    public string BiomeName;
    public int WorldIndex = 0;
    public BiomeAuthoringFeature[] Features;
    public float ExtraTerrainNoiseScale;

    public Color ColourSpawn;
    public float MaxDistance;
}

public class BiomeBaker : Baker<Biome>
{
    public override void Bake(Biome authoring)
    {
        if (authoring.Features != null)
        {
            AddComponent(new BiomeData
            {
                BiomeName = authoring.BiomeName,
                ExtraTerrainNoiseScale = authoring.ExtraTerrainNoiseScale,
                ColourSpawn = authoring.ColourSpawn,
                MaxDistance = authoring.MaxDistance,
                WorldIndex = authoring.WorldIndex
            });

            var FeatureBuffer = AddBuffer<BiomeFeature>();
            for (int i = 0; i < authoring.Features.Length; i++)
            {
                FeatureBuffer.Add(new BiomeFeature
                {
                    FeaturePrefab = GetEntity(authoring.Features[i].FeaturePrefab),
                    PercentChanceToSpawn = authoring.Features[i].PercentChanceToSpawn,
                    //Danger = authoring.Features[i].Danger,
                    IsTerrain = authoring.Features[i].IsTerrain,
                    MinNoiseValue = authoring.Features[i].MinNoiseValue,
                    MaxNoiseValue = authoring.Features[i].MaxNoiseValue
                });
            }
        }
    }
}

public struct BiomeData : IComponentData
{
    public FixedString128Bytes BiomeName;
    public int WorldIndex;
    public float ExtraTerrainNoiseScale;

    public Color ColourSpawn;
    public float MaxDistance;
}

[InternalBufferCapacity(0)]
public struct BiomeFeature : IBufferElementData
{
    public Entity FeaturePrefab;
    public float PercentChanceToSpawn;
    //public int Danger;
    public bool IsTerrain;
    public float MinNoiseValue;
    public float MaxNoiseValue;
}

[System.Serializable]
public struct BiomeAuthoringFeature
{
    public GameObject FeaturePrefab;
    public float PercentChanceToSpawn;
    //public int Danger;
    public bool IsTerrain;
    public float MinNoiseValue;
    public float MaxNoiseValue;
}