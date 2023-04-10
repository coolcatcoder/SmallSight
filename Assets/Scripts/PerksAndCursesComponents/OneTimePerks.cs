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
        var entity = GetEntity(TransformUsageFlags.None);
        if (authoring.Perks != null)
        {
            var FeatureBuffer = AddBuffer<OneTimePerkElement>(entity);
            for (int i = 0; i < authoring.Perks.Length; i++)
            {
                FeatureBuffer.Add(new OneTimePerkElement
                {
                    Description = authoring.Perks[i].Description,
                    Cost = authoring.Perks[i].Cost,
                    Used = false,
                    VarToChange = authoring.Perks[i].VarToChange,
                    AmountToChange = authoring.Perks[i].AmountToChange,
                    SkillToSet = authoring.Perks[i].SkillToSet
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
    public FixedString128Bytes Description;
    public int Cost;
    public bool Used;
    public Change VarToChange;
    public float AmountToChange;
    public Skills SkillToSet;
}

[Serializable]
public struct OneTimePerk
{
    public string Description;
    public int Cost;
    public Change VarToChange;
    public float AmountToChange;
    public Skills SkillToSet;
}