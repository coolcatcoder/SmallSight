using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

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
            .WithAll<CameraData>()
            .Build(World.DefaultGameObjectInjectionWorld.EntityManager);

            if (CamQuery.CalculateEntityCount() == 1)
            {
                ECam = CamQuery.ToEntityArray(Allocator.Temp)[0];
            }
        }
        else
        {
            //var CamComp = SystemAPI.GetComponent<CameraData>(ECam);
            var CamComp = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<CameraData>(ECam);

            GCam.GetComponent<Camera>().orthographicSize = CamComp.Zoom;
            GCam.transform.position = (float3)CamComp.Pos;
        }
    }
}
