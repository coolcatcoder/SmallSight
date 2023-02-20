using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class DestroyOnRestart : MonoBehaviour
{
}

public class DestroyOnRestartBaker : Baker<DestroyOnRestart>
{
    public override void Bake(DestroyOnRestart authoring)
    {
        AddComponent<DestroyOnRestartData>();
    }
}

public struct DestroyOnRestartData : IComponentData
{
}