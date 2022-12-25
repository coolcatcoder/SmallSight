using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ChunkMaster : IComponentData
{
    public NativeHashMap<int2, Entity> Chunks;
    //public int ChunkSize; Scrapped for now!
}
