using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class SkillStatsBehaviour : MonoBehaviour
{
    public Skills Skill; // at some point switch this over to an array of a struct containing this, then bake it as a DynamicBuffer
    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;
}

public class SkillStatsBehaviourBaker : Baker<SkillStatsBehaviour>
{
    public override void Bake(SkillStatsBehaviour authoring)
    {
        AddComponent(new SkillStatsBehaviourData
        {
            Skill = authoring.Skill,
            VisibleStatsChange = authoring.VisibleStatsChange,
            HiddenStatsChange = authoring.HiddenStatsChange
        });
    }
}

public struct SkillStatsBehaviourData : IComponentData
{
    public Skills Skill;
    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;
}