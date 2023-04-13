using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public partial struct CleanUpNativeContainers : ISystem, ISystemStartStop
{
    NativeList<int3> BlocksInMesh;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TilemapMeshData>();
    }

    public void OnDestroy(ref SystemState state)
    {
        
    }

    public void OnStartRunning(ref SystemState state)
    {
        BlocksInMesh = SystemAPI.GetSingletonRW<TilemapMeshData>().ValueRW.BlocksInMesh;
    }

    public void OnStopRunning(ref SystemState state)
    {
        BlocksInMesh.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        
    }
}
