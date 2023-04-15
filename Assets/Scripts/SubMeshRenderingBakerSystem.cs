using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial struct SubMeshRenderingBakerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MapData>();
        state.RequireForUpdate<SubMeshPrefabData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
        Entity SubMeshPrefabEntity = SystemAPI.GetSingletonEntity<SubMeshPrefabData>();

        if (MapInfo.EnableSubMeshRendering)
        {
            MapInfo.EnableSubMeshRendering = false;

            for (int i = 0; i < MapInfo.TilemapSize.x * MapInfo.TilemapSize.y; i++)
            {
                Entity SMEntity = state.EntityManager.Instantiate(SubMeshPrefabEntity);

                state.EntityManager.RemoveComponent<SubMeshPrefabData>(SMEntity);
                state.EntityManager.AddComponent<SubMeshPrefabFilterData>(SMEntity);

                SystemAPI.GetComponentRW<MaterialMeshInfo>(SMEntity, false).ValueRW.Submesh = (sbyte)i;
            }
        }

        if (MapInfo.DisableSubMeshRendering)
        {
            MapInfo.DisableSubMeshRendering = false;

            state.EntityManager.DestroyEntity(new EntityQueryBuilder()
                .WithAll<SubMeshPrefabFilterData>()
                .Build(ref state));
        }
    }
}
