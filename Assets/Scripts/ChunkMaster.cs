using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ChunkMaster : IComponentData
{
    public NativeHashMap<int2, Entity> Chunks;
    //public int ChunkSize; Scrapped for now!

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

    public NativeList<int2> ChunksToGenerate;
    public NativeList<int2> ChunksToUnload;
    public NativeList<int2> ChunksToLoad;
}
