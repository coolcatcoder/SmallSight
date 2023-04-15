using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class SubMeshPrefabFilter : MonoBehaviour
{
}

public class SubmeshPrefabFilterBaker : Baker<SubMeshPrefabFilter>
{
    public override void Bake(SubMeshPrefabFilter authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new SubMeshPrefabFilterData());
    }
}

public struct SubMeshPrefabFilterData : IComponentData
{
}