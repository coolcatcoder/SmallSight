using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
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