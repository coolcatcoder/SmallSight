using UnityEngine;
using Unity.Entities;

public class AddToWorld : MonoBehaviour
{
    public int WorldIndex;
}

public class AddToWorldBaker : Baker<AddToWorld>
{
    public override void Bake(AddToWorld authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new AddToWorldData
        {
            WorldIndex = authoring.WorldIndex
        });
    }
}

public struct AddToWorldData : IComponentData
{
    public int WorldIndex;
}