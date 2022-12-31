using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

public class ChunkGenerationQueueAuthoring : MonoBehaviour
{
}

class ChunkGenerationQueueBaker : Baker<ChunkGenerationQueueAuthoring>
{
    public override void Bake(ChunkGenerationQueueAuthoring authoring)
    {
        AddBuffer<ChunkGenerationQueueData>();
    }
}
