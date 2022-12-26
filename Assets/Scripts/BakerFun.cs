using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace BakerFun
{
    public class BakerFun
    {
        public NativeArray<Entity> GetEntities(GameObject[] authoring, Allocator ArrayAllocation)
        {
            NativeArray<Entity> EntityArray = new NativeArray<Entity>(authoring.Length, ArrayAllocation);

            Debug.Log("not done yet");

            return EntityArray;
        }
    }
}