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

public partial struct Player : ISystem, ISystemStartStop
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InputData>();
    }
    public void OnStartRunning(ref SystemState state)
    {

    }

    public void OnUpdate(ref SystemState state)
    {
        ref var PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        Entity PlayerEntity = SystemAPI.GetSingletonEntity<PlayerData>();
        ref var MapInfo = ref SystemAPI.GetSingletonRW<ChunkMaster>().ValueRW;
        LocalTransform PlayerTransform = SystemAPI.GetComponent<LocalTransform>(PlayerEntity);

        if (PlayerInfo.DebugDrag)
        {
            MapInfo.ChunksToGenerate.Add(MapInfo.GetChunkNum(PlayerTransform.Position));

            PlayerInfo.DebugChunkColour = CalculateBiomeColour(PlayerTransform.Position, ref MapInfo);
        }

        Move(ref state);
    }

    public void OnStopRunning(ref SystemState state)
    {

    }

    public void OnDestroy(ref SystemState state)
    {

    }

    public void Move(ref SystemState state)
    {
        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;

        if (PlayerInfo.VisibleStats.x <= 0)
        {
            return;
        }

        ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;
        ref InputData InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;

        float3 NewPos = PlayerTransform.Position;

        if (InputInfo.Movement.x >= PlayerInfo.MovementThreshold)
        {
            NewPos.x += 1;
        }
        else if (InputInfo.Movement.x <= -PlayerInfo.MovementThreshold)
        {
            NewPos.x -= 1;
        }

        if (InputInfo.Movement.y >= PlayerInfo.MovementThreshold)
        {
            NewPos.z += 1;
        }
        else if (InputInfo.Movement.y <= -PlayerInfo.MovementThreshold)
        {
            NewPos.z -= 1;
        }

        //Debug.Log(NewPos);

        if (math.all(NewPos == PlayerTransform.Position))
        {
            return;
        }

        ref ChunkMaster MapInfo = ref SystemAPI.GetSingletonRW<ChunkMaster>().ValueRW;

        if (!MapInfo.IsSafe((int3)NewPos, SystemAPI.GetSingleton<PlayerData>().MaxDanger, ref state))
        {
            return;
        }

        PlayerTransform.Position = NewPos;

        var NewCamPos = NewPos;
        NewCamPos.y = 5;

        //ref CameraData Cam = ref SystemAPI.GetComponentLookup<CameraData>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;
        ref CameraData Cam = ref SystemAPI.GetSingletonRW<CameraData>().ValueRW;
        Cam.Pos = (int3)NewCamPos;

        MapInfo.ChunksToGenerate.Add(MapInfo.GetChunkNum(NewPos));
        //Debug.Log(GetChunkNum(NewPos, MapInfo.ChunkSize));

        SystemAPI.GetSingletonRW<PlayerData>().ValueRW.DebugChunkColour = CalculateBiomeColour(NewPos, ref MapInfo);

        PlayerInfo.VisibleStats.y -= 1;
        DevourBlocks((int3)NewPos, ref MapInfo, ref PlayerInfo, ref state);

        if (PlayerInfo.VisibleStats.y < 0)
        {
            PlayerInfo.VisibleStats.x -= 1;
        }

        PlayerInfo.MaxDanger = (int)(PlayerInfo.VisibleStats.w * 100);

        Cam.Zoom = math.clamp(PlayerInfo.HiddenStats.x, PlayerInfo.MinCameraZoom, PlayerInfo.MaxCameraZoom);

        if (PlayerInfo.VisibleStats.x <= 0)
        {
            ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
            UIInfo.UIState = UIStatus.Dead;
            UIInfo.Cost = 1;
        }
    }

    //[BurstCompile]
    //public static int2 GetChunkNum(float3 Pos, float ChunkSize)
    //{
    //    //return new int2(Mathf.FloorToInt(Pos.x / ChunkSize), Mathf.FloorToInt(Pos.z / ChunkSize));
    //    return new int2((int)math.floor(Pos.x / ChunkSize), (int)math.floor(Pos.z / ChunkSize));
    //}

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

    //[BurstCompile]
    //public bool IsSafe(int3 Pos, int MaxDangerLevel, ref ChunkMaster MapInfo)
    //{
    //    if(!MapInfo.Chunks.TryGetValue(MapInfo.GetChunkNum(Pos), out Entity ChunkEntity))
    //    {
    //        Debug.Log("chunk isnt real?");
    //        return true;
    //    }

    //    if (ChunkEntity == Entity.Null)
    //    {
    //        Debug.Log("Couldn't get chunk entity, assuming safe!");
    //        return true;
    //    }

    //    DynamicBuffer<EntityHerd> StuffInChunk = SystemAPI.GetBuffer<EntityHerd>(ChunkEntity);

    //    for (int i = 0; i < StuffInChunk.Length; i++)
    //    {
    //        if (StuffInChunk[i].Danger > MaxDangerLevel && math.all(Pos.xz == (int2)SystemAPI.GetComponent<LocalTransform>(StuffInChunk[i].Block).Position.xz))
    //        {
    //            return false;
    //        }
    //    }

    //    return true;
    //}

    [BurstCompile]
    public void DevourBlocks(int3 Pos, ref ChunkMaster MapInfo, ref PlayerData Stats, ref SystemState state)
    {
        if (!MapInfo.Chunks.TryGetValue(MapInfo.GetChunkNum(Pos), out Entity ChunkEntity))
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
                BlockBehaviourData Block = SystemAPI.GetComponent<BlockBehaviourData>(EntityToRemove);

                Stats.VisibleStats += Block.VisibleStats;
                Stats.HiddenStats += Block.HiddenStats;

                state.EntityManager.DestroyEntity(EntityToRemove);
                StuffInChunk = SystemAPI.GetBuffer<EntityHerd>(ChunkEntity);
                StuffInChunk.RemoveAt(i);
                StuffInChunk = SystemAPI.GetBuffer<EntityHerd>(ChunkEntity);
            }
        }
    }
}
