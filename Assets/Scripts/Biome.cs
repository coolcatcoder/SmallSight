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
        var entity = GetEntity(TransformUsageFlags.None);
        if (authoring.Features != null)
        {
            AddComponent(entity, new BiomeData
            {
                BiomeName = authoring.BiomeName,
                ExtraTerrainNoiseScale = authoring.ExtraTerrainNoiseScale,
                ColourSpawn = new float3(authoring.ColourSpawn.r,authoring.ColourSpawn.g,authoring.ColourSpawn.b)*2-1,
                MaxDistance = authoring.MaxDistance*authoring.MaxDistance,
                WorldIndex = authoring.WorldIndex,

                //BiomeFeaturesBlobAsset = blobReference,

                NotNull = true,

                BiomeRandom = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Random.Range(0, 3000)) // is 3001 seeds enough?
            });

            var FeatureBuffer = AddBuffer<BiomeFeatureElement>(entity);
            for (int i = 0; i < authoring.Features.Length; i++)
            {
                Block BlockInfo = authoring.Features[i].FeaturePrefab.GetComponent<Block>();

                FeatureBuffer.Add(new BiomeFeatureElement
                {
                    //FeaturePrefab = GetEntity(authoring.Features[i].FeaturePrefab, TransformUsageFlags.Dynamic),

                    PercentChanceToSpawn = authoring.Features[i].PercentChanceToSpawn,
                    IsTerrain = authoring.Features[i].IsTerrain,
                    MinNoiseValue = authoring.Features[i].MinNoiseValue,
                    MaxNoiseValue = authoring.Features[i].MaxNoiseValue,

                    TypeIndex = BlockInfo.UniqueIndex
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

    public float3 ColourSpawn;
    public float MaxDistance;

    public bool NotNull;

    public Unity.Mathematics.Random BiomeRandom;
}

[InternalBufferCapacity(0)]
public struct BiomeFeatureElement : IBufferElementData
{
    //public Entity FeaturePrefab;
    public float PercentChanceToSpawn;
    //public int Danger;
    public bool IsTerrain;
    public float MinNoiseValue;
    public float MaxNoiseValue;

    public int TypeIndex;
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