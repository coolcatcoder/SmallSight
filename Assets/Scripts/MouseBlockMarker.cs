using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class MouseBlockMarker : MonoBehaviour
{
}

public class MouseBlockMarkerBaker : Baker<MouseBlockMarker>
{
    public override void Bake(MouseBlockMarker authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new MouseBlockMarkerData
        {
        });
    }
}

public struct MouseBlockMarkerData : IComponentData
{
}