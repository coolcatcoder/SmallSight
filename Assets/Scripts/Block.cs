using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class Block : MonoBehaviour
{
    public int StrengthToWalkOn;
    public bool ConsumeOnCollision = true;
    public bool TeleportSafe = false;
    public float YLevel = -2;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;

    public SpecialBehaviour Behaviour = SpecialBehaviour.None;
}

public class BlockBaker : Baker<Block>
{
    public override void Bake(Block authoring)
    {
        AddComponent(new BlockData
        {
            StrengthToWalkOn = authoring.StrengthToWalkOn,
            VisibleStatsChange = authoring.VisibleStatsChange,
            HiddenStatsChange = authoring.HiddenStatsChange,
            ConsumeOnCollision = authoring.ConsumeOnCollision,
            Behaviour = authoring.Behaviour,
            TeleportSafe = authoring.TeleportSafe,
            YLevel = authoring.YLevel
        });
    }
}

public struct BlockData : IComponentData
{
    public int StrengthToWalkOn;
    public bool ConsumeOnCollision;
    public bool TeleportSafe;
    public float YLevel;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;

    public SpecialBehaviour Behaviour;
}

[System.Flags]
public enum SpecialBehaviour
{
    None = 0,
    Warp = 1,
    Replace = 2,
    SkillStats = 4,
    SkillToCross = 8
}