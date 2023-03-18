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
        AddComponent(new MouseBlockMarkerData
        {
        });
    }
}

public struct MouseBlockMarkerData : IComponentData
{
}