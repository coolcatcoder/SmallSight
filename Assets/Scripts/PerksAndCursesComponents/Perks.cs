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
                    Description = authoring.MultiUsePerks[i].Description,
                    Cost = authoring.MultiUsePerks[i].Cost,
                    AmountOwned = 0,
                    VarToChange = authoring.MultiUsePerks[i].VarToChange,
                    AmountToChange = authoring.MultiUsePerks[i].AmountToChange,
                    SkillToSet = authoring.MultiUsePerks[i].SkillToSet
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
    public FixedString128Bytes Description;
    public int Cost;
    public int AmountOwned;
    public Change VarToChange;
    public float AmountToChange;
    public Skills SkillToSet;
}

[Serializable]
public struct Perk
{
    public string Description;
    public int Cost;
    public Change VarToChange;
    public float AmountToChange;
    public Skills SkillToSet;
}