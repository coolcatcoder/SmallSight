using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public class CameraSync : MonoBehaviour
{
    private Entity ECam;
    public GameObject GCam;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (ECam == Entity.Null)
        {
            EntityQuery CamQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<PlayerData>()
            .Build(World.DefaultGameObjectInjectionWorld.EntityManager);

            if (CamQuery.CalculateEntityCount() == 1)
            {
                ECam = CamQuery.ToEntityArray(Allocator.Temp)[0];
            }
        }
        else
        {
            //var CamComp = SystemAPI.GetComponent<CameraData>(ECam);
            var PlayerInfo = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PlayerData>(ECam);
            var PlayerTransform = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<LocalTransform>(ECam);

            GCam.GetComponent<Camera>().orthographicSize = PlayerInfo.HiddenStats.x;

            float3 CamPos = new float3(PlayerTransform.Position.x, 5, PlayerTransform.Position.z);
            GCam.transform.position = CamPos;
        }
    }
}