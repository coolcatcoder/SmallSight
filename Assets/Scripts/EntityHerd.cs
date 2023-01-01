using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[InternalBufferCapacity(0)]
public struct EntityHerd : IBufferElementData
{
    public Entity Block;
    public int Danger;
}
