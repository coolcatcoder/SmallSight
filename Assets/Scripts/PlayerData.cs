using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct PlayerData : IComponentData
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

    //public int UIState;
}
