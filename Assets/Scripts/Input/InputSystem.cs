using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class InputSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        EntityManager.AddComponent<InputData>(EntityManager.CreateEntity());
        UnityEngine.Object.FindObjectOfType<PlayerInput>().actionEvents[0].AddListener(ThrowInputs);
    }

    protected override void OnUpdate()
    {
        ref var InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
        
        if (InputInfo.Held)
        {
            InputInfo.TimeHeldFor += SystemAPI.Time.DeltaTime;
        }
    }

    public void ThrowInputs(InputAction.CallbackContext context)
    {
        ref var InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;

        InputInfo.Held = !context.canceled;
        InputInfo.Pressed = !context.canceled;
        InputInfo.TimeHeldFor = 0;
        InputInfo.Movement = context.ReadValue<Vector2>();
    }
}

public struct InputData : IComponentData
{
    public bool Pressed;
    public bool Held;
    public float TimeHeldFor;
    public float2 Movement;
}
