using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

public class CameraSync : MonoBehaviour
{
    private Entity ECam;
    private Entity EMap;
    private Entity EInput;
    public GameObject GCam2D;
    public GameObject GCamParent3D;
    public GameObject GCam3D;

    public float SmoothTime = 0.3f;
    private Vector3 Velocity = Vector3.zero;
    private float2 WeirdCamMov;

    // Start is called before the first frame update
    void Start()
    {
        var Wow = new DepthSorted_Tag();
        Debug.Log(Wow);
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
        else if (EMap == Entity.Null)
        {
            EntityQuery MapQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<MapData>()
            .Build(World.DefaultGameObjectInjectionWorld.EntityManager);

            if (MapQuery.CalculateEntityCount() == 1)
            {
                EMap = MapQuery.ToEntityArray(Allocator.Temp)[0];
            }
        }
        else if (EInput == Entity.Null)
        {
            EntityQuery InputQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<InputData>()
            .Build(World.DefaultGameObjectInjectionWorld.EntityManager);

            if (InputQuery.CalculateEntityCount() == 1)
            {
                EInput = InputQuery.ToEntityArray(Allocator.Temp)[0];
            }
        }
        else
        {
            MapData MapInfo = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<MapData>(EMap);
            if (MapInfo.Is3D)
            {
                Camera3DUpdate();
            }
            else
            {
                Camera2DUpdate();
            }
        }
    }

    public void Camera2DUpdate()
    {
        var PlayerInfo = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PlayerData>(ECam);
        var PlayerTransform = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<LocalTransform>(ECam);

        GCam2D.SetActive(true);
        GCam3D.SetActive(false);
        GCam2D.GetComponent<Camera>().orthographicSize = PlayerInfo.HiddenStats.x;

        float3 CamPos = new float3(PlayerTransform.Position.x, 5, PlayerTransform.Position.z);

        if (PlayerInfo.JustTeleported)
        {
            PlayerInfo.JustTeleported = false;
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData<PlayerData>(ECam, PlayerInfo);
            GCam2D.transform.position = CamPos;
        }
        else
        {
            GCam2D.transform.position = Vector3.SmoothDamp(GCam2D.transform.position, CamPos, ref Velocity, SmoothTime);
        }
    }

    public void Camera3DUpdate()
    {
        var PlayerInfo = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PlayerData>(ECam);
        var PlayerTransform = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<LocalTransform>(ECam);
        var InputInfo = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<InputData>(EInput);

        //GCam.GetComponent<Camera>().orthographicSize = PlayerInfo.HiddenStats.x;
        GCam2D.SetActive(false);
        GCam3D.SetActive(true);

        //float3 CamPos = new float3(PlayerTransform.Position.x-5, PlayerTransform.Position.y+5, PlayerTransform.Position.z);

        if (PlayerInfo.JustTeleported)
        {
            PlayerInfo.JustTeleported = false;
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData<PlayerData>(ECam, PlayerInfo);
            GCamParent3D.transform.position = PlayerTransform.Position;
        }
        else
        {
            GCamParent3D.transform.position = Vector3.SmoothDamp(GCamParent3D.transform.position, PlayerTransform.Position, ref Velocity, SmoothTime);
        }

        //Vector3 CamRotationX = new Vector3(InputInfo.CameraMovement.y, 0, 0);
        //Vector3 CamRotationY = new Vector3(0, InputInfo.CameraMovement.x, 0);
        //GCamParent3D.transform.eulerAngles += CamRotation * PlayerInfo.CameraSensitivity;
        //GCamParent3D.transform.Rotate(CamRotationX * PlayerInfo.CameraSensitivity, Space.World);
        //GCamParent3D.transform.Rotate(CamRotationY * PlayerInfo.CameraSensitivity, Space.World);

        //GCamParent3D.transform.RotateAround(new float3(1, 0, 0), InputInfo.CameraMovement.x);
        //GCamParent3D.transform.RotateAround(new float3(0, 1, 0), InputInfo.CameraMovement.y);

        //Quaternion CamRot = new();
        //CamRot.eulerAngles = new float3(InputInfo.CameraMovement.y, InputInfo.CameraMovement.x, 0)*PlayerInfo.CameraSensitivity;

        //GCamParent3D.transform.rotation *= Quaternion.Euler(new float3(InputInfo.CameraMovement.y, InputInfo.CameraMovement.x, 0) * PlayerInfo.CameraSensitivity);

        WeirdCamMov += InputInfo.CameraMovement * PlayerInfo.CameraSensitivity;

        GCamParent3D.transform.rotation =
  Quaternion.Euler(0, -WeirdCamMov.x, 0) // A rotation around world up
  * Quaternion.AngleAxis(WeirdCamMov.y, Vector3.right); // A rotation around the 'right' axis
    }
}