using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class AlmanacComp : MonoBehaviour
{
    public AlmanacWorld BelongsTo;
    public AlmanacPage[] Pages;

    [System.Serializable]
    public struct AlmanacPage
    {
        public string Title;
        public string Paragraph;
    }
}

public class AlmanacBaker : Baker<AlmanacComp>
{
    public override void Bake(AlmanacComp authoring)
    {
        AddComponent(new AlmanacData
        {
        });
    }
}

public struct AlmanacData : IComponentData
{
}