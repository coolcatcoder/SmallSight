using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

public class DefaultBiome : MonoBehaviour
{
}

public class DefaultBiomeBaker : Baker<DefaultBiome>
{
    public override void Bake(DefaultBiome authoring)
    {
        AddComponent<DefaultBiomeData>();
    }
}

public struct DefaultBiomeData : IComponentData
{
}