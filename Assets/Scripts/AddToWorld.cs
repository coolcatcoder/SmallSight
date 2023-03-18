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
        AddComponent(new AddToWorldData
        {
            WorldIndex = authoring.WorldIndex
        });
    }
}

public struct AddToWorldData : IComponentData
{
    public int WorldIndex;
}