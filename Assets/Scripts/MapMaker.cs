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
public partial struct MapMaker : ISystem, ISystemStartStop
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ChunkMaster>();
    }

    //[BurstCompile] ahhhh
    public void OnStartRunning(ref SystemState state)
    {
        ref ChunkMaster MapInfo = ref SystemAPI.GetSingletonRW<ChunkMaster>().ValueRW;

        //MapInfo.Seed = (uint)UnityEngine.Random.Range(0, MapInfo.MaxSeed);

        //MapInfo.RandStruct = Unity.Mathematics.Random.CreateFromIndex(MapInfo.Seed);

        //MapInfo.BiomeSeed = MapInfo.RandStruct.NextFloat3(MapInfo.MinBiomeSeed, MapInfo.MaxBiomeSeed);

        MapInfo.RandomiseSeeds();

        state.EntityManager.AddComponent<FinishedGenerating>(SystemAPI.GetSingletonEntity<ChunkMaster>());

        FindSafePlayerSpawn(ref state);
    }

    public void FindSafePlayerSpawn(ref SystemState state)
    {
        ref var PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        Entity PlayerEntity = SystemAPI.GetSingletonEntity<PlayerData>();
        ref var MapInfo = ref SystemAPI.GetSingletonRW<ChunkMaster>().ValueRW;
        ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(PlayerEntity, false).ValueRW;
        ref CameraData Cam = ref SystemAPI.GetSingletonRW<CameraData>().ValueRW;

        GenerateChunk(GetChunkNum(PlayerTransform.Position, MapInfo.ChunkSize), ref MapInfo, ref state);

        while (!IsSafe((int3)PlayerTransform.Position, -1, ref MapInfo, ref state))
        {
            PlayerTransform.Position = MapInfo.RandStruct.NextInt3(MapInfo.MinTeleportBounds, MapInfo.MaxTeleportBounds);
            PlayerTransform.Position.y = 0;
            Cam.Pos = (int3)PlayerTransform.Position;
            Cam.Pos.y = 5;
            GenerateChunk(GetChunkNum(PlayerTransform.Position, MapInfo.ChunkSize), ref MapInfo, ref state);
        }
    }

    //yoinked from player
    public bool IsSafe(int3 Pos, int MaxDangerLevel, ref ChunkMaster MapInfo, ref SystemState state)
    {
        if (!MapInfo.Chunks.TryGetValue(GetChunkNum(Pos, MapInfo.ChunkSize), out Entity ChunkEntity))
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

    [BurstCompile] //yoinked from player
    public int2 GetChunkNum(float3 Pos, float ChunkSize)
    {
        //return new int2(Mathf.FloorToInt(Pos.x / ChunkSize), Mathf.FloorToInt(Pos.z / ChunkSize));
        return new int2((int)math.floor(Pos.x / ChunkSize), (int)math.floor(Pos.z / ChunkSize));
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
    public void OnStopRunning(ref SystemState state)
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
        state.EntityManager.AddBuffer<EntityHerd>(ChunkEntity);
        state.EntityManager.AddComponent<DestroyDuringReset>(ChunkEntity);

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
                Entity BiomeEntity = CalculateBiomeEntity(WorldPos, ref MapInfo, ref state);
                BiomeData Biome = state.EntityManager.GetComponentData<BiomeData>(BiomeEntity);
                DynamicBuffer<BiomeFeature> BiomeFeatures = state.EntityManager.GetBuffer<BiomeFeature>(BiomeEntity);

                var CurrentSeededPos = new float3(WorldPos.x + MapInfo.Seed, WorldPos.y, WorldPos.z);
                float CurrentNoiseValue = noise.snoise(CurrentSeededPos.xz * (MapInfo.TerrainNoiseScale + Biome.ExtraTerrainNoiseScale));

                bool ContainsTerrain = false;

                for (int i = 0; i < BiomeFeatures.Length; i++)
                {
                    BiomeFeatures = state.EntityManager.GetBuffer<BiomeFeature>(BiomeEntity);
                    if (BiomeFeatures[i].IsTerrain && ((CurrentNoiseValue >= BiomeFeatures[i].MinNoiseValue) && (CurrentNoiseValue < BiomeFeatures[i].MaxNoiseValue)))
                    {
                        int Danger = BiomeFeatures[i].Danger;
                        ContainsTerrain = true;
                        Entity BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
                        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = WorldPos;
                        state.EntityManager.AddComponent<DestroyDuringReset>(BlockEntity);

                        SystemAPI.GetBuffer<EntityHerd>(ChunkEntity).Add(new EntityHerd
                        {
                            Block = BlockEntity,
                            Danger = Danger
                        });
                    }
                    BiomeFeatures = state.EntityManager.GetBuffer<BiomeFeature>(BiomeEntity);
                }

                if (!ContainsTerrain) // create plants and stuff now!
                {
                    BiomeFeatures = state.EntityManager.GetBuffer<BiomeFeature>(BiomeEntity);
                    for (int i = 0; i < BiomeFeatures.Length; i++)
                    {
                        BiomeFeatures = state.EntityManager.GetBuffer<BiomeFeature>(BiomeEntity);
                        if ((!BiomeFeatures[i].IsTerrain) && (MapInfo.RandStruct.NextFloat() < BiomeFeatures[i].PercentChanceToSpawn / 100))
                        {
                            int Danger = BiomeFeatures[i].Danger;
                            Entity BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
                            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = WorldPos;
                            state.EntityManager.AddComponent<DestroyDuringReset>(BlockEntity);

                            SystemAPI.GetBuffer<EntityHerd>(ChunkEntity).Add(new EntityHerd
                            {
                                Block = BlockEntity,
                                Danger = Danger
                            });
                        }
                        BiomeFeatures = state.EntityManager.GetBuffer<BiomeFeature>(BiomeEntity);
                    }
                }
            }
        }
    }

    [BurstCompile]
    public void UnloadChunk(int2 Chunk, ref ChunkMaster MapInfo, ref SystemState state) //fix asap
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
    public void LoadChunk(int2 Chunk, ref ChunkMaster MapInfo, ref SystemState state) //fix asap
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
    public Entity CalculateBiomeEntity(float3 Pos, ref ChunkMaster MapInfo, ref SystemState state)
    {
        // Biomes are calculated based on 3 channels of perlin noise (can be treated as rgb should you want to visulize it)

        float3 SeededPos1 = Pos;
        SeededPos1.x += MapInfo.BiomeSeed.x;

        float3 SeededPos2 = Pos;
        SeededPos1.x += MapInfo.BiomeSeed.y;

        float3 SeededPos3 = Pos;
        SeededPos3.x += MapInfo.BiomeSeed.z;

        float3 CurrentBiomeNoise = new float3(noise.snoise(SeededPos1.xz * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos2.xz * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos3.xz * MapInfo.BiomeNoiseScale));

        BiomeJob2 CurrentBiomeJob = new BiomeJob2
        {
            BiomeNoise = CurrentBiomeNoise,
            CurrentBiomeEntity = new NativeReference<Entity>(Allocator.TempJob)
        };

        state.Dependency = CurrentBiomeJob.Schedule(state.Dependency);
        state.Dependency.Complete();
        // the 2 lines of code above are not good for performance

        var CurrentBiomeEntity = CurrentBiomeJob.CurrentBiomeEntity.Value;
        CurrentBiomeJob.CurrentBiomeEntity.Dispose();

        //var CurrentBiome = state.EntityManager.GetComponentData<BiomeData>(CurrentBiomeEntity);

        if (CurrentBiomeEntity == Entity.Null)
        {
            CurrentBiomeEntity = SystemAPI.GetSingletonEntity<DefaultBiomeData>();
        }

        return CurrentBiomeEntity;
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
public partial struct BiomeJob1 : IJobEntity
{
    public float3 BiomeNoise;
    public NativeReference<Entity> CurrentBiomeEntity;

    [BurstCompile]
    void Execute(ref BiomeData Biome, ref DynamicBuffer<BiomeFeature> Features, Entity BiomeEntity)
    {
        if (math.all(BiomeNoise >= Biome.MinNoiseValues) && math.all(BiomeNoise < Biome.MaxNoiseValues))
        {
            CurrentBiomeEntity.Value = BiomeEntity;
            //break; Removed?
        }
    }
}

[BurstCompile]
public partial struct BiomeJob2 : IJobEntity
{
    public float3 BiomeNoise;
    public NativeReference<Entity> CurrentBiomeEntity;

    [BurstCompile]
    void Execute(ref BiomeData Biome, ref DynamicBuffer<BiomeFeature> Features, Entity BiomeEntity)
    {
        //if (math.all(BiomeNoise >= Biome.MinNoiseValues) && math.all(BiomeNoise < Biome.MaxNoiseValues))
        //{
        //    CurrentBiomeEntity.Value = BiomeEntity;
        //    //break; Removed?
        //}
        if (math.distance(new float3(Biome.ColourSpawn.r * 2 - 1, Biome.ColourSpawn.g * 2 - 1, Biome.ColourSpawn.b * 2 - 1), BiomeNoise) <= Biome.MaxDistance/100)
        {
            CurrentBiomeEntity.Value = BiomeEntity;
        }
    }
}

public struct DestroyDuringReset : IComponentData
{

}
