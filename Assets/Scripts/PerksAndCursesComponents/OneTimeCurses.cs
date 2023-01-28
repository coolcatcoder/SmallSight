using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class OneTimeCurses : MonoBehaviour
{
    public OneTimeCurse[] Curses;
}

public class OneTimeCursesBaker : Baker<OneTimeCurses>
{
    public override void Bake(OneTimeCurses authoring)
    {
        if (authoring.Curses != null)
        {
            var FeatureBuffer = AddBuffer<OneTimeCurseElement>();
            for (int i = 0; i < authoring.Curses.Length; i++)
            {
                FeatureBuffer.Add(new OneTimeCurseElement
                {
                    Description = authoring.Curses[i].Description,
                    Cost = authoring.Curses[i].Cost,
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
public struct OneTimeCurseElement : IBufferElementData
{
    public FixedString128Bytes Description;
    public int Cost;
    public bool Used;
}

[Serializable]
public struct OneTimeCurse
{
    public string Description;
    public int Cost;
}