using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class OneTimePerks : MonoBehaviour
{
    public OneTimePerk[] Perks;
}

public class OneTimePerksBaker : Baker<OneTimePerks>
{
    public override void Bake(OneTimePerks authoring)
    {
        if (authoring.Perks != null)
        {
            var FeatureBuffer = AddBuffer<OneTimePerkElement>();
            for (int i = 0; i < authoring.Perks.Length; i++)
            {
                FeatureBuffer.Add(new OneTimePerkElement
                {
                    Description = new NativeText(authoring.Perks[i].Description, Allocator.Persistent),
                    Cost = authoring.Perks[i].Cost,
                    Used = false
                });
            }

        }
        else
        {
            Debug.Log("Features is null?????");
        }
    }
}

[InternalBufferCapacity(0)]
public struct OneTimePerkElement : IBufferElementData
{
    public NativeText Description;
    public int Cost;
    public bool Used;
}

[Serializable]
public struct OneTimePerk
{
    public string Description;
    public int Cost;
}