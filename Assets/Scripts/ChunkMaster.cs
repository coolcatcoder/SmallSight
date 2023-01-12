using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
    public bool FindNewSafePosForPlayer; // bit of a mouthfull...

    public int ChunkSize;

    [BurstCompile]
    public int2 GetChunkNum(float3 Pos)
    {
        return new int2((int)math.floor(Pos.x / ChunkSize), (int)math.floor(Pos.z / ChunkSize));
    }

    public void RandomiseSeeds()
    {
        Seed = (uint)UnityEngine.Random.Range(0, MaxSeed);
        RandStruct = Unity.Mathematics.Random.CreateFromIndex(Seed);
        BiomeSeed = RandStruct.NextFloat3(MinBiomeSeed, MaxBiomeSeed);
    }

    [BurstCompile]
    public bool IsSafe(int3 Pos, int MaxDangerLevel, ref SystemState state)
    {
        if (!Chunks.TryGetValue(GetChunkNum(Pos), out Entity ChunkEntity))
        {
            Debug.Log("chunk isnt real?");
            return true;
        }

        if (ChunkEntity == Entity.Null)
        {
            Debug.Log("Couldn't get chunk entity, assuming safe!");
            return true;
        }

        DynamicBuffer<EntityHerd> StuffInChunk = SystemAPI.GetBuffer<EntityHerd>(ChunkEntity);

        for (int i = 0; i < StuffInChunk.Length; i++)
        {
            if (StuffInChunk[i].Danger > MaxDangerLevel && math.all(Pos.xz == (int2)SystemAPI.GetComponent<LocalTransform>(StuffInChunk[i].Block).Position.xz))
            {
                return false;
            }
        }

        return true;
    }
}
