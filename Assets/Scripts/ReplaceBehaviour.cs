using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class ReplaceBehaviour : MonoBehaviour
{
    public GameObject ReplacementBlock;
}

public class ReplaceBehaviourBaker : Baker<ReplaceBehaviour>
{
    public override void Bake(ReplaceBehaviour authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ReplaceBehaviourData
        {
            ReplacementBlock = GetEntity(authoring.ReplacementBlock, TransformUsageFlags.Dynamic)
        });
    }
}

public struct ReplaceBehaviourData : IComponentData
{
    public Entity ReplacementBlock;
}