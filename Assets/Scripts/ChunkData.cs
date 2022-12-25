using Unity.Entities;
using Unity.Collections;

public struct ChunkData : IComponentData
{
    //public NativeArray<Entity> StuffInChunk;
    //public NativeArray<int> SafetyOfStuffInChunk;
    public NativeArray<Something> StuffInChunk;
}

public struct Something
{
    public Entity Thing;
    public int Danger;
}
