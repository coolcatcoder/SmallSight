using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;

//public partial class MapMaker : SystemBase
[BurstCompile]
public partial struct MapMaker : ISystem
{
    //public static Entity ChunkHolder;
    //public static int ChunkSize;
    //public int QueueSize;

    //public static NativeList<int2> ChunksToGenerate;
    //public static NativeList<int2> ChunksToUnload;
    //public static NativeList<int2> ChunksToLoad;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //ChunkSize = 10;
        //QueueSize = 100;

        //ChunksToGenerate = new NativeList<int2>(QueueSize, Allocator.Persistent);
        //ChunksToUnload = new NativeList<int2>(QueueSize, Allocator.Persistent);
        //ChunksToLoad = new NativeList<int2>(QueueSize, Allocator.Persistent);

        state.RequireForUpdate<ChunkMaster>();
    }

    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        ref ChunkMaster MapInfo = ref SystemAPI.GetSingletonRW<ChunkMaster>().ValueRW;

        MapInfo.Seed = (uint)UnityEngine.Random.Range(0, MapInfo.MaxSeed);

        MapInfo.RandStruct = new Unity.Mathematics.Random(MapInfo.Seed);

        MapInfo.BiomeSeed = MapInfo.RandStruct.NextFloat3(MapInfo.MinBiomeSeed, MapInfo.MaxBiomeSeed);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ref ChunkMaster MapInfo = ref SystemAPI.GetSingletonRW<ChunkMaster>().ValueRW;

        //code here
        for (int i = 0; i < MapInfo.ChunksToGenerate.Length; i++)
        {
            GenerateChunk(MapInfo.ChunksToGenerate[i], ref MapInfo, ref state);
        }

        for (int i = 0; i < MapInfo.ChunksToUnload.Length; i++)
        {
            UnloadChunk(MapInfo.ChunksToUnload[i], ref MapInfo, ref state);
        }

        for (int i = 0; i < MapInfo.ChunksToLoad.Length; i++)
        {
            LoadChunk(MapInfo.ChunksToLoad[i], ref MapInfo, ref state);
        }

        MapInfo.ChunksToGenerate.Clear();
        MapInfo.ChunksToUnload.Clear();
        MapInfo.ChunksToLoad.Clear();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [BurstCompile]
    public void GenerateChunk(int2 Chunk, ref ChunkMaster MapInfo, ref SystemState state)
    {
        if (MapInfo.Chunks.ContainsKey(Chunk))
        {
            return;
        }
        Entity ChunkEntity = state.EntityManager.CreateEntity();
        //state.EntityManager.SetName(ChunkEntity, "Chunk(" + Chunk.x + "," + Chunk.y + ")"); Not burst compatible...
        MapInfo.Chunks.Add(Chunk, ChunkEntity);

        int3 ChunkCentre = new int3(Chunk.x * MapInfo.ChunkSize + (MapInfo.ChunkSize / 2), -1, Chunk.y * MapInfo.ChunkSize + (MapInfo.ChunkSize / 2));
        //int3 ChunkMaxCorner = new int3(ChunkCentre.x + (ChunkSize / 2), -1, ChunkCentre.z + (ChunkSize / 2));
        int3 ChunkMinCorner = new int3(ChunkCentre.x - (MapInfo.ChunkSize / 2), -1, ChunkCentre.z - (MapInfo.ChunkSize / 2));

        for (int x = 0; x < MapInfo.ChunkSize; x++)
        {
            for (int z = 0; z < MapInfo.ChunkSize; z++)
            {
                int3 WorldPos = new int3(x + ChunkMinCorner.x, -1, z + ChunkMinCorner.z);
                BiomeData Biome = CalculateBiome(WorldPos, MapInfo, ref state);
            }
        }
    }

    [BurstCompile]
    public void UnloadChunk(int2 Chunk, ref ChunkMaster MapInfo, ref SystemState state)
    {
        if (MapInfo.Chunks.TryGetValue(Chunk, out Entity ChunkEntity))
        {
            if (!state.EntityManager.HasComponent<Disabled>(ChunkEntity))
            {
                state.EntityManager.AddComponent<Disabled>(ChunkEntity);
            }
        }
    }

    [BurstCompile]
    public void LoadChunk(int2 Chunk, ref ChunkMaster MapInfo, ref SystemState state)
    {
        if (MapInfo.Chunks.TryGetValue(Chunk, out Entity ChunkEntity))
        {
            if (state.EntityManager.HasComponent<Disabled>(ChunkEntity))
            {
                state.EntityManager.RemoveComponent<Disabled>(ChunkEntity);
            }
        }
    }

    [BurstCompile]
    public BiomeData CalculateBiome(float3 Pos, ChunkMaster MapInfo, ref SystemState state)
    {
        float3 SeededPos1 = Pos;
        SeededPos1.x += MapInfo.BiomeSeed.x;

        float3 SeededPos2 = Pos;
        SeededPos1.x += MapInfo.BiomeSeed.y;

        float3 SeededPos3 = Pos;
        SeededPos3.x += MapInfo.BiomeSeed.z;

        float3 CurrentBiomeNoise = new float3(noise.snoise(SeededPos1.xz * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos2.xz * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos3.xz * MapInfo.BiomeNoiseScale));

        BiomeJob CurrentBiomeJob = new BiomeJob
        {
            BiomeNoise = CurrentBiomeNoise
        };

        CurrentBiomeJob.Run();

        var CurrentBiome = CurrentBiomeJob.CurrentBiome.Value;
        CurrentBiomeJob.CurrentBiome.Dispose();

        return CurrentBiome;
    }

    //    [BurstCompile]
    //    public static bool IsSafe(int3 Pos, int MaxDangerLevel, ref SystemState state)
    //    {
    //        Entity Chunk = GetChunkEntity(GetChunkNum(Pos), ref state);

    //        if (Chunk==null)
    //        {
    //            Debug.Log("Couldn't get chunk entity, assuming safe!");
    //            return true;
    //        }

    //        ChunkData CD = SystemAPI.GetComponent<ChunkData>(Chunk);

    //        for (int i = 0; i < CD.StuffInChunk.Length; i++)
    //        {
    //            if (CD.StuffInChunk[i].Danger > MaxDangerLevel && math.all(Pos == (int3)SystemAPI.GetComponent<LocalTransform>(CD.StuffInChunk[i].Thing).Position))
    //            {
    //                return false;
    //            }
    //        }

    //        return true;
    //    }

    //    [BurstCompile]
    //    public static int2 GetChunkNum(int3 Pos)
    //    {
    //        return new int2((int)math.floor(Pos.x / ChunkSize), (int)math.floor(Pos.z / ChunkSize));
    //    }

    //    [BurstCompile]
    //    public static Entity GetChunkEntity(int2 ChunkNum, ref SystemState state)
    //    {
    //        ChunkMaster CurrentChunkMaster = SystemAPI.GetComponent<ChunkMaster>(ChunkHolder);

    //        CurrentChunkMaster.Chunks.TryGetValue(ChunkNum, out Entity CurrentChunk);

    //        return CurrentChunk;
    //    }

    //    public static bool IsChunkGenerated(int2 ChunkNum, ref SystemState state)
    //    {
    //        return SystemAPI.GetComponent<ChunkMaster>(ChunkHolder).Chunks.TryGetValue(ChunkNum, out _);
    //    }

    //    public static bool IsChunkLoaded(Entity Chunk, ref SystemState state)
    //    {
    //        return !SystemAPI.HasComponent<Disabled>(Chunk);
    //    }

    //    public static void UnloadChunk(Entity Chunk, ref SystemState state)
    //    {
    //        if (IsChunkLoaded(Chunk, ref state))
    //        {
    //            state.EntityManager.AddComponent<Disabled>(Chunk);
    //        }
    //    }

    //    public static void LoadChunk(Entity Chunk, ref SystemState state)
    //    {
    //        if (!IsChunkLoaded(Chunk, ref state))
    //        {
    //            state.EntityManager.RemoveComponent<Disabled>(Chunk);
    //        }
    //    }

    //    public static void GenerateChunk(int2 ChunkNum)
    //    {
    //        //unfinished
    //    }
}

[BurstCompile]
public struct BiomeJob : IJobEntity
{
    public float3 BiomeNoise;
    public NativeReference<BiomeData> CurrentBiome;

    [BurstCompile]
    void Execute(ref BiomeData Biome)
    {
        if (math.all(BiomeNoise >= Biome.MinNoiseValues) && math.all(BiomeNoise < Biome.MaxNoiseValues))
        {
            CurrentBiome = new NativeReference<BiomeData>(Biome,Allocator.TempJob);
            //break; Removed?
        }
    }
}
