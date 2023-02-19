using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Block : MonoBehaviour
{
    public int StrengthToWalkOn;
    public bool ConsumeOnCollision = true;
    public bool TeleportSafe = false;
    public float YLevel = -2;
    public float DecorationChance = 100f;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;

    public SpecialBehaviour Behaviour = SpecialBehaviour.None;

    public GameObject[] Decorations;
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
            YLevel = authoring.YLevel,
            HasDecorations = authoring.Decorations != null,
            DecorationChance = authoring.DecorationChance
        });

        if (authoring.Decorations != null)
        {
            var DecorationBuffer = AddBuffer<DecorationElement>();

            for (int i = 0; i < authoring.Decorations.Length; i++)
            {
                DecorationBuffer.Add(new DecorationElement
                {
                    DecorationEntity = GetEntity(authoring.Decorations[i])
                });
            }
        }
    }
}

public struct BlockData : IComponentData
{
    public int StrengthToWalkOn;
    public bool ConsumeOnCollision;
    public bool TeleportSafe;
    public float YLevel;
    public float DecorationChance;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;

    public SpecialBehaviour Behaviour;

    public Entity DecorationEntity;
    public bool HasDecorations;
}

public struct DecorationElement : IBufferElementData
{
    public Entity DecorationEntity;
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