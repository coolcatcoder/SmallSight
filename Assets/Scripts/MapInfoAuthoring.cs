using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class MapInfoAuthoring : MonoBehaviour
{
    public int MaxChunks = 100000;
}

class MapInfoBaker : Baker<MapInfoAuthoring>
{
    public override void Bake(MapInfoAuthoring authoring)
    {
        AddComponent(new ChunkMaster
        {
            Chunks = new NativeHashMap<Unity.Mathematics.int2, Entity>(authoring.MaxChunks, Allocator.Persistent)
        });
    }
}
