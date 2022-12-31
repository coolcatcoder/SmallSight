using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

public class ChunkLoadingQueueAuthoring : MonoBehaviour
{
}

class ChunkLoadingQueueBaker : Baker<ChunkLoadingQueueAuthoring>
{
    public override void Bake(ChunkLoadingQueueAuthoring authoring)
    {
        AddBuffer<ChunkLoadingQueueData>();
    }
}
