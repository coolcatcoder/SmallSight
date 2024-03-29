using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class Curses : MonoBehaviour
{
    public Curse[] MultiUseCurses;
}

public class CursesBaker : Baker<Curses>
{
    public override void Bake(Curses authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        if (authoring.MultiUseCurses != null)
        {
            var FeatureBuffer = AddBuffer<CurseElement>(entity);
            for (int i = 0; i < authoring.MultiUseCurses.Length; i++)
            {
                FeatureBuffer.Add(new CurseElement
                {
                    Description = authoring.MultiUseCurses[i].Description,
                    Cost = authoring.MultiUseCurses[i].Cost,
                    AmountOwned = 0,
                    VarToChange = authoring.MultiUseCurses[i].VarToChange,
                    AmountToChange = authoring.MultiUseCurses[i].AmountToChange,
                    SkillToSet = authoring.MultiUseCurses[i].SkillToSet
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
public struct CurseElement : IBufferElementData
{
    public FixedString128Bytes Description;
    public int Cost;
    public int AmountOwned;
    public Change VarToChange;
    public float AmountToChange;
    public Skills SkillToSet;
}

[Serializable]
public struct Curse
{
    public string Description;
    public int Cost;
    public Change VarToChange;
    public float AmountToChange;
    public Skills SkillToSet;
}