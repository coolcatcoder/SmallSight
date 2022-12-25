using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;

public partial class MapMaker : SystemBase
{
    Entity ChunkHolder;
    public int ChunkSize = 10;

    protected override void OnCreate()
    {
        RequireForUpdate<ChunkMaster>();
    }

    protected override void OnStartRunning()
    {
        EntityQuery ChunkMasterQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ChunkMaster>()
            .Build(this);

        ChunkHolder = ChunkMasterQuery.ToEntityArray(Allocator.Temp)[0];
    }

    protected override void OnUpdate()
    {
        
    }

    public bool IsSafe(int3 Pos, int MaxDangerLevel)
    {
        Entity Chunk = GetChunkEntity(GetChunkNum(Pos));

        if (Chunk==null)
        {
            Debug.Log("Couldn't get chunk entity, assuming safe!");
            return true;
        }

        ChunkData CD = SystemAPI.GetComponent<ChunkData>(Chunk);

        for (int i = 0; i < CD.StuffInChunk.Length; i++)
        {
            if (CD.StuffInChunk[i].Danger > MaxDangerLevel && math.all(Pos == (int3)SystemAPI.GetComponent<LocalTransform>(CD.StuffInChunk[i].Thing).Position))
            {
                return false;
            }
        }

        return true;
    }

    [BurstCompile]
    public int2 GetChunkNum(int3 Pos)
    {
        return new int2((int)math.floor(Pos.x / ChunkSize), (int)math.floor(Pos.z / ChunkSize));
    }

    public Entity GetChunkEntity(int2 ChunkNum)
    {
        ChunkMaster CurrentChunkMaster = SystemAPI.GetComponent<ChunkMaster>(ChunkHolder);

        CurrentChunkMaster.Chunks.TryGetValue(ChunkNum, out Entity CurrentChunk);

        return CurrentChunk;
    }
}
