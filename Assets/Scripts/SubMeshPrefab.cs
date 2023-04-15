using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class SubMeshPrefab : MonoBehaviour
{
}

public class SubmeshPrefabBaker : Baker<SubMeshPrefab>
{
    public override void Bake(SubMeshPrefab authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new SubMeshPrefabData());
    }
}

public struct SubMeshPrefabData : IComponentData
{
}