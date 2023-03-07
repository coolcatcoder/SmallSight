using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public partial struct CleanUpNativeContainers : ISystem, ISystemStartStop
{
    NativeHashMap<int2, Entity> GB2D;
    NativeHashMap<int3, Entity> GB3D;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MapData>();
    }

    public void OnDestroy(ref SystemState state)
    {
        
    }

    public void OnStartRunning(ref SystemState state)
    {
        GB2D = SystemAPI.GetSingletonRW<MapData>().ValueRW.GeneratedBlocks2D;
        GB3D = SystemAPI.GetSingletonRW<MapData>().ValueRW.GeneratedBlocks3D;
    }

    public void OnStopRunning(ref SystemState state)
    {
        GB2D.Dispose();
        GB3D.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        
    }
}
