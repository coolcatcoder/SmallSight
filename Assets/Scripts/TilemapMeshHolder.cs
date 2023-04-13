using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class TilemapMeshHolder : MonoBehaviour
{
}

public class TilemapMeshHolderBaker : Baker<TilemapMeshHolder>
{
    public override void Bake(TilemapMeshHolder authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new TilemapMeshHolderData());
    }
}

public struct TilemapMeshHolderData : IComponentData
{
}