using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MapMaker : MonoBehaviour
{
    public int MaxBlocks;
    public uint MaxSeed;

    public float3 MinBiomeSeed;
    public float3 MaxBiomeSeed;

    public float BiomeNoiseScale;

    public float TerrainNoiseScale;

    public int3 MaxTeleportBounds;
    public int3 MinTeleportBounds;
}

public class MapMakerBaker : Baker<MapMaker>
{
    public override void Bake(MapMaker authoring)
    {
        AddComponent(new MapData
        {
            GeneratedBlocks = new NativeHashMap<Unity.Mathematics.int2, Entity>(authoring.MaxBlocks, Allocator.Persistent),
            MaxSeed = authoring.MaxSeed,
            MinBiomeSeed = authoring.MinBiomeSeed,
            MaxBiomeSeed = authoring.MaxBiomeSeed,
            BiomeNoiseScale = authoring.BiomeNoiseScale,
            TerrainNoiseScale = authoring.TerrainNoiseScale,
            MaxTeleportBounds = authoring.MaxTeleportBounds,
            MinTeleportBounds = authoring.MinTeleportBounds,
        });
    }
}

public struct MapData : IComponentData
{
    public NativeHashMap<int2, Entity> GeneratedBlocks;

    public uint Seed;
    public uint MaxSeed;

    public float3 MinBiomeSeed;
    public float3 MaxBiomeSeed;

    public float3 BiomeSeed;
    public float BiomeNoiseScale;

    public float TerrainNoiseScale;

    public int3 MaxTeleportBounds;
    public int3 MinTeleportBounds;

    public Unity.Mathematics.Random RandStruct;

    public bool RestartGame;
}

public static class MapExtensionMethods
{
    public static void RandomiseSeeds(this MapData MapInfo)
    {
        MapInfo.Seed = (uint)UnityEngine.Random.Range(0, MapInfo.MaxSeed);
        MapInfo.RandStruct = Unity.Mathematics.Random.CreateFromIndex(MapInfo.Seed);
        MapInfo.BiomeSeed = MapInfo.RandStruct.NextFloat3(MapInfo.MinBiomeSeed, MapInfo.MaxBiomeSeed);
    }

    [BurstCompile]
    public static Color GetBiomeColour(this MapData MapInfo, int2 Pos)
    {
        float2 SeededPos1 = Pos;
        SeededPos1.x += MapInfo.BiomeSeed.x;

        float2 SeededPos2 = Pos;
        SeededPos2.x += MapInfo.BiomeSeed.y;

        float2 SeededPos3 = Pos;
        SeededPos3.x += MapInfo.BiomeSeed.z;

        float3 CurrentBiomeNoise = new float3(noise.snoise(SeededPos1 * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos2 * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos3 * MapInfo.BiomeNoiseScale));

        return new Color(
                (CurrentBiomeNoise.x + 1) / 2,
                (CurrentBiomeNoise.y + 1) / 2,
                (CurrentBiomeNoise.z + 1) / 2
                );
    }
}

public partial struct MapSystem : ISystem, ISystemStartStop
{
    public void OnCreate(ref SystemState state)
    {
        
    }

    public void OnStartRunning(ref SystemState state)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
        MapInfo.RandomiseSeeds();
    }

    public void OnUpdate(ref SystemState state)
    {
        
    }

    public void OnStopRunning(ref SystemState state)
    {

    }

    public void OnDestroy(ref SystemState state)
    {

    }

    public void GenerateBlock(int2 Pos, ref MapData MapInfo, ref SystemState state)
    {
        if (MapInfo.GeneratedBlocks.ContainsKey(Pos))
        {
            return;
        }
    }
}
