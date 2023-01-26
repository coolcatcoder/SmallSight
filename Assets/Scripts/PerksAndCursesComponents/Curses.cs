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
        if (authoring.MultiUseCurses != null)
        {
            var FeatureBuffer = AddBuffer<CurseElement>();
            for (int i = 0; i < authoring.MultiUseCurses.Length; i++)
            {
                FeatureBuffer.Add(new CurseElement
                {
                    Description = new NativeText(authoring.MultiUseCurses[i].Description, Allocator.Persistent),
                    Cost = authoring.MultiUseCurses[i].Cost,
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
public struct CurseElement : IBufferElementData
{
    public NativeText Description;
    public int Cost;
    public bool AmountOwned;
}

[Serializable]
public struct Curse
{
    public string Description;
    public int Cost;
}