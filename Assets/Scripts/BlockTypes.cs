using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;

public class BlockTypes : MonoBehaviour
{
    public BlockTypeMonoElement[] BlockTypesArray;
}

[System.Serializable]
public struct BlockTypeMonoElement
{
    public int StrengthToWalkOn;
    public bool ConsumeOnCollision;
    public bool TeleportSafe;
    public int YLevel;
    public int SubstrateYLevel;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;

    public SpecialBehaviour Behaviour;

    public AlmanacWorld SectionIn;
    public int PageOn;

    //public GameObject[] Decorations;

    public float2 UV;
    public float2 SubstrateUV;
}

public class BlockTypesBaker : Baker<BlockTypes>
{
    public override void Bake(BlockTypes authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        var BlockTypesBuffer = AddBuffer<BlockTypeElement>(entity);

        if (authoring.BlockTypesArray != null)
        {
            for (int i = 0; i < authoring.BlockTypesArray.Length; i++)
            {
                var BlockTypeInfo = authoring.BlockTypesArray[i];

                BlockTypesBuffer.Add(new BlockTypeElement
                {
                    StrengthToWalkOn = BlockTypeInfo.StrengthToWalkOn,
                    ConsumeOnCollision = BlockTypeInfo.ConsumeOnCollision,
                    TeleportSafe = BlockTypeInfo.TeleportSafe,
                    YLevel = BlockTypeInfo.YLevel,
                    SubstrateYLevel = BlockTypeInfo.SubstrateYLevel,

                    VisibleStatsChange = BlockTypeInfo.VisibleStatsChange,
                    HiddenStatsChange = BlockTypeInfo.HiddenStatsChange,

                    Behaviour = BlockTypeInfo.Behaviour,

                    SectionIn = BlockTypeInfo.SectionIn,
                    PageOn = BlockTypeInfo.PageOn,

                    UV = BlockTypeInfo.UV,
                    SubstrateUV = BlockTypeInfo.SubstrateUV
                });
                
            }
        }
    }
}

[InternalBufferCapacity(0)]
public struct BlockTypeElement : IBufferElementData
{
    public int StrengthToWalkOn;
    public bool ConsumeOnCollision;
    public bool TeleportSafe;
    public int YLevel;
    public int SubstrateYLevel;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;

    public SpecialBehaviour Behaviour;

    public AlmanacWorld SectionIn;
    public int PageOn;

    public float2 UV;
    public float2 SubstrateUV;
}