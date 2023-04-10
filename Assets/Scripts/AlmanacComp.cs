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
        [TextArea]
        public string Paragraph;

        public Texture2D PageIcon;
    }
}

public class AlmanacBaker : Baker<AlmanacComp>
{
    public override void Bake(AlmanacComp authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new AlmanacData
        {
            BelongsTo = authoring.BelongsTo
        });

        if (authoring.Pages != null)
        {
            Texture2D[] Icons = new Texture2D[authoring.Pages.Length];

            var PageBuffer = AddBuffer<PageElement>(entity);

            for (int i = 0; i < authoring.Pages.Length; i++)
            {
                PageBuffer.Add(new PageElement
                {
                    Title = authoring.Pages[i].Title,
                    Paragraph = authoring.Pages[i].Paragraph
                });

                if (authoring.Pages[i].PageIcon != null)
                {
                    Icons[i] = authoring.Pages[i].PageIcon;
                }
            }

            AddComponentObject(entity, new ManagedIconData
            {
                Icons = Icons
            });
        }
    }
}

public struct AlmanacData : IComponentData
{
    public AlmanacWorld BelongsTo;
}

[InternalBufferCapacity(0)]
public struct PageElement : IBufferElementData
{
    public FixedString32Bytes Title;
    public FixedString512Bytes Paragraph; //might be too much memory
}

public class ManagedIconData : IComponentData
{
    public Texture2D[] Icons;
}