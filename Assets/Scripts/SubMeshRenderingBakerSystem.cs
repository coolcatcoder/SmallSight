//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Rendering;
//using Unity.Collections;
//using Unity.Transforms;

//[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
//public partial struct SubMeshRenderingBakerSystem : ISystem
//{
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<MapData>();
//        state.RequireForUpdate<SubMeshPrefabData>();
//        state.RequireForUpdate<TilemapMeshHolderData>();
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        //Debug.Log("Something got baked?");

//        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
//        Entity SubMeshPrefabEntity = SystemAPI.GetSingletonEntity<SubMeshPrefabData>();

//        if (MapInfo.EnableSubMeshRendering)
//        {
//            MapInfo.EnableSubMeshRendering = false;

//            Entity MeshHolderEntity = SystemAPI.GetSingletonEntity<TilemapMeshHolderData>();
//            ref var TilemapMeshComp = ref SystemAPI.GetComponentRW<MaterialMeshInfo>(MeshHolderEntity, false).ValueRW;
//            var RenderMeshArrayInfo = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(MeshHolderEntity);

//            Mesh TilemapMesh = RenderMeshArrayInfo.GetMesh(TilemapMeshComp);

//            var RenderDesc = new RenderMeshDescription(shadowCastingMode: UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows: false);
//            var RenderMeshInfo = new RenderMeshArray();

//            for (int i = 0; i < MapInfo.TilemapSize.x * MapInfo.TilemapSize.y; i++)
//            {
//                Entity SMEntity = state.EntityManager.CreateEntity();

//                RenderMeshUtility.AddComponents(SMEntity, state.EntityManager, RenderDesc, RenderMeshInfo);

//                state.EntityManager.AddComponent<SubMeshPrefabFilterData>(SMEntity);
                
//                SystemAPI.GetComponentRW<MaterialMeshInfo>(SMEntity, false).ValueRW.Submesh = (sbyte)i;
//            }
//        }

//        if (MapInfo.DisableSubMeshRendering)
//        {
//            MapInfo.DisableSubMeshRendering = false;

//            state.EntityManager.DestroyEntity(new EntityQueryBuilder(Allocator.Temp)
//                .WithAll<SubMeshPrefabFilterData>()
//                .Build(ref state));
//        }
//    }
//}
