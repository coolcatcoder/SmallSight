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
            Behaviour = authoring.Behaviour
        });
    }
}

public struct BlockData : IComponentData
{
    public int StrengthToWalkOn;
    public bool ConsumeOnCollision;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;

    public SpecialBehaviour Behaviour;
}

public enum SpecialBehaviour
{
    None,
    Warp
}