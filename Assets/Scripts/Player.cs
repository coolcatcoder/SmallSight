using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using System;

public partial class Player : SystemBase
{
    int3 CurrentPos;
    //MapMaker MapSystem;

    protected override void OnStartRunning()
    {
        //MapSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MapMaker>();
    }

    protected override void OnUpdate()
    {
        //throw new System.NotImplementedException();
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

        Debug.Log(NewPos);

        if (math.all(NewPos == PTransform.Position))
        {
            return;
        }

        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(PlayerEntity, false).ValueRW.Position = NewPos;

        var NewCamPos = NewPos;
        NewCamPos.y = 5;

        SystemAPI.GetComponentLookup<CameraData>().GetRefRW(PlayerEntity, false).ValueRW.Pos = (int3)NewCamPos;

        CurrentPos = (int3)NewPos;
    }
}
