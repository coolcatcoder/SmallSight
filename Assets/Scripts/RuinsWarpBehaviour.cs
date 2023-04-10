using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class RuinsWarpBehaviour : MonoBehaviour
{
    public int3 RuinPos; // we assume this pos to be safe, which it may not be if the player has gone and destroyed parts of the ruin
}

public class RuinsWarpBehaviourBaker : Baker<RuinsWarpBehaviour>
{
    public override void Bake(RuinsWarpBehaviour authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new RuinsWarpBehaviourData
        {
            RuinPos = authoring.RuinPos
        });
    }
}

public struct RuinsWarpBehaviourData : IComponentData
{
    public int3 RuinPos;
}