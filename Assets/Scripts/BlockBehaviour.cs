using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class BlockBehaviour : MonoBehaviour
{
    public float4 VisibleStats;
    public float4 HiddenStats;

    public ConsumeBehaviour ConsumedBehaviour;
}

public class BlockBehaviourBaker : Baker<BlockBehaviour>
{
    public override void Bake(BlockBehaviour authoring)
    {
        AddComponent<BlockBehaviourData>(new BlockBehaviourData
        {
            VisibleStats = authoring.VisibleStats,
            HiddenStats = authoring.HiddenStats,
            ConsumedBehaviour = authoring.ConsumedBehaviour
        });
    }
}

public enum ConsumeBehaviour
{
    AddStats,
    Unused
}

public struct BlockBehaviourData : IComponentData
{
    public float4 VisibleStats;
    public float4 HiddenStats;

    public ConsumeBehaviour ConsumedBehaviour;
}