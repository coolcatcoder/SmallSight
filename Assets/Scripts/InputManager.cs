using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

public partial class InputManager : SystemBase
{
    protected override void OnStartRunning()
    {
        EntityManager.AddComponent<InputData>(EntityManager.CreateEntity());
        UnityEngine.Object.FindObjectOfType<PlayerInput>().actionEvents[0].AddListener(ThrowInputs);
        UnityEngine.Object.FindObjectOfType<PlayerInput>().actionEvents[11].AddListener(ThrowTeleport);
        UnityEngine.Object.FindObjectOfType<PlayerInput>().actionEvents[12].AddListener(ThrowCamera);
        UnityEngine.Object.FindObjectOfType<PlayerInput>().actionEvents[13].AddListener(ThrowYMove);

        var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(11380664438141642328);
        var type = TypeManager.GetType(typeIndex);
        Debug.Log(type);
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

    public void ThrowTeleport(InputAction.CallbackContext context)
    {
        ref var InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
        InputInfo.Teleport = !context.canceled;
        //Debug.Log("telleeeeport????");
    }

    public void ThrowCamera(InputAction.CallbackContext context)
    {
        ref var InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
        InputInfo.CameraMovement = context.ReadValue<Vector2>();
    }

    public void ThrowYMove(InputAction.CallbackContext context)
    {
        ref var InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;

        InputInfo.Held = !context.canceled;
        InputInfo.Pressed = !context.canceled;
        InputInfo.TimeHeldFor = 0;
        InputInfo.YMovement = context.ReadValue<Vector2>();
    }
}

public struct InputData : IComponentData
{
    public bool Pressed;
    public bool Held;
    public float TimeHeldFor;
    public float2 Movement;
    public float2 YMovement;
    public bool Teleport;
    public float2 CameraMovement;
}
