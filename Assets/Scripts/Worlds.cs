using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class Worlds : MonoBehaviour
{
    public WorldElement[] WorldElements;
}

public class WorldsBaker : Baker<Worlds>
{
    public override void Bake(Worlds authoring)
    {
        if (authoring.WorldElements != null)
        {
            var WorldBuffer = AddBuffer<WorldData>();
            for (int i = 0; i < authoring.WorldElements.Length; i++)
            {
                WorldBuffer.Add(new WorldData
                {
                    WorldIndex = authoring.WorldElements[i].WorldIndex,
                    Dangerous = authoring.WorldElements[i].Dangerous,
                    BackGround = authoring.WorldElements[i].BackGround
                });
            }
        }
    }
}

[InternalBufferCapacity(0)]
public struct WorldData : IBufferElementData
{
    public int WorldIndex;
    public bool Dangerous;
    public Color BackGround;
}

[System.Serializable]
public struct WorldElement
{
    public int WorldIndex;
    public bool Dangerous;
    public Color BackGround;
}