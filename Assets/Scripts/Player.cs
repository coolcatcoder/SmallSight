using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class Player : MonoBehaviour
{
    public float4 DefaultVisibleStats;
    public float4 DefaultHiddenStats;

    public float SecondsUntilHoldMovement = 1f;
    public float HeldMovementDelay = 0.2f;

    public int GenerationThickness = 1;

    public float ChanceOfDangerousWarp = 50;
}

public class PlayerBaker : Baker<Player>
{
    public override void Bake(Player authoring)
    {
        AddComponent(new PlayerData
        {
            SecondsUntilHoldMovement = authoring.SecondsUntilHoldMovement,
            HeldMovementDelay = authoring.HeldMovementDelay,
            GenerationThickness = authoring.GenerationThickness,
            DefaultVisibleStats = authoring.DefaultVisibleStats,
            DefaultHiddenStats = authoring.DefaultHiddenStats,
            ChanceOfDangerousWarp = authoring.ChanceOfDangerousWarp
        });
    }
}

public struct PlayerData : IComponentData
{
    public float4 DefaultVisibleStats;
    public float4 DefaultHiddenStats;
    public float4 VisibleStats;
    public float4 HiddenStats;

    public float SecondsUntilHoldMovement;
    public float HeldMovementDelay;

    public int GenerationThickness;

    public float ChanceOfDangerousWarp;
}