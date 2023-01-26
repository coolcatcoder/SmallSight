using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class Block : MonoBehaviour
{
    public int StrengthToWalkOn;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;
}

public class BlockBaker : Baker<Block>
{
    public override void Bake(Block authoring)
    {
        AddComponent(new BlockData
        {
            StrengthToWalkOn = authoring.StrengthToWalkOn,
            VisibleStatsChange = authoring.VisibleStatsChange,
            HiddenStatsChange = authoring.HiddenStatsChange
        });
    }
}

public struct BlockData : IComponentData
{
    public int StrengthToWalkOn;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;
}