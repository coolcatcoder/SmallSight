using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class SkillToCrossBehaviour : MonoBehaviour
{
    public Skills Skill;
}

public class SkillToCrossBehaviourBaker : Baker<SkillToCrossBehaviour>
{
    public override void Bake(SkillToCrossBehaviour authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new SkillToCrossBehaviourData
        {
            Skill = authoring.Skill
        });
    }
}

public struct SkillToCrossBehaviourData : IComponentData
{
    public Skills Skill;
}