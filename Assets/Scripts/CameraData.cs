using Unity.Entities;
using Unity.Mathematics;

public struct CameraData : IComponentData
{
    public float Zoom;
    public int3 Pos;
}
