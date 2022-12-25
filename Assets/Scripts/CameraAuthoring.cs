using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CameraAuthoring : MonoBehaviour
{
    public float Zoom;
    public int3 Pos;
}

public class CameraBaker : Baker<CameraAuthoring>
{
    public override void Bake(CameraAuthoring authoring)
    {
        AddComponent(new CameraData
        {
            Zoom = authoring.Zoom,
            Pos = authoring.Pos
        });
    }
}
