using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class Player : MonoBehaviour
{
    public float4 DefaultVisibleStats; // health,stamina,teleports,strength
    public float4 DefaultHiddenStats; // vision,karma,????,????

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

    public float MinInputDetected;

    public Skills PlayerSkills;
}

[System.Flags]
public enum Skills // info about bitwise and enums: https://www.alanzucconi.com/2015/07/26/enum-flags-and-bitwise-operators/
{
    None = 0,
    Botanist = 1,
    ThornWading = 2,
    Exhausted = 4,
    Swimmer = 8,
    Hunter = 16,
    RockEater = 32
}