using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

public class PlayerDataAuthoring : MonoBehaviour
{
    public float4 VisibleStats;
    public float4 HiddenStats;

    //public TextMeshProUGUI StatsText;

    public float MovementThreshold;

    public float MinNoiseNotWalkable;

    //public Renderer[] BiomeIndicators;

    public float MinCameraZoom;
    public float MaxCameraZoom;
    //public Camera PlayerCamera;

    public Color DebugChunkColour;

    public bool DebugDrag;

    public int MaxDanger;
}

class PlayerDataBaker : Baker<PlayerDataAuthoring>
{
    public override void Bake(PlayerDataAuthoring authoring)
    {
        AddComponent(new PlayerData
        {
            VisibleStats = authoring.VisibleStats,
            HiddenStats = authoring.HiddenStats,
            MovementThreshold = authoring.MovementThreshold,
            MinNoiseNotWalkable = authoring.MinNoiseNotWalkable,
            MinCameraZoom = authoring.MinCameraZoom,
            MaxCameraZoom = authoring.MaxCameraZoom,
            DebugChunkColour = authoring.DebugChunkColour,
            DebugDrag = authoring.DebugDrag,
            MaxDanger = authoring.MaxDanger
        });
    }
}
