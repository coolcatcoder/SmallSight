using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

public class ChunkUnloadingQueueAuthoring : MonoBehaviour
{
}

class ChunkUnloadingQueueBaker : Baker<ChunkUnloadingQueueAuthoring>
{
    public override void Bake(ChunkUnloadingQueueAuthoring authoring)
    {
        AddBuffer<ChunkUnloadingQueueData>();
    }
}
