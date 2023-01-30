using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class DefaultBiome : MonoBehaviour
{
    public int WorldIndex;
}

public class DefaultBiomeBaker : Baker<DefaultBiome>
{
    public override void Bake(DefaultBiome authoring)
    {
        AddComponent(new DefaultBiomeData
        {
            WorldIndex = authoring.WorldIndex
        });
    }
}

public struct DefaultBiomeData : IComponentData
{
    public int WorldIndex;
}