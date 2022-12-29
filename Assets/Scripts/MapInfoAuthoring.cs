using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MapInfoAuthoring : MonoBehaviour
{
    public int MaxChunks = 100000;
    public uint MaxSeed;

    public float3 MinBiomeSeed;
    public float3 MaxBiomeSeed;

    public float BiomeNoiseScale;

    public float TerrainNoiseScale;

    public int3 MaxTeleportBounds;
    public int3 MinTeleportBounds;
}

class MapInfoBaker : Baker<MapInfoAuthoring>
{
    public override void Bake(MapInfoAuthoring authoring)
    {
        AddComponent(new ChunkMaster
        {
            Chunks = new NativeHashMap<Unity.Mathematics.int2, Entity>(authoring.MaxChunks, Allocator.Persistent),
            MaxSeed = authoring.MaxSeed,
            MinBiomeSeed = authoring.MinBiomeSeed,
            MaxBiomeSeed = authoring.MaxBiomeSeed,
            BiomeNoiseScale = authoring.BiomeNoiseScale,
            TerrainNoiseScale = authoring.TerrainNoiseScale,
            MaxTeleportBounds = authoring.MaxTeleportBounds,
            MinTeleportBounds = authoring.MinTeleportBounds,
            ChunksToGenerate = new NativeList<int2>(500, Allocator.Persistent),
            ChunksToUnload = new NativeList<int2>(500, Allocator.Persistent),
            ChunksToLoad = new NativeList<int2>(500, Allocator.Persistent)
        });
    }
}
