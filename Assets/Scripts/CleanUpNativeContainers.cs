using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public partial struct CleanUpNativeContainers : ISystem, ISystemStartStop
{
    NativeHashMap<int2, Entity> TilemapManager;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TilemapData>();
    }

    public void OnDestroy(ref SystemState state)
    {
        
    }

    public void OnStartRunning(ref SystemState state)
    {
        ref var TilemapInfo = ref SystemAPI.GetSingletonRW<TilemapData>().ValueRW;
        TilemapManager = TilemapInfo.TilemapManager;
    }

    public void OnStopRunning(ref SystemState state)
    {
        TilemapManager.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        
    }
}
