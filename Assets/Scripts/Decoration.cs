using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class Decoration : MonoBehaviour // add a max hue and a min hue
{
    public float2 MinPos;
    public float2 MaxPos;
    public float YLevel = -1;
}

public class DecorationBaker : Baker<Decoration>
{
    public override void Bake(Decoration authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new DecorationData
        {
            MinPos = authoring.MinPos,
            MaxPos = authoring.MaxPos,
            YLevel = authoring.YLevel
        });
    }
}

public struct DecorationData : IComponentData
{
    public float2 MinPos;
    public float2 MaxPos;
    public float YLevel;
}