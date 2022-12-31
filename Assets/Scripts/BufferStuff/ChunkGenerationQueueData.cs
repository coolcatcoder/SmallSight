using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

[InternalBufferCapacity(1000)] //idk what units this is using, going to assume mib, and i hope like anything this is enough!
public struct ChunkGenerationQueueData : IBufferElementData
{
    public int2 ChunkToGenerate;
}
