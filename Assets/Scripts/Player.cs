using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using System;
using Unity.Burst;

public partial class Player : SystemBase
{
    protected override void OnStartRunning()
    {
        //MapSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MapMaker>();
    }

    protected override void OnUpdate()
    {
        ref var PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        Entity PlayerEntity = SystemAPI.GetSingletonEntity<PlayerData>();
        ref var MapInfo = ref SystemAPI.GetSingletonRW<ChunkMaster>().ValueRW;
        LocalTransform PlayerTransform = SystemAPI.GetComponent<LocalTransform>(PlayerEntity);

        if (PlayerInfo.DebugDrag)
        {
            MapInfo.ChunksToGenerate.Add(GetChunkNum(PlayerTransform.Position, MapInfo.ChunkSize));

            PlayerInfo.DebugChunkColour = CalculateBiomeColour(PlayerTransform.Position, ref MapInfo);
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.canceled || context.started)
        {
            return;
        }

        EntityQuery PlayerQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<PlayerData>()
            .WithAllRW<LocalTransform>()
            .Build(this);

        Entity PlayerEntity = PlayerQuery.ToEntityArray(Allocator.Temp)[0];

        var PTransform = SystemAPI.GetComponent<LocalTransform>(PlayerEntity);
        //var PTransform = SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(PlayerEntity, false).ValueRW;
        var PData = SystemAPI.GetComponent<PlayerData>(PlayerEntity);

        float3 NewPos = PTransform.Position;

        float2 Movement = context.ReadValue<Vector2>();

        if (Movement.x >= PData.MovementThreshold)
        {
            NewPos.x += 1;
        }
        else if (Movement.x <= -PData.MovementThreshold)
        {
            NewPos.x -= 1;
        }

        if (Movement.y >= PData.MovementThreshold)
        {
            NewPos.z += 1;
        }
        else if (Movement.y <= -PData.MovementThreshold)
        {
            NewPos.z -= 1;
        }

        //Debug.Log(NewPos);

        if (math.all(NewPos == PTransform.Position))
        {
            return;
        }

        ref ChunkMaster MapInfo = ref SystemAPI.GetSingletonRW<ChunkMaster>().ValueRW;

        if (!IsSafe((int3)NewPos, SystemAPI.GetSingleton<PlayerData>().MaxDanger, ref MapInfo))
        {
            return;
        }

        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(PlayerEntity, false).ValueRW.Position = NewPos;

        var NewCamPos = NewPos;
        NewCamPos.y = 5;

        SystemAPI.GetComponentLookup<CameraData>().GetRefRW(PlayerEntity, false).ValueRW.Pos = (int3)NewCamPos;

        MapInfo.ChunksToGenerate.Add(GetChunkNum(NewPos, MapInfo.ChunkSize));
        Debug.Log(GetChunkNum(NewPos, MapInfo.ChunkSize));

        SystemAPI.GetSingletonRW<PlayerData>().ValueRW.DebugChunkColour = CalculateBiomeColour(NewPos, ref MapInfo);

        DevourBlocks((int3)NewPos, ref MapInfo);
    }

    [BurstCompile]
    public static int2 GetChunkNum(float3 Pos, float ChunkSize)
    {
        //return new int2(Mathf.FloorToInt(Pos.x / ChunkSize), Mathf.FloorToInt(Pos.z / ChunkSize));
        return new int2((int)math.floor(Pos.x / ChunkSize), (int)math.floor(Pos.z / ChunkSize));
    }

    [BurstCompile]
    public Color CalculateBiomeColour(float3 Pos, ref ChunkMaster MapInfo)
    {
        // Biomes are calculated based on 3 channels of perlin noise (can be treated as rgb should you want to visulize it)

        float3 SeededPos1 = Pos;
        SeededPos1.x += MapInfo.BiomeSeed.x;

        float3 SeededPos2 = Pos;
        SeededPos1.x += MapInfo.BiomeSeed.y;

        float3 SeededPos3 = Pos;
        SeededPos3.x += MapInfo.BiomeSeed.z;

        float3 CurrentBiomeNoise = new float3(noise.snoise(SeededPos1.xz * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos2.xz * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos3.xz * MapInfo.BiomeNoiseScale));

        return new Color(
                (CurrentBiomeNoise.x + 1) / 2,
                (CurrentBiomeNoise.y + 1) / 2,
                (CurrentBiomeNoise.z + 1) / 2
                );
    }

    [BurstCompile]
    public bool IsSafe(int3 Pos, int MaxDangerLevel, ref ChunkMaster MapInfo)
    {
        if(!MapInfo.Chunks.TryGetValue(GetChunkNum(Pos, MapInfo.ChunkSize), out Entity ChunkEntity))
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

    [BurstCompile]
    public void DevourBlocks(int3 Pos, ref ChunkMaster MapInfo)
    {
        if (!MapInfo.Chunks.TryGetValue(GetChunkNum(Pos, MapInfo.ChunkSize), out Entity ChunkEntity))
        {
            Debug.Log("Chunk isnt real?");
            return;
        }

        if (ChunkEntity == Entity.Null)
        {
            Debug.Log("Couldn't get chunk entity?");
            return;
        }

        DynamicBuffer<EntityHerd> StuffInChunk = SystemAPI.GetBuffer<EntityHerd>(ChunkEntity);

        for (int i = 0; i < StuffInChunk.Length; i++)
        {
            if (math.all(Pos.xz == (int2)SystemAPI.GetComponent<LocalTransform>(StuffInChunk[i].Block).Position.xz))
            {
                StuffInChunk = SystemAPI.GetBuffer<EntityHerd>(ChunkEntity);
                Entity EntityToRemove = StuffInChunk[i].Block;
                World.EntityManager.DestroyEntity(EntityToRemove);
                StuffInChunk = SystemAPI.GetBuffer<EntityHerd>(ChunkEntity);
                StuffInChunk.RemoveAt(i);
                StuffInChunk = SystemAPI.GetBuffer<EntityHerd>(ChunkEntity);
            }
        }
    }
}
