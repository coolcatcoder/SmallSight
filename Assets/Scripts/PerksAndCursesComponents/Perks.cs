using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class Perks : MonoBehaviour
{
    public Perk[] MultiUsePerks;
}

public class PerksBaker : Baker<Perks>
{
    public override void Bake(Perks authoring)
    {
        if (authoring.MultiUsePerks != null)
        {
            var FeatureBuffer = AddBuffer<PerkElement>();
            for (int i = 0; i < authoring.MultiUsePerks.Length; i++)
            {
                FeatureBuffer.Add(new PerkElement
                {
                    Description = new NativeText(authoring.MultiUsePerks[i].Description, Allocator.Persistent),
                    Cost = authoring.MultiUsePerks[i].Cost,
                    AmountOwned = false
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
public struct PerkElement : IBufferElementData
{
    public NativeText Description;
    public int Cost;
    public bool AmountOwned;
}

[Serializable]
public struct Perk
{
    public string Description;
    public int Cost;
}