using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public class MapMaker : MonoBehaviour
{
    public int2 TilemapSize;
    public uint MaxSeed;

    public float3 MinBiomeSeed;
    public float3 MaxBiomeSeed;

    public float BiomeNoiseScale;

    public float TerrainNoiseScale;

    public int2 MaxTeleportBounds;
    public int2 MinTeleportBounds;

    public int WorldIndex = 0;

    public int BlockBatchSize = 32;

    public GameObject ColourMarker;
}

public class MapMakerBaker : Baker<MapMaker>
{
    public override void Bake(MapMaker authoring)
    {
        AddComponent(GetEntity(TransformUsageFlags.None),new MapData
        {
            TilemapSize = authoring.TilemapSize,
            MaxSeed = authoring.MaxSeed,
            MinBiomeSeed = authoring.MinBiomeSeed,
            MaxBiomeSeed = authoring.MaxBiomeSeed,
            BiomeNoiseScale = authoring.BiomeNoiseScale,
            TerrainNoiseScale = authoring.TerrainNoiseScale,
            MaxTeleportBounds = authoring.MaxTeleportBounds,
            MinTeleportBounds = authoring.MinTeleportBounds,
            WorldIndex = authoring.WorldIndex,
            BlockBatchSize = authoring.BlockBatchSize,
            ColourMarker = GetEntity(authoring.ColourMarker, TransformUsageFlags.Dynamic)
        });
    }
}

[ChunkSerializable] //weird
public struct MapData : IComponentData
{
    public int2 TilemapSize;

    public uint Seed;
    public uint MaxSeed;

    public float3 MinBiomeSeed;
    public float3 MaxBiomeSeed;

    public float3 BiomeSeed;
    public float BiomeNoiseScale;

    public float TerrainNoiseScale;

    public int2 MaxTeleportBounds;
    public int2 MinTeleportBounds;

    public Unity.Mathematics.Random RandStruct;

    public bool RestartGame;
    public bool KeepStats;

    public int WorldIndex;

    public bool Is3D;
    public float Quality;

    public int BlockBatchSize;

    public Optimisation OptimisationTechnique;
    public DebugFeatures DebugStuff;

    public Entity ColourMarker;

    public int FramesSinceLastMovement;
    public int2 PreviousSpiralPos;

    public int BlocksToSpiral;
    public int MaxBlocksToSpiral;

    public bool HasMoved;
}

public struct GridCell
{
    public bool Generated;
    public bool Empty;

    public int StrengthToWalkOn;
    public bool ConsumeOnCollision;
    public bool TeleportSafe;
    public float YLevel;

    public float4 VisibleStatsChange;
    public float4 HiddenStatsChange;

    public SpecialBehaviour Behaviour;

    public AlmanacWorld SectionIn;
    public int PageOn;

    //public Entity DecorationEntity; replace with decoration information
}

public struct TilemapMeshData : IComponentData
{
    public NativeList<int3> BlocksInMesh;
    public bool MakeMesh;
}

public static class MapExtensionMethods
{
    public static void RandomiseSeeds(ref this MapData MapInfo)
    {
        MapInfo.Seed = (uint)UnityEngine.Random.Range(0, MapInfo.MaxSeed);
        MapInfo.RandStruct = Unity.Mathematics.Random.CreateFromIndex(MapInfo.Seed);
        MapInfo.BiomeSeed = MapInfo.RandStruct.NextFloat3(MapInfo.MinBiomeSeed, MapInfo.MaxBiomeSeed);
    }

    [BurstCompile]
    public static Color GetBiomeColour2D(this MapData MapInfo, int2 Pos)
    {
        float2 SeededPos1 = Pos;
        SeededPos1.x += MapInfo.BiomeSeed.x;

        float2 SeededPos2 = Pos;
        SeededPos2.x += MapInfo.BiomeSeed.y;

        float2 SeededPos3 = Pos;
        SeededPos3.x += MapInfo.BiomeSeed.z;

        float3 CurrentBiomeNoise = new float3(noise.snoise(SeededPos1 * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos2 * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos3 * MapInfo.BiomeNoiseScale));

        return new Color(
                (CurrentBiomeNoise.x + 1) / 2,
                (CurrentBiomeNoise.y + 1) / 2,
                (CurrentBiomeNoise.z + 1) / 2
                );
    }

    [BurstCompile]
    public static Color GetBiomeColour3D(this MapData MapInfo, int3 Pos)
    {
        float3 SeededPos1 = Pos;
        SeededPos1.x += MapInfo.BiomeSeed.x;

        float3 SeededPos2 = Pos;
        SeededPos2.x += MapInfo.BiomeSeed.y;

        float3 SeededPos3 = Pos;
        SeededPos3.x += MapInfo.BiomeSeed.z;

        float3 CurrentBiomeNoise = new float3(noise.snoise(SeededPos1 * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos2 * MapInfo.BiomeNoiseScale), noise.snoise(SeededPos3 * MapInfo.BiomeNoiseScale));

        return new Color(
                (CurrentBiomeNoise.x + 1) / 2,
                (CurrentBiomeNoise.y + 1) / 2,
                (CurrentBiomeNoise.z + 1) / 2
                );
    }

    [BurstCompile]
    public static float GetNoise2D(this MapData MapInfo, int2 Pos, BiomeData Biome)
    {
        float2 SeededPos = Pos;
        SeededPos.x += MapInfo.Seed;
        return noise.snoise(SeededPos * (MapInfo.TerrainNoiseScale + Biome.ExtraTerrainNoiseScale));
    }

    [BurstCompile]
    public static float GetNoise3D(this MapData MapInfo, int3 Pos, BiomeData Biome)
    {
        float3 SeededPos = Pos;
        SeededPos.x += MapInfo.Seed;
        return noise.snoise(SeededPos * (MapInfo.TerrainNoiseScale + Biome.ExtraTerrainNoiseScale));
    }
}

//[BurstCompile]
//[UpdateInGroup(typeof(SimulationSystemGroup))]
//public partial struct MapSystem : ISystem, ISystemStartStop
//{
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<InputData>();
//        state.RequireForUpdate<PlayerData>();
//        state.RequireForUpdate<MapData>();
//        state.RequireForUpdate<UIData>();
//    }

//    public void OnStartRunning(ref SystemState state)
//    {

//    }

//    //[BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

//        if (MapInfo.DebugStuff == DebugFeatures.ShowTrueBiomeColour)
//        {
//            UpdateColourDebug(ref MapInfo, ref state);
//        }
//        else if (!MapInfo.Is3D)
//        {
//            Update2D(ref MapInfo, ref state);
//        }
//        else
//        {
//            Update3D(ref MapInfo, ref state);
//        }
        
//    }

//    public void OnStopRunning(ref SystemState state)
//    {
        
//    }

//    public void OnDestroy(ref SystemState state)
//    {

//    }

//    #region Updates

//    public void Update2D(ref MapData MapInfo, ref SystemState state)
//    {
//        return;
//        bool HasRestarted = false;

//        if (MapInfo.RestartGame)
//        {
//            //MapInfo.RestartGame = false;
//            HasRestarted = true;
//            MapInfo.RandomiseSeeds();
//            MapInfo.GeneratedBlocks2D.Clear();

//            state.EntityManager.DestroyEntity(MapInfo.ResetQuery);

//            if (!MapInfo.KeepStats)
//            {
//                ref PlayerData PlayerInfoRW = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
//                PlayerInfoRW.VisibleStats = PlayerInfoRW.DefaultVisibleStats;
//                PlayerInfoRW.HiddenStats = PlayerInfoRW.DefaultHiddenStats;
//            }
//            else
//            {
//                MapInfo.KeepStats = false;
//            }

//            AddBlocksJob AddBlocks = new AddBlocksJob
//            {
//                WorldIndex = MapInfo.WorldIndex,
//                GeneratedBlocks = MapInfo.GeneratedBlocks2D
//            };

//            state.Dependency = AddBlocks.Schedule(state.Dependency);
//            state.Dependency.Complete();

//            ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
//            UIInfo.UIState = UIStatus.Alive;
//            UIInfo.Setup = false;

//            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW.Position.xz = FindSafePos2D(ref state);

//            ref WorldData WorldInfo = ref SystemAPI.GetSingletonBuffer<WorldData>().ElementAt(MapInfo.WorldIndex);
//            RenderSettings.skybox.SetColor("_GroundColor", WorldInfo.BackGround);
//            RenderSettings.skybox.SetColor("_SkyTint", WorldInfo.BackGround);

//            SystemAPI.GetSingletonRW<PlayerData>().ValueRW.JustTeleported = true;
//        }

//        bool HasMoved = MovePlayer2D(ref state);

//        PlayerData PlayerInfo = SystemAPI.GetSingleton<PlayerData>();
//        int2 PlayerPos = (int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz;

//        if (HasMoved || HasRestarted)
//        {
//            int BlocksToSearch = PlayerInfo.GenerationThickness * PlayerInfo.GenerationThickness;

//            NativeList<BlockGenerator2D> BlocksToGenerate = new NativeList<BlockGenerator2D>(BlocksToSearch, Allocator.Persistent);

//            var BlocksToGenerateJob = new BlocksToGenerate2DJob()
//            {
//                Blocks = BlocksToGenerate.AsParallelWriter(),
//                GeneratedBlocks = MapInfo.GeneratedBlocks2D,
//                GenerationPos = PlayerPos,
//                GenerationThickness = PlayerInfo.GenerationThickness
//            };

//            BlocksToGenerateJob.Schedule(BlocksToSearch, MapInfo.BlockBatchSize).Complete();

//            for (int i = 0; i < BlocksToGenerate.Length; i++)
//            {
//                BlocksToGenerate.ElementAt(i).BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(BlocksToGenerate[i].Pos), ref state);
//            }

//            for (int i = 0; i < BlocksToGenerate.Length; i++)
//            {
//                GenerateBlock2DChaos(BlocksToGenerate[i], ref MapInfo, ref state);
//            }

//            BlocksToGenerate.Dispose();

//            //Debug.Log("The player is going places!");
//            MapInfo.FramesSinceLastMovement = 1;
//            MapInfo.PreviousSpiralPos = new int2(0, 0);
//        }
//        else
//        {
//            MapInfo.FramesSinceLastMovement++;

//            if (MapInfo.OptimisationTechnique == Optimisation.Random)
//            {
//                for (int i = 0; i < PlayerInfo.RandomsPerFrame; i++)
//                {
//                    BlockGenerator2D RandBlock = new()
//                    {
//                        Pos = MapInfo.RandStruct.NextInt2(PlayerPos - new int2(PlayerInfo.RandomDistance, PlayerInfo.RandomDistance), PlayerPos + new int2(PlayerInfo.RandomDistance, PlayerInfo.RandomDistance))
//                    };

//                    if (!MapInfo.GeneratedBlocks2D.ContainsKey(RandBlock.Pos))
//                    {
//                        RandBlock.BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(RandBlock.Pos), ref state);
//                        GenerateBlock2D(RandBlock, ref MapInfo, ref state);
//                    }
//                }
//            }

//            if (MapInfo.OptimisationTechnique == Optimisation.Spiral)
//            {
//                if (MapInfo.FramesSinceLastMovement < MapInfo.MaxBlocksToSpiral)
//                {
//                    NativeReference<int2> JobPrevPos = new NativeReference<int2>(Allocator.TempJob);
//                    JobPrevPos.Value = MapInfo.PreviousSpiralPos;
//                    NativeList<int2> SpiralPositions = new NativeList<int2>(MapInfo.BlocksToSpiral, Allocator.TempJob);

//                    SpiralPosJob PosJob = new SpiralPosJob()
//                    {
//                        PreviousPosition = JobPrevPos,
//                        Index = MapInfo.FramesSinceLastMovement,
//                        Quantity = MapInfo.BlocksToSpiral,
//                        Blocks = SpiralPositions
//                    };

//                    PosJob.Schedule().Complete();

//                    MapInfo.PreviousSpiralPos = JobPrevPos.Value;
//                    JobPrevPos.Dispose();

//                    MapInfo.FramesSinceLastMovement += MapInfo.BlocksToSpiral - 1;

//                    for (int i = 0; i < MapInfo.BlocksToSpiral; i++)
//                    {
//                        if (!MapInfo.GeneratedBlocks2D.ContainsKey(SpiralPositions[i] + PlayerPos))
//                        {
//                            BlockGenerator2D SpiralBlock = new()
//                            {
//                                Pos = SpiralPositions[i] + PlayerPos
//                            };

//                            SpiralBlock.BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(SpiralBlock.Pos), ref state);
//                            GenerateBlock2D(SpiralBlock, ref MapInfo, ref state);
//                        }
//                    }

//                    SpiralPositions.Dispose();

//                    //MapInfo.PreviousSpiralPos = GetNextSpiralPos(MapInfo.PreviousSpiralPos, MapInfo.FramesSinceLastMovement);
//                }
//                else
//                {
//                    MapInfo.FramesSinceLastMovement--;
//                }
//            }
//            else
//            {
//                MapInfo.FramesSinceLastMovement = 1;
//                MapInfo.PreviousSpiralPos = new int2(0, 0);
//            }
//        }
//    }

//    public void Update3D(ref MapData MapInfo, ref SystemState state)
//    {
//        if (MapInfo.RestartGame)
//        {
//            MapInfo.RestartGame = false;
//            MapInfo.RandomiseSeeds();
//            MapInfo.GeneratedBlocks3D.Clear();

//            state.EntityManager.DestroyEntity(MapInfo.ResetQuery);

//            if (!MapInfo.KeepStats)
//            {
//                ref PlayerData PlayerInfoRW = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
//                PlayerInfoRW.VisibleStats = PlayerInfoRW.DefaultVisibleStats;
//                PlayerInfoRW.HiddenStats = PlayerInfoRW.DefaultHiddenStats;
//            }
//            else
//            {
//                MapInfo.KeepStats = false;
//            }

//            ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
//            UIInfo.UIState = UIStatus.Alive;
//            UIInfo.Setup = false;

//            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW.Position = FindSafePos3D(ref state);

//            ref WorldData WorldInfo = ref SystemAPI.GetSingletonBuffer<WorldData>().ElementAt(MapInfo.WorldIndex);
//            RenderSettings.skybox.SetColor("_GroundColor", WorldInfo.BackGround);
//            RenderSettings.skybox.SetColor("_SkyTint", WorldInfo.BackGround);

//            SystemAPI.GetSingletonRW<PlayerData>().ValueRW.JustTeleported = true;
//        }

//        MovePlayer3D(ref state);

//        PlayerData PlayerInfo = SystemAPI.GetSingleton<PlayerData>();

//        int BlocksToSearch = PlayerInfo.GenerationThickness * PlayerInfo.GenerationThickness * PlayerInfo.GenerationThickness;

//        NativeList<BlockGenerator3D> BlocksToGenerate = new NativeList<BlockGenerator3D>(BlocksToSearch, Allocator.Persistent);

//        var BlocksToGenerateJob = new BlocksToGenerate3DJob()
//        {
//            Blocks = BlocksToGenerate.AsParallelWriter(),
//            GeneratedBlocks = MapInfo.GeneratedBlocks3D,
//            GenerationPos = (int3)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position,
//            GenerationThickness = PlayerInfo.GenerationThickness,
//            GenerationThicknessSquared = PlayerInfo.GenerationThickness*PlayerInfo.GenerationThickness
//        };

//        BlocksToGenerateJob.Schedule(BlocksToSearch, MapInfo.BlockBatchSize).Complete();

//        for (int i = 0; i < BlocksToGenerate.Length; i++)
//        {
//            BlocksToGenerate.ElementAt(i).BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour3D(BlocksToGenerate[i].Pos), ref state);
//        }

//        for (int i = 0; i < BlocksToGenerate.Length; i++)
//        {
//            GenerateBlock3D(BlocksToGenerate[i], ref MapInfo, ref state);
//        }

//        BlocksToGenerate.Dispose();
//    }

//    public void UpdateColourDebug(ref MapData MapInfo, ref SystemState state)
//    {
//        if (MapInfo.RestartGame)
//        {
//            MapInfo.RestartGame = false;
//            MapInfo.RandomiseSeeds();
//            MapInfo.GeneratedBlocks2D.Clear();

//            state.EntityManager.DestroyEntity(MapInfo.ResetQuery);

//            if (!MapInfo.KeepStats)
//            {
//                ref PlayerData PlayerInfoRW = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
//                PlayerInfoRW.VisibleStats = PlayerInfoRW.DefaultVisibleStats;
//                PlayerInfoRW.HiddenStats = PlayerInfoRW.DefaultHiddenStats;
//            }
//            else
//            {
//                MapInfo.KeepStats = false;
//            }

//            ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
//            UIInfo.UIState = UIStatus.Alive;
//            UIInfo.Setup = false;

//            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW.Position.xz = FindSafePos2D(ref state);

//            ref WorldData WorldInfo = ref SystemAPI.GetSingletonBuffer<WorldData>().ElementAt(MapInfo.WorldIndex);
//            RenderSettings.skybox.SetColor("_GroundColor", WorldInfo.BackGround);
//            RenderSettings.skybox.SetColor("_SkyTint", WorldInfo.BackGround);

//            SystemAPI.GetSingletonRW<PlayerData>().ValueRW.JustTeleported = true;
//        }

//        MovePlayerColourDebug(ref state, ref MapInfo);

//        //GenerateBlock((int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz, ref SystemAPI.GetSingletonRW<MapData>().ValueRW, ref state);

//        PlayerData PlayerInfo = SystemAPI.GetSingleton<PlayerData>();
//        int2 PlayerPos = (int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz;

//        int BlocksToSearch = PlayerInfo.GenerationThickness * PlayerInfo.GenerationThickness;

//        NativeList<BlockGeneratorColourDebug> BlocksToGenerate = new NativeList<BlockGeneratorColourDebug>(BlocksToSearch, Allocator.Persistent);

//        var BlocksToGenerateJob = new BlocksToGenerateColourDebugJob()
//        {
//            Blocks = BlocksToGenerate.AsParallelWriter(),
//            GeneratedBlocks = MapInfo.GeneratedBlocks2D,
//            GenerationPos = PlayerPos,
//            GenerationThickness = PlayerInfo.GenerationThickness
//        };

//        BlocksToGenerateJob.Schedule(BlocksToSearch, MapInfo.BlockBatchSize).Complete();

//        for (int i = 0; i < BlocksToGenerate.Length; i++)
//        {
//            BlocksToGenerate.ElementAt(i).BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(BlocksToGenerate[i].Pos), ref state);
//            BlocksToGenerate.ElementAt(i).TrueColour = MapInfo.GetBiomeColour2D(BlocksToGenerate.ElementAt(i).Pos);
//        }

//        for (int i = 0; i < BlocksToGenerate.Length; i++)
//        {
//            GenerateBlockColourDebug(BlocksToGenerate[i], ref MapInfo, ref state);
//        }

//        if (MapInfo.OptimisationTechnique == Optimisation.Random && BlocksToGenerate.Length == 0)
//        {
//            for (int i = 0; i < PlayerInfo.RandomsPerFrame; i++)
//            {
//                BlockGeneratorColourDebug RandBlock = new()
//                {
//                    Pos = MapInfo.RandStruct.NextInt2(PlayerPos - new int2(PlayerInfo.RandomDistance, PlayerInfo.RandomDistance), PlayerPos + new int2(PlayerInfo.RandomDistance, PlayerInfo.RandomDistance))
//                };

//                if (!MapInfo.GeneratedBlocks2D.ContainsKey(RandBlock.Pos))
//                {
//                    RandBlock.BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(RandBlock.Pos), ref state);
//                    RandBlock.TrueColour = MapInfo.GetBiomeColour2D(RandBlock.Pos);
//                    GenerateBlockColourDebug(RandBlock, ref MapInfo, ref state);
//                }
//            }
//        }

//        BlocksToGenerate.Dispose();
//    }

//    #endregion

//    public void SetupMapData(ref SystemState state)
//    {
//        ref MapDataWithoutBlocks MD = ref SystemAPI.GetSingletonRW<MapDataWithoutBlocks>().ValueRW;
//        Entity MapEntity = SystemAPI.GetSingletonEntity<MapDataWithoutBlocks>();

//        state.EntityManager.AddComponent<MapData>(MapEntity);
//        state.EntityManager.SetComponentData(MapEntity, new MapData
//        {
//            MaxBlocks = MD.MaxBlocks,
//            Seed = MD.Seed,
//            MaxSeed = MD.MaxSeed,
//            MinBiomeSeed = MD.MinBiomeSeed,
//            MaxBiomeSeed = MD.MaxBiomeSeed,
//            BiomeSeed = MD.BiomeSeed,
//            BiomeNoiseScale = MD.BiomeNoiseScale,
//            TerrainNoiseScale = MD.TerrainNoiseScale,
//            MaxTeleportBounds = MD.MaxTeleportBounds,
//            MinTeleportBounds = MD.MinTeleportBounds,
//            RandStruct = MD.RandStruct,
//            RestartGame = MD.RestartGame,
//            WorldIndex = MD.WorldIndex,
//            BlockBatchSize = MD.BlockBatchSize,
//            ColourMarker = MD.ColourMarker
//        });
//    }

//    public int2 FindSafePos2D(ref SystemState state)
//    {
//        var SafePos = false;
//        int2 BPos = new();

//        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

//        while (!SafePos)
//        {
//            BPos = MapInfo.RandStruct.NextInt2(MapInfo.MinTeleportBounds.xy, MapInfo.MaxTeleportBounds.xy);

//            BlockGenerator2D BlockGen = new BlockGenerator2D
//            {
//                Pos = BPos,
//                BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(BPos), ref state)
//            };

//            if (!MapInfo.GeneratedBlocks2D.ContainsKey(BPos))
//            {
//                GenerateBlock2D(BlockGen, ref MapInfo, ref state);
//            }

//            if (MapInfo.GeneratedBlocks2D[BPos] == Entity.Null)
//            {
//                SafePos = true;
//            }
//            else if (SystemAPI.GetComponent<BlockData>(MapInfo.GeneratedBlocks2D[BPos]).TeleportSafe)
//            {
//                SafePos = true;
//            }
//        }

//        return BPos;
//    }

//    public int3 FindSafePos3D(ref SystemState state)
//    {
//        var SafePos = false;
//        int3 BPos = new();

//        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

//        while (!SafePos)
//        {
//            BPos = MapInfo.RandStruct.NextInt3(MapInfo.MinTeleportBounds, MapInfo.MaxTeleportBounds);

//            BlockGenerator3D BlockGen = new BlockGenerator3D
//            {
//                Pos = BPos,
//                BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour3D(BPos), ref state)
//            };

//            if (!MapInfo.GeneratedBlocks3D.ContainsKey(BPos))
//            {
//                GenerateBlock3D(BlockGen, ref MapInfo, ref state);
//            }

//            if (MapInfo.GeneratedBlocks3D[BPos] == Entity.Null)
//            {
//                SafePos = true;
//            }
//            else if (SystemAPI.GetComponent<BlockData>(MapInfo.GeneratedBlocks3D[BPos]).TeleportSafe)
//            {
//                SafePos = true;
//            }
//        }

//        return BPos;
//    }

//    #region Movement

//    public bool MovePlayer2D(ref SystemState state) //dont like how the player and the map maker are in 1 file...
//    {
//        bool HasMoved = false;

//        ref InputData InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
//        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
//        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

//        if (PlayerInfo.VisibleStats.x <= 0 || UIInfo.UIState == UIStatus.MainMenu)
//        {
//            return false;
//        }

//        if (InputInfo.Teleport && PlayerInfo.VisibleStats.z > 0)
//        {
//            InputInfo.Teleport = false;
//            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;

//            PlayerTransform.Position.xz = FindSafePos2D(ref state);

//            PlayerInfo.VisibleStats.z--;
//            PlayerInfo.JustTeleported = true;

//            return true;
//        }

//        if (InputInfo.Pressed || (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement)))
//        {
//            InputInfo.Pressed = false;
//            if (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement))
//            {
//                InputInfo.TimeHeldFor = PlayerInfo.SecondsUntilHoldMovement - PlayerInfo.HeldMovementDelay;
//            }

//            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;
//            float3 NewPos = PlayerTransform.Position;

//            if (InputInfo.Movement.x >= PlayerInfo.MinInputDetected)
//            {
//                NewPos.x += 1;
//            }
//            else if (InputInfo.Movement.x <= -PlayerInfo.MinInputDetected)
//            {
//                NewPos.x -= 1;
//            }

//            if (InputInfo.Movement.y >= PlayerInfo.MinInputDetected)
//            {
//                NewPos.z += 1;
//            }
//            else if (InputInfo.Movement.y <= -PlayerInfo.MinInputDetected)
//            {
//                NewPos.z -= 1;
//            }

//            if (!math.all(NewPos == PlayerTransform.Position))
//            {
//                ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
//                //ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

//                if (MapInfo.GeneratedBlocks2D.TryGetValue((int2)NewPos.xz, out Entity BlockEntity))
//                {
//                    if (BlockEntity == Entity.Null)
//                    {
//                        PlayerTransform.Position = NewPos;
//                        HasMoved = true;

//                        if (PlayerInfo.PlayerSkills.HasFlag(Skills.Exhausted))
//                        {
//                            PlayerInfo.VisibleStats.y -= 2;
//                        }
//                        else
//                        {
//                            PlayerInfo.VisibleStats.y--;
//                        }


//                        if (PlayerInfo.VisibleStats.y < 0)
//                        {
//                            PlayerInfo.VisibleStats.x--;
//                        }

//                        if (PlayerInfo.VisibleStats.x <= 0)
//                        {
//                            UIInfo.UIState = UIStatus.Dead;
//                        }

//                        Color BiomeColour = MapInfo.GetBiomeColour2D((int2)NewPos.xz);
//                        UIInfo.BiomeColour = BiomeColour;
//                        UIInfo.BiomeName = SystemAPI.GetComponent<BiomeData>(GetBiomeEntity(MapInfo.WorldIndex, BiomeColour, ref state)).BiomeName;
//                    }
//                    else
//                    {
//                        BlockData BlockInfo = SystemAPI.GetComponent<BlockData>(BlockEntity);
//                        if (PlayerInfo.VisibleStats.w >= BlockInfo.StrengthToWalkOn)
//                        {
//                            if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.SkillToCross) && !(PlayerInfo.PlayerSkills.HasFlag(SystemAPI.GetComponent<SkillToCrossBehaviourData>(BlockEntity).Skill)))
//                            {
//                                return false;
//                            }

//                            if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.SkillStats))
//                            {
//                                SkillStatsBehaviourData SkillStatsInfo = SystemAPI.GetComponent<SkillStatsBehaviourData>(BlockEntity);

//                                if (PlayerInfo.PlayerSkills.HasFlag(SkillStatsInfo.Skill))
//                                {
//                                    PlayerInfo.VisibleStats += SkillStatsInfo.VisibleStatsChange;
//                                    PlayerInfo.HiddenStats += SkillStatsInfo.HiddenStatsChange;
//                                }
//                                else
//                                {
//                                    PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
//                                    PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;
//                                }
//                            }
//                            else
//                            {
//                                PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
//                                PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;
//                            }

//                            if (BlockInfo.ConsumeOnCollision && (!BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Replace)))
//                            {
//                                if (BlockInfo.DecorationEntity != Entity.Null)
//                                {
//                                    state.EntityManager.DestroyEntity(BlockInfo.DecorationEntity);
//                                }

//                                state.EntityManager.DestroyEntity(BlockEntity);
//                                MapInfo.GeneratedBlocks2D[(int2)NewPos.xz] = Entity.Null;
//                            }

//                            if (BlockInfo.Behaviour != SpecialBehaviour.None)
//                            {
//                                if (MapInfo.DebugStuff != DebugFeatures.NoWarps)
//                                {
//                                    if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Warp))
//                                    {
//                                        MapInfo.RestartGame = true;
//                                        MapInfo.KeepStats = true;

//                                        bool IsDangerous = MapInfo.RandStruct.NextFloat() < (PlayerInfo.ChanceOfDangerousWarp / 100f);
//                                        bool Outcome = !IsDangerous;
//                                        int WorldIndex = 0;
//                                        DynamicBuffer<WorldData> Worlds = SystemAPI.GetSingletonBuffer<WorldData>();

//                                        while (Outcome != IsDangerous)
//                                        {
//                                            WorldIndex = MapInfo.RandStruct.NextInt(0, Worlds.Length);
//                                            Outcome = Worlds[WorldIndex].Dangerous;
//                                        }

//                                        MapInfo.WorldIndex = WorldIndex;
//                                    }

//                                    if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.WarpToRuins))
//                                    {
//                                        PlayerTransform.Position = SystemAPI.GetComponent<RuinsWarpBehaviourData>(BlockEntity).RuinPos; //teleports them to whatever ruin it is
//                                    }
//                                }

//                                if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Replace))
//                                {
//                                    Entity NewBlock = state.EntityManager.Instantiate(SystemAPI.GetComponent<ReplaceBehaviourData>(BlockEntity).ReplacementBlock);
//                                    BlockData NewBlockInfo = SystemAPI.GetComponent<BlockData>(NewBlock);
//                                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(NewBlock, false).ValueRW.Position = new float3(NewPos.x, NewBlockInfo.YLevel, NewPos.z);

//                                    if (BlockInfo.DecorationEntity != Entity.Null)
//                                    {
//                                        state.EntityManager.DestroyEntity(BlockInfo.DecorationEntity);
//                                    }
//                                    state.EntityManager.DestroyEntity(BlockEntity);
//                                    MapInfo.GeneratedBlocks2D[(int2)NewPos.xz] = NewBlock;
//                                }
//                            }

//                            if (!BlockInfo.Behaviour.HasFlag(SpecialBehaviour.WarpToRuins))
//                            {
//                                PlayerTransform.Position = NewPos;
//                            }
//                            HasMoved = true;

//                            PlayerInfo.VisibleStats.y--;
//                            if (PlayerInfo.VisibleStats.y < 0)
//                            {
//                                PlayerInfo.VisibleStats.x--;
//                            }

//                            if (PlayerInfo.VisibleStats.x <= 0)
//                            {
//                                UIInfo.UIState = UIStatus.Dead;
//                            }

//                            Color BiomeColour = MapInfo.GetBiomeColour2D((int2)NewPos.xz);
//                            UIInfo.BiomeColour = BiomeColour;
//                            UIInfo.BiomeName = SystemAPI.GetComponent<BiomeData>(GetBiomeEntity(MapInfo.WorldIndex, BiomeColour, ref state)).BiomeName;
//                        }
//                    }
//                }
//            }
//            //else
//            //{
//            //    Debug.Log($"PT: {PlayerTransform.Position}\nNT: {NewPos}");
//            //}
//        }
//        return HasMoved;
//    }

//    public void MovePlayer3D(ref SystemState state)
//    {
//        ref InputData InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
//        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
//        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

//        if (PlayerInfo.VisibleStats.x <= 0 || UIInfo.UIState == UIStatus.MainMenu)
//        {
//            return;
//        }

//        if (InputInfo.Teleport && PlayerInfo.VisibleStats.z > 0)
//        {
//            InputInfo.Teleport = false;
//            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;

//            PlayerTransform.Position = FindSafePos3D(ref state);

//            PlayerInfo.VisibleStats.z--;
//            PlayerInfo.JustTeleported = true;
//        }

//        if (InputInfo.Pressed || (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement)))
//        {
//            InputInfo.Pressed = false;
//            if (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement))
//            {
//                InputInfo.TimeHeldFor = PlayerInfo.SecondsUntilHoldMovement - PlayerInfo.HeldMovementDelay;
//            }

//            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;
//            float3 NewPos = PlayerTransform.Position;

//            if (InputInfo.Movement.x >= PlayerInfo.MinInputDetected)
//            {
//                NewPos.x += 1;
//            }
//            else if (InputInfo.Movement.x <= -PlayerInfo.MinInputDetected)
//            {
//                NewPos.x -= 1;
//            }

//            if (InputInfo.YMovement.y >= PlayerInfo.MinInputDetected)
//            {
//                NewPos.y += 1;
//            }
//            else if (InputInfo.YMovement.y <= -PlayerInfo.MinInputDetected)
//            {
//                NewPos.y -= 1;
//            }

//            if (InputInfo.Movement.y >= PlayerInfo.MinInputDetected)
//            {
//                NewPos.z += 1;
//            }
//            else if (InputInfo.Movement.y <= -PlayerInfo.MinInputDetected)
//            {
//                NewPos.z -= 1;
//            }

//            if (!math.all(NewPos == PlayerTransform.Position))
//            {
//                ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
//                //ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

//                if (MapInfo.GeneratedBlocks3D.TryGetValue((int3)NewPos, out Entity BlockEntity))
//                {
//                    if (BlockEntity == Entity.Null)
//                    {
//                        PlayerTransform.Position = NewPos;

//                        if (PlayerInfo.PlayerSkills.HasFlag(Skills.Exhausted))
//                        {
//                            PlayerInfo.VisibleStats.y -= 2;
//                        }
//                        else
//                        {
//                            PlayerInfo.VisibleStats.y--;
//                        }


//                        if (PlayerInfo.VisibleStats.y < 0)
//                        {
//                            PlayerInfo.VisibleStats.x--;
//                        }

//                        if (PlayerInfo.VisibleStats.x <= 0)
//                        {
//                            UIInfo.UIState = UIStatus.Dead;
//                        }

//                        Color BiomeColour = MapInfo.GetBiomeColour3D((int3)NewPos);
//                        UIInfo.BiomeColour = BiomeColour;
//                        UIInfo.BiomeName = SystemAPI.GetComponent<BiomeData>(GetBiomeEntity(MapInfo.WorldIndex, BiomeColour, ref state)).BiomeName;
//                    }
//                    else
//                    {
//                        BlockData BlockInfo = SystemAPI.GetComponent<BlockData>(BlockEntity);
//                        if (PlayerInfo.VisibleStats.w >= BlockInfo.StrengthToWalkOn)
//                        {
//                            if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.SkillToCross) && !(PlayerInfo.PlayerSkills.HasFlag(SystemAPI.GetComponent<SkillToCrossBehaviourData>(BlockEntity).Skill)))
//                            {
//                                return;
//                            }

//                            if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.SkillStats))
//                            {
//                                SkillStatsBehaviourData SkillStatsInfo = SystemAPI.GetComponent<SkillStatsBehaviourData>(BlockEntity);

//                                if (PlayerInfo.PlayerSkills.HasFlag(SkillStatsInfo.Skill))
//                                {
//                                    PlayerInfo.VisibleStats += SkillStatsInfo.VisibleStatsChange;
//                                    PlayerInfo.HiddenStats += SkillStatsInfo.HiddenStatsChange;
//                                }
//                                else
//                                {
//                                    PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
//                                    PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;
//                                }
//                            }
//                            else
//                            {
//                                PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
//                                PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;
//                            }

//                            if (BlockInfo.ConsumeOnCollision && (!BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Replace)))
//                            {
//                                if (BlockInfo.DecorationEntity != Entity.Null)
//                                {
//                                    state.EntityManager.DestroyEntity(BlockInfo.DecorationEntity);
//                                }

//                                state.EntityManager.DestroyEntity(BlockEntity);
//                                MapInfo.GeneratedBlocks3D[(int3)NewPos] = Entity.Null;
//                            }

//                            if (BlockInfo.Behaviour != SpecialBehaviour.None)
//                            {
//                                if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Warp))
//                                {
//                                    MapInfo.RestartGame = true;
//                                    MapInfo.KeepStats = true;

//                                    bool IsDangerous = MapInfo.RandStruct.NextFloat() < (PlayerInfo.ChanceOfDangerousWarp / 100f);
//                                    bool Outcome = !IsDangerous;
//                                    int WorldIndex = 0;
//                                    DynamicBuffer<WorldData> Worlds = SystemAPI.GetSingletonBuffer<WorldData>();

//                                    while (Outcome != IsDangerous)
//                                    {
//                                        WorldIndex = MapInfo.RandStruct.NextInt(0, Worlds.Length);
//                                        Outcome = Worlds[WorldIndex].Dangerous;
//                                    }

//                                    MapInfo.WorldIndex = WorldIndex;
//                                }

//                                if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Replace))
//                                {
//                                    Entity NewBlock = state.EntityManager.Instantiate(SystemAPI.GetComponent<ReplaceBehaviourData>(BlockEntity).ReplacementBlock);
//                                    BlockData NewBlockInfo = SystemAPI.GetComponent<BlockData>(NewBlock);
//                                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(NewBlock, false).ValueRW.Position = new float3(NewPos.x, NewPos.y, NewPos.z);

//                                    //if (BlockInfo.DecorationEntity != Entity.Null)
//                                    //{
//                                    //    state.EntityManager.DestroyEntity(BlockInfo.DecorationEntity);
//                                    //}
//                                    state.EntityManager.DestroyEntity(BlockEntity);
//                                    MapInfo.GeneratedBlocks3D[(int3)NewPos] = NewBlock;
//                                }
//                            }

//                            PlayerTransform.Position = NewPos;

//                            PlayerInfo.VisibleStats.y--;
//                            if (PlayerInfo.VisibleStats.y < 0)
//                            {
//                                PlayerInfo.VisibleStats.x--;
//                            }

//                            if (PlayerInfo.VisibleStats.x <= 0)
//                            {
//                                UIInfo.UIState = UIStatus.Dead;
//                            }

//                            Color BiomeColour = MapInfo.GetBiomeColour3D((int3)NewPos);
//                            UIInfo.BiomeColour = BiomeColour;
//                            UIInfo.BiomeName = SystemAPI.GetComponent<BiomeData>(GetBiomeEntity(MapInfo.WorldIndex, BiomeColour, ref state)).BiomeName;
//                        }
//                    }
//                }
//            }
//            //else
//            //{
//            //    Debug.Log($"PT: {PlayerTransform.Position}\nNT: {NewPos}");
//            //}
//        }
//    }

//    public void MovePlayerColourDebug(ref SystemState state, ref MapData MapInfo)
//    {
//        ref InputData InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
//        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
//        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

//        if (PlayerInfo.VisibleStats.x <= 0 || UIInfo.UIState == UIStatus.MainMenu)
//        {
//            return;
//        }

//        if (InputInfo.Teleport && PlayerInfo.VisibleStats.z > 0)
//        {
//            InputInfo.Teleport = false;
//            MapInfo.RestartGame = true;
//            MapInfo.KeepStats = true;

//            bool IsDangerous = MapInfo.RandStruct.NextFloat() < (PlayerInfo.ChanceOfDangerousWarp / 100f);
//            bool Outcome = !IsDangerous;
//            int WorldIndex = 0;
//            DynamicBuffer<WorldData> Worlds = SystemAPI.GetSingletonBuffer<WorldData>();

//            while (Outcome != IsDangerous)
//            {
//                WorldIndex = MapInfo.RandStruct.NextInt(0, Worlds.Length);
//                Outcome = Worlds[WorldIndex].Dangerous;
//            }

//            MapInfo.WorldIndex = WorldIndex;
//        }

//        if (InputInfo.Pressed || (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement)))
//        {
//            InputInfo.Pressed = false;
//            if (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement))
//            {
//                InputInfo.TimeHeldFor = PlayerInfo.SecondsUntilHoldMovement - PlayerInfo.HeldMovementDelay;
//            }

//            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;
//            float3 NewPos = PlayerTransform.Position;

//            if (InputInfo.Movement.x >= PlayerInfo.MinInputDetected)
//            {
//                NewPos.x += 1;
//            }
//            else if (InputInfo.Movement.x <= -PlayerInfo.MinInputDetected)
//            {
//                NewPos.x -= 1;
//            }

//            if (InputInfo.Movement.y >= PlayerInfo.MinInputDetected)
//            {
//                NewPos.z += 1;
//            }
//            else if (InputInfo.Movement.y <= -PlayerInfo.MinInputDetected)
//            {
//                NewPos.z -= 1;
//            }

//            PlayerTransform.Position = NewPos;
//        }
//    }

//    #endregion

//    public void GenerateBlock2D(BlockGenerator2D BlockGenInfo, ref MapData MapInfo, ref SystemState state)
//    {
//        Entity BlockEntity = Entity.Null;

//        //Entity BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour(BlockGenInfo.Pos), ref state);
//        BiomeData Biome = SystemAPI.GetComponent<BiomeData>(BlockGenInfo.BiomeEntity);
//        float BlockNoise = MapInfo.GetNoise2D(BlockGenInfo.Pos, Biome);
//        //Debug.Log(BlockNoise);

//        DynamicBuffer<BiomeFeature> BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BlockGenInfo.BiomeEntity);

//        bool IsTerrain = false;

//        for (int i = 0; i < BiomeFeatures.Length; i++)
//        {
//            if (BiomeFeatures[i].IsTerrain && ((BlockNoise >= BiomeFeatures[i].MinNoiseValue) && (BlockNoise < BiomeFeatures[i].MaxNoiseValue)))
//            {
//                IsTerrain = true;
//                BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
//                SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

//                ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
//                if (BlockInfo.HasDecorations)
//                {
//                    var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
//                    if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
//                    {
//                        BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

//                        DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
//                        float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

//                        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
//                    }
//                }
//            }

//            //BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BiomeEntity); this should not be required
//        }

//        if (!IsTerrain)
//        {
//            for (int i = 0; i < BiomeFeatures.Length; i++)
//            {
//                if ((!BiomeFeatures[i].IsTerrain) && (MapInfo.RandStruct.NextFloat() < BiomeFeatures[i].PercentChanceToSpawn / 100))
//                {
//                    BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
//                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

//                    ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
//                    if (BlockInfo.HasDecorations)
//                    {
//                        var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
//                        if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
//                        {
//                            BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

//                            DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
//                            float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

//                            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
//                        }
//                    }

//                    break;
//                }
//            }
//        }

//        MapInfo.GeneratedBlocks2D.Add(BlockGenInfo.Pos, BlockEntity);
//    } //delete this

//    public void GenerateBlock2DChaos(BlockGenerator2D BlockGenInfo, ref MapData MapInfo, ref SystemState state)
//    {
//        BiomeData Biome = SystemAPI.GetComponent<BiomeData>(BlockGenInfo.BiomeEntity);
//        float BlockNoise = MapInfo.GetNoise2D(BlockGenInfo.Pos, Biome);

//        DynamicBuffer<BiomeFeature> BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BlockGenInfo.BiomeEntity);

//        int TerrainIndex = -1;
//        int OtherIndex = -1;

//        for (int i = 0; i < BiomeFeatures.Length; i++)
//        {
//            if (BiomeFeatures[i].IsTerrain && (BlockNoise >= BiomeFeatures[i].MinNoiseValue) && (BlockNoise < BiomeFeatures[i].MaxNoiseValue))
//            {
//                if (TerrainIndex == -1)
//                {
//                    TerrainIndex = i;
//                }
//            }
//            else if (MapInfo.RandStruct.NextFloat() < BiomeFeatures[i].PercentChanceToSpawn / 100)
//            {
//                if (OtherIndex == -1)
//                {
//                    OtherIndex = i;
//                }
//            }

//            if (TerrainIndex != -1 && OtherIndex != -1)
//            {
//                break;
//            }
//        }

//        if (TerrainIndex == -1 && OtherIndex == -1)
//        {
//            MapInfo.GeneratedBlocks2D.Add(BlockGenInfo.Pos, Entity.Null);
//        }
//        else
//        {
//            int FeatureIndex;
//            if (TerrainIndex != -1)
//            {
//                FeatureIndex = TerrainIndex;
//            }
//            else
//            {
//                FeatureIndex = OtherIndex;
//            }

//            Entity BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[FeatureIndex].FeaturePrefab);
//            MapInfo.GeneratedBlocks2D.Add(BlockGenInfo.Pos, BlockEntity);
//            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

//            ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
//            if (BlockInfo.HasDecorations)
//            {
//                var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
//                if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
//                {
//                    BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

//                    DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
//                    float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

//                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
//                }
//            }
//        }
//    }

//    public void GenerateBlock3D(BlockGenerator3D BlockGenInfo, ref MapData MapInfo, ref SystemState state)
//    {
//        Entity BlockEntity = Entity.Null;

//        //Entity BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour(BlockGenInfo.Pos), ref state);
//        BiomeData Biome = SystemAPI.GetComponent<BiomeData>(BlockGenInfo.BiomeEntity);
//        float BlockNoise = MapInfo.GetNoise3D(BlockGenInfo.Pos, Biome);
//        //Debug.Log(BlockNoise);

//        DynamicBuffer<BiomeFeature> BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BlockGenInfo.BiomeEntity);

//        bool IsTerrain = false;

//        for (int i = 0; i < BiomeFeatures.Length; i++)
//        {
//            if (BiomeFeatures[i].IsTerrain && ((BlockNoise >= BiomeFeatures[i].MinNoiseValue) && (BlockNoise < BiomeFeatures[i].MaxNoiseValue)))
//            {
//                IsTerrain = true;
//                BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
//                SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = BlockGenInfo.Pos; //new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

//                //ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
//                //if (BlockInfo.HasDecorations)
//                //{
//                //    var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
//                //    if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
//                //    {
//                //        BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

//                //        DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
//                //        float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

//                //        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
//                //    }
//                //}
//            }

//            //BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BiomeEntity); this should not be required
//        }

//        if (!IsTerrain)
//        {
//            for (int i = 0; i < BiomeFeatures.Length; i++)
//            {
//                if ((!BiomeFeatures[i].IsTerrain) && (MapInfo.RandStruct.NextFloat() < BiomeFeatures[i].PercentChanceToSpawn / 100))
//                {
//                    BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
//                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = BlockGenInfo.Pos; //new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

//                    //ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
//                    //if (BlockInfo.HasDecorations)
//                    //{
//                    //    var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
//                    //    if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
//                    //    {
//                    //        BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

//                    //        DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
//                    //        float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

//                    //        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
//                    //    }
//                    //}

//                    break;
//                }
//            }
//        }

//        MapInfo.GeneratedBlocks3D.Add(BlockGenInfo.Pos, BlockEntity);
//    }

//    public void GenerateBlockColourDebug(BlockGeneratorColourDebug BlockGenInfo, ref MapData MapInfo, ref SystemState state)
//    {
//        Entity EColour = state.EntityManager.Instantiate(MapInfo.ColourMarker);
//        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(EColour, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, 4, BlockGenInfo.Pos.y);
//        state.EntityManager.AddComponentData(EColour, new URPMaterialPropertyBaseColor { Value = new float4(BlockGenInfo.TrueColour.r, BlockGenInfo.TrueColour.g, BlockGenInfo.TrueColour.b, 1) });

//        Entity BlockEntity = Entity.Null;

//        //Entity BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour(BlockGenInfo.Pos), ref state);
//        BiomeData Biome = SystemAPI.GetComponent<BiomeData>(BlockGenInfo.BiomeEntity);
//        float BlockNoise = MapInfo.GetNoise2D(BlockGenInfo.Pos, Biome);
//        //Debug.Log(BlockNoise);

//        DynamicBuffer<BiomeFeature> BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BlockGenInfo.BiomeEntity);

//        bool IsTerrain = false;

//        for (int i = 0; i < BiomeFeatures.Length; i++)
//        {
//            if (BiomeFeatures[i].IsTerrain && ((BlockNoise >= BiomeFeatures[i].MinNoiseValue) && (BlockNoise < BiomeFeatures[i].MaxNoiseValue)))
//            {
//                IsTerrain = true;
//                BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
//                SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

//                ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
//                if (BlockInfo.HasDecorations)
//                {
//                    var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
//                    if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
//                    {
//                        BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

//                        DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
//                        float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

//                        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
//                    }
//                }
//            }

//            //BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BiomeEntity); this should not be required
//        }

//        if (!IsTerrain)
//        {
//            for (int i = 0; i < BiomeFeatures.Length; i++)
//            {
//                if ((!BiomeFeatures[i].IsTerrain) && (MapInfo.RandStruct.NextFloat() < BiomeFeatures[i].PercentChanceToSpawn / 100))
//                {
//                    BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
//                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

//                    ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
//                    if (BlockInfo.HasDecorations)
//                    {
//                        var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
//                        if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
//                        {
//                            BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

//                            DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
//                            float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

//                            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
//                        }
//                    }

//                    break;
//                }
//            }
//        }

//        MapInfo.GeneratedBlocks2D.Add(BlockGenInfo.Pos, BlockEntity);
//    }

//    public Entity GetBiomeEntity(int WorldIndex, Color BlockColour, ref SystemState state)
//    {
//        NativeReference<Entity> BEntity = new NativeReference<Entity>(Allocator.TempJob);
//        BiomeEntityJob BJob = new BiomeEntityJob
//        {
//            BlockColour = BlockColour,
//            WorldIndex = WorldIndex,
//            BiomeEntity = BEntity
//        };

//        state.Dependency = BJob.Schedule(state.Dependency);
//        //state.Dependency = BJob.ScheduleParallel(state.Dependency);
//        state.Dependency.Complete();

//        Entity BiomeEntity = BJob.BiomeEntity.Value;
//        BEntity.Dispose();

//        if (BiomeEntity == Entity.Null)
//        {
//            BiomeEntity = GetDefaultBiomeEntity(WorldIndex, ref state);
//        }

//        return BiomeEntity;
//    }

//    public Entity GetDefaultBiomeEntity(int WorldIndex, ref SystemState state)
//    {
//        NativeReference<Entity> BEntity = new NativeReference<Entity>(Allocator.TempJob);
//        DefaultBiomeJob BJob = new DefaultBiomeJob
//        {
//            WorldIndex = WorldIndex,
//            BiomeEntity = BEntity
//        };

//        state.Dependency = BJob.Schedule(state.Dependency);
//        state.Dependency.Complete();

//        Entity BiomeEntity = BJob.BiomeEntity.Value;
//        BEntity.Dispose();

//        if (BiomeEntity == Entity.Null)
//        {
//            Debug.Log("Every world needs a default biome....");
//        }

//        return BiomeEntity;
//    }

//    [BurstCompile]
//    public partial struct BiomeEntityJob : IJobEntity
//    {
//        [ReadOnly]
//        public Color BlockColour;

//        [ReadOnly]
//        public int WorldIndex;

//        public NativeReference<Entity> BiomeEntity;

//        void Execute(ref BiomeData Biome, Entity entity)
//        {
//            if (Biome.WorldIndex == WorldIndex && math.distance(((float4)(Vector4)Biome.ColourSpawn).xyz, ((float4)(Vector4)BlockColour).xyz) <= Biome.MaxDistance)
//            {
//                BiomeEntity.Value = entity;
//            }
//        }
//    }

//    [BurstCompile]
//    public partial struct DefaultBiomeJob : IJobEntity
//    {
//        [ReadOnly]
//        public int WorldIndex;

//        public NativeReference<Entity> BiomeEntity;

//        void Execute(ref DefaultBiomeData DefaultBiome, Entity entity)
//        {
//            if (DefaultBiome.WorldIndex == WorldIndex)
//            {
//                BiomeEntity.Value = entity;
//            }
//        }
//    }

//    [BurstCompile]
//    struct BlocksToGenerate2DJob : IJobParallelFor
//    {
//        [WriteOnly]
//        public NativeList<BlockGenerator2D>.ParallelWriter Blocks;
//        [ReadOnly]
//        public NativeHashMap<int2, Entity> GeneratedBlocks;
//        [ReadOnly]
//        public int2 GenerationPos;
//        [ReadOnly]
//        public int GenerationThickness;
//        public void Execute(int i)
//        {
//            int2 Pos = IndexTo2DPos(i,GenerationThickness) + GenerationPos - new int2(GenerationThickness,GenerationThickness)/2;

//            if (!GeneratedBlocks.ContainsKey(Pos))
//            {
//                Blocks.AddNoResize(new BlockGenerator2D { Pos = Pos });
//            }
//        }

//        public static int2 IndexTo2DPos(int Index, int GenerationThickness)
//        {
//            return new int2(Index % GenerationThickness, Index / GenerationThickness);
//        }
//    }

//    [BurstCompile]
//    struct BlocksToGenerate3DJob : IJobParallelFor
//    {
//        [WriteOnly]
//        public NativeList<BlockGenerator3D>.ParallelWriter Blocks;
//        [ReadOnly]
//        public NativeHashMap<int3, Entity> GeneratedBlocks;
//        [ReadOnly]
//        public int3 GenerationPos;
//        [ReadOnly]
//        public int GenerationThickness;
//        [ReadOnly]
//        public int GenerationThicknessSquared;
//        public void Execute(int i)
//        {
//            int3 Pos = IndexTo3DPos(i, GenerationThickness, GenerationThicknessSquared) + GenerationPos - new int3(GenerationThickness, GenerationThickness, GenerationThickness) / 2;

//            if (!GeneratedBlocks.ContainsKey(Pos))
//            {
//                Blocks.AddNoResize(new BlockGenerator3D { Pos = Pos });
//            }
//        }

//        public static int3 IndexTo3DPos(int Index, int GenerationThickness, int GenerationThicknessSquared)
//        {
//            return new int3(Index % GenerationThickness, Index / GenerationThickness % GenerationThickness, Index / GenerationThicknessSquared);
//        }
//    }

//    [BurstCompile]
//    struct BlocksToGenerateColourDebugJob : IJobParallelFor
//    {
//        [WriteOnly]
//        public NativeList<BlockGeneratorColourDebug>.ParallelWriter Blocks;
//        [ReadOnly]
//        public NativeHashMap<int2, Entity> GeneratedBlocks;
//        [ReadOnly]
//        public int2 GenerationPos;
//        [ReadOnly]
//        public int GenerationThickness;
//        public void Execute(int i)
//        {
//            int2 Pos = IndexTo2DPos(i, GenerationThickness) + GenerationPos - new int2(GenerationThickness, GenerationThickness) / 2;

//            if (!GeneratedBlocks.ContainsKey(Pos))
//            {
//                Blocks.AddNoResize(new BlockGeneratorColourDebug { Pos = Pos });
//            }
//        }

//        public static int2 IndexTo2DPos(int Index, int GenerationThickness)
//        {
//            return new int2(Index % GenerationThickness, Index / GenerationThickness);
//        }
//    }

//    [BurstCompile]
//    public partial struct AddBlocksJob : IJobEntity
//    {
//        [ReadOnly]
//        public int WorldIndex;

//        [WriteOnly]
//        public NativeHashMap<int2, Entity> GeneratedBlocks;

//        void Execute(ref AddToWorldData BlockInfo, ref WorldTransform BlockTransform, Entity entity)
//        {
//            if (BlockInfo.WorldIndex == WorldIndex)
//            {
//                GeneratedBlocks.Add((int2)BlockTransform.Position.xz,entity);
//            }
//        }
//    }

//    //[BurstCompile]
//    struct SpiralPosJob : IJob
//    {
//        [WriteOnly]
//        public NativeList<int2> Blocks;

//        public NativeReference<int2> PreviousPosition;

//        [ReadOnly]
//        public int Index;
//        [ReadOnly]
//        public int Quantity;

//        public void Execute()
//        {
//            for (int i = 0; i < Quantity; i++)
//            {
//                PreviousPosition.Value = GetNextSpiralPos(PreviousPosition.Value, i+Index);
//                Blocks.Add(PreviousPosition.Value);
//            }
//        }

//        public static int2 GetNextSpiralPos(int2 PreviousPos, int SpiralIndex)
//        {
//            return new int2((int)math.round(PreviousPos.x + math.sin(math.floor(math.sqrt(4 * SpiralIndex - 7)) * math.PI / 2)), (int)math.round(PreviousPos.y + math.cos(math.floor(math.sqrt(4 * SpiralIndex - 7)) * math.PI / 2)));
//        }
//    }
//}

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
public partial struct Map2DStart : ISystem, ISystemStartStop
{
    NativeArray<GridCell> TilemapManager;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MapData>();
        state.RequireForUpdate<PlayerData>();
        state.RequireForUpdate<BiomeData>();
    }

    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        Entity MapEntity = SystemAPI.GetSingletonEntity<MapData>();

        state.EntityManager.AddComponent<TilemapMeshData>(MapEntity);

        ref TilemapMeshData TilemapMeshInfo = ref SystemAPI.GetSingletonRW<TilemapMeshData>().ValueRW;

        TilemapMeshInfo.BlocksInMesh = new NativeList<int3>(MapInfo.TilemapSize.x * MapInfo.TilemapSize.y, Allocator.Persistent);

        TilemapManager = new NativeArray<GridCell>(MapInfo.TilemapSize.x * MapInfo.TilemapSize.y, Allocator.Persistent);

        MapInfo.RandomiseSeeds();

        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        PlayerInfo.VisibleStats = PlayerInfo.DefaultVisibleStats;
        PlayerInfo.HiddenStats = PlayerInfo.DefaultHiddenStats;
    }

    //[BurstCompile] fix this asap!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    public void OnUpdate(ref SystemState state)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        if (MapInfo.RestartGame)
        {
            RestartGame(ref MapInfo, ref state);
        }

        PlayerData PlayerInfo = SystemAPI.GetSingleton<PlayerData>();
        int2 PlayerPos = (int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz;

        if (MapInfo.HasMoved || MapInfo.RestartGame)
        {
            Debug.Log("moved or restarted");
            GenerateBlocks(ref state, ref MapInfo, PlayerInfo.GenerationThickness, PlayerPos);
        }
    }

    [BurstCompile]
    public void OnStopRunning(ref SystemState state)
    {
        TilemapManager.Dispose();
    }

    public void OnDestroy(ref SystemState state)
    {

    }

    //[BurstCompile]
    public void GenerateBlocks(ref SystemState state, ref MapData MapInfo, int GenerationThickness, int2 CenterPosition)
    {
        int BlocksToSearch = GenerationThickness * GenerationThickness;

        NativeArray<int2> BlockPositions = new NativeArray<int2>(BlocksToSearch, Allocator.Persistent);
        NativeArray<float3> BlockBiomeNoise = new NativeArray<float3>(BlocksToSearch, Allocator.Persistent);
        NativeArray<Entity> BlockBiomes = new NativeArray<Entity>(BlocksToSearch, Allocator.Persistent);
        NativeArray<float> BlockTerrainNoise = new NativeArray<float>(BlocksToSearch, Allocator.Persistent);
        //dispose native containers above using dispose(JobHandle)!!

        var FindBlocks = new FindBlocksJob() // done
        {
            BlockPositions = BlockPositions,
            TilemapManager = TilemapManager,
            GenerationPos = CenterPosition,
            GenerationThickness = GenerationThickness
        };

        var GetBiomeNoise = new GetBiomeNoiseJob() //done
        {
            BlockPositions = BlockPositions,
            BlockBiomeNoise = BlockBiomeNoise,
            BiomeNoiseScale = MapInfo.BiomeNoiseScale,
            BiomeSeed = MapInfo.BiomeSeed
        };

        var GetDefaultBiome = new GetDefaultBiomeJob()
        {
            WorldIndex = MapInfo.WorldIndex,
            BlockBiomes = BlockBiomes
        };

        var GetBiomes = new GetBiomesJob()
        {
            BlockBiomeNoise = BlockBiomeNoise,
            BlockBiomes = BlockBiomes,
            WorldIndex = MapInfo.WorldIndex,
            BlockQuantity = BlocksToSearch
        };

        var GetNoise = new GetNoiseJob()
        {
            BlockPositions = BlockPositions,
            BlockTerrainNoise = BlockTerrainNoise,
            Seed = MapInfo.Seed,
            TerrainNoiseScale = MapInfo.TerrainNoiseScale
        };

        var GenerateBlocks = new GenerateBlocksJob()
        {
            DefaultBiome = DefaultBiome.Value,
            DefaultBiomeBlob = DefaultBiomeBlob.Value,
            ECB = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            RandStruct = MapInfo.RandStruct,
            BlockBiomes = BlockBiomes,
            BlockBiomeBlobs = BlockBiomeBlobs,
            BlockTerrainNoise = BlockTerrainNoise,
            BlockPositions = BlockPositions,
            EmptySpaces = EmptySpaces.AsParallelWriter()
        };

        var FindBlocksHandle = FindBlocks.ScheduleParallel(BlocksToSearch, MapInfo.BlockBatchSize, new JobHandle());
        var GetBiomeNoiseHandle = GetBiomeNoise.ScheduleParallel(BlocksToSearch, MapInfo.BlockBatchSize, FindBlocksHandle);
        var GetBiomesHandle = GetBiomes.Schedule(GetBiomeNoiseHandle); // no parallel sadly....
        var GetDefaultBiomeHandle = GetDefaultBiome.ScheduleParallel(GetBiomesHandle);
        var GetNoiseHandle = GetNoise.ScheduleParallel(BlocksToSearch, MapInfo.BlockBatchSize, GetDefaultBiomeHandle);
        var GenerateBlocksHandle = GenerateBlocks.ScheduleParallel(BlocksToSearch, MapInfo.BlockBatchSize, GetNoiseHandle);

        BlockPositions.Dispose(GenerateBlocksHandle);
        BlockBiomeNoise.Dispose(GenerateBlocksHandle);
        BlockBiomes.Dispose(GenerateBlocksHandle);
        BlockTerrainNoise.Dispose(GenerateBlocksHandle);
    }

    public unsafe static void Fill<T>(NativeArray<T> array, T value)
    where T : unmanaged
    {
        UnsafeUtility.MemCpyReplicate(array.GetUnsafePtr(), &value, UnsafeUtility.SizeOf<T>(), array.Length);
    }

    [BurstCompile]
    public static int PosToIndex(int2 Pos, int GridWidth)
    {
        return Pos.y * GridWidth + Pos.x;
    }

    [BurstCompile]
    public static int2 IndexToPos(int Index, int GridWidth)
    {
        return new int2(Index%GridWidth,Index/GridWidth);
    }

    [BurstCompile]
    public void RestartGame(ref MapData MapInfo, ref SystemState state)
    {
        MapInfo.RandomiseSeeds();
        unsafe { UnsafeUtility.MemClear(TilemapManager.GetUnsafePtr(), UnsafeUtility.SizeOf<GridCell>() * TilemapManager.Length); }; // acts like .Clear()

        if (!MapInfo.KeepStats)
        {
            ref PlayerData PlayerInfoRW = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
            PlayerInfoRW.VisibleStats = PlayerInfoRW.DefaultVisibleStats;
            PlayerInfoRW.HiddenStats = PlayerInfoRW.DefaultHiddenStats;
        }
        else
        {
            MapInfo.KeepStats = false;
        }

        //AddBlocksJob AddBlocks = new AddBlocksJob Fix asap!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //{
        //    WorldIndex = MapInfo.WorldIndex,
        //    GeneratedBlocks = TilemapManager
        //};

        //state.Dependency = AddBlocks.Schedule(state.Dependency);
        //state.Dependency.Complete();

        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
        UIInfo.UIState = UIStatus.Alive;
        UIInfo.Setup = false;

        //SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW.Position.xz = FindSafePos(ref state);

        ref WorldData WorldInfo = ref SystemAPI.GetSingletonBuffer<WorldData>().ElementAt(MapInfo.WorldIndex);
        //RenderSettings.skybox.SetColor("_GroundColor", WorldInfo.BackGround); // do this in a system base
        //RenderSettings.skybox.SetColor("_SkyTint", WorldInfo.BackGround);

        SystemAPI.GetSingletonRW<PlayerData>().ValueRW.JustTeleported = true;
    }

    //[BurstCompile]
    //public int2 FindSafePos(ref SystemState state, ref MapData MapInfo, ref TilemapData TilemapInfo)
    //{
    //    var SafePos = false;
    //    int2 BPos = new();

    //    while (!SafePos)
    //    {
    //        BPos = MapInfo.RandStruct.NextInt2(MapInfo.MinTeleportBounds.xy, MapInfo.MaxTeleportBounds.xy);

    //        BlockGenerator2D BlockGen = new BlockGenerator2D
    //        {
    //            Pos = BPos,
    //            BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(BPos), ref state)
    //        };

    //        if (!MapInfo.GeneratedBlocks2D.ContainsKey(BPos))
    //        {
    //            GenerateBlock2D(BlockGen, ref MapInfo, ref state);
    //        }

    //        if (MapInfo.GeneratedBlocks2D[BPos] == Entity.Null)
    //        {
    //            SafePos = true;
    //        }
    //        else if (SystemAPI.GetComponent<BlockData>(MapInfo.GeneratedBlocks2D[BPos]).TeleportSafe)
    //        {
    //            SafePos = true;
    //        }
    //    }
    //}

    [BurstCompile]
    struct FindBlocksJob : IJobFor
    {
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int2> BlockPositions;

        [ReadOnly]
        public NativeArray<GridCell> TilemapManager;

        [ReadOnly]
        public int2 GenerationPos;

        [ReadOnly]
        public int GenerationThickness;

        [ReadOnly]
        public int GridThickness;

        public void Execute(int i)
        {
            int2 Pos = IndexToPos(i, GenerationThickness) + GenerationPos - new int2(GenerationThickness, GenerationThickness) / 2;

            if (!TilemapManager[PosToIndex(Pos,GridThickness)].Generated)
            {
                BlockPositions[i] = Pos;
            }
        }

        //public static int2 IndexTo2DPos(int Index, int GenerationThickness)
        //{
        //    return new int2(Index % GenerationThickness, Index / GenerationThickness);
        //}
    }

    [BurstCompile]
    struct GetBiomeNoiseJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<int2> BlockPositions;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> BlockBiomeNoise; // The plural of "noise" is "noises" but the plural of "biome noise" is not "biome noises"??????

        [ReadOnly]
        public float BiomeNoiseScale;

        [ReadOnly]
        public float3 BiomeSeed;

        public void Execute(int i)
        {
            BlockBiomeNoise[i] = GetBiomeColour(BiomeSeed, BiomeNoiseScale, BlockPositions[i]);
        }

        public static float3 GetBiomeColour(float3 BiomeSeed, float BiomeNoiseScale, int2 Pos)
        {
            float2 SeededPos1 = Pos;
            SeededPos1.x += BiomeSeed.x;

            float2 SeededPos2 = Pos;
            SeededPos2.x += BiomeSeed.y;

            float2 SeededPos3 = Pos;
            SeededPos3.x += BiomeSeed.z;

            // okay in the biome colour thingies during baking for each axis do *2-1 to get the colours to be float4 in the range of -1 to 1
            return new float3(noise.snoise(SeededPos1 * BiomeNoiseScale), noise.snoise(SeededPos2 * BiomeNoiseScale), noise.snoise(SeededPos3 * BiomeNoiseScale));
        }
    }

    [BurstCompile]
    public partial struct GetBiomesJob : IJobEntity // this won't work in parallel ugh
    {
        [ReadOnly]
        public NativeArray<float3> BlockBiomeNoise;

        [WriteOnly]
        public NativeArray<Entity> BlockBiomes;

        [ReadOnly]
        public int WorldIndex;

        [ReadOnly]
        public int BlockQuantity;

        void Execute(BiomeData Biome, Entity entity)
        {
            if (Biome.WorldIndex == WorldIndex)
            {
                for (int i = 0; i < BlockQuantity; i++)
                {
                    if (math.distancesq(Biome.ColourSpawn, BlockBiomeNoise[i]) <= Biome.MaxDistance)
                    {
                        BlockBiomes[i] = entity;
                    }
                }
            }
        }
    }

    [BurstCompile]
    public partial struct GetDefaultBiomeJob : IJobEntity
    {
        [ReadOnly]
        public int WorldIndex;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<Entity> BlockBiomes;

        void Execute(DefaultBiomeData DefaultBiome, Entity entity)
        {
            if (DefaultBiome.WorldIndex == WorldIndex)
            {
                Fill(BlockBiomes, entity);
            }
        }
    }

    [BurstCompile]
    struct GetNoiseJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<int2> BlockPositions;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> BlockTerrainNoise;

        [ReadOnly]
        public uint Seed;

        [ReadOnly]
        public float TerrainNoiseScale;

        public void Execute(int i)
        {
            BlockTerrainNoise[i] = GetNoise(BlockPositions[i], Seed, TerrainNoiseScale, 0); //replace 0 with however I'm doing the biomes!
        }

        public static float GetNoise(int2 Pos, uint Seed, float TerrainNoiseScale, float ExtraTerrainNoiseScale)
        {
            float2 SeededPos = Pos;
            SeededPos.x += Seed;
            return noise.snoise(SeededPos * (TerrainNoiseScale + ExtraTerrainNoiseScale));
        }
    }

    //[BurstCompile]
    partial struct GenerateBlocksJob : IJobEntity
    {
        [ReadOnly]
        public int BlockQuantity;

        [ReadOnly]
        public int GridWidth;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<GridCell> TilemapManager;

        [WriteOnly]
        public NativeList<int3>.ParallelWriter BlocksToRender;

        [ReadOnly]
        public NativeArray<Entity> BlockBiomes;

        [ReadOnly]
        public NativeArray<float> BlockTerrainNoise;

        [ReadOnly]
        public NativeArray<int2> BlockPositions;

        public void Execute(ref BiomeData BiomeInfo, ref DynamicBuffer<BiomeFeatureElement> BiomeFeatures, Entity entity)
        {
            for (int i = 0; i < BlockQuantity; i++)
            {
                if (entity == BlockBiomes[i])
                {
                    int TerrainIndex = -1;
                    int OtherIndex = -1;

                    for (int k = 0; k < BiomeFeatures.Length; k++)
                    {
                        if (BiomeFeatures[k].IsTerrain && (BlockTerrainNoise[i] >= BiomeFeatures[k].MinNoiseValue) && (BlockTerrainNoise[i] < BiomeFeatures[k].MaxNoiseValue))
                        {
                            if (TerrainIndex == -1)
                            {
                                TerrainIndex = i;
                            }
                        }
                        else if (BiomeInfo.BiomeRandom.NextFloat() < BiomeFeatures[k].PercentChanceToSpawn / 100)
                        {
                            if (OtherIndex == -1)
                            {
                                OtherIndex = i;
                            }
                        }

                        if (TerrainIndex != -1 && OtherIndex != -1) // || or && I don't know!
                        {
                            break;
                        }
                    }

                    if (TerrainIndex == -1 && OtherIndex == -1)
                    {
                        ref GridCell BlockInfo = ref UnsafeElementAt(TilemapManager, PosToIndex(BlockPositions[i], GridWidth));
                        BlockInfo.Generated = true;
                        BlockInfo.Empty = true;
                    }
                    else
                    {
                        int FeatureIndex;

                        FeatureIndex = math.select(TerrainIndex, OtherIndex, TerrainIndex != -1); // if 3rd param, then 1st param, else 2nd param, nice and simple!

                        ref GridCell BlockInfo = ref UnsafeElementAt(TilemapManager, PosToIndex(BlockPositions[i], GridWidth));
                        BlockInfo.Generated = true;

                        BiomeFeatureElement BlockPrefabInfo = BiomeFeatures[FeatureIndex];

                        BlockInfo.StrengthToWalkOn = BlockPrefabInfo.StrengthToWalkOn;
                        BlockInfo.ConsumeOnCollision = BlockPrefabInfo.ConsumeOnCollision;
                        BlockInfo.TeleportSafe = BlockPrefabInfo.TeleportSafe;
                        BlockInfo.YLevel = BlockPrefabInfo.YLevel;

                        BlocksToRender.AddNoResize(new int3(BlockPositions[i].x, )

                        //Entity BlockEntity = ECB.Instantiate(i, BiomeFeatures[FeatureIndex].FeaturePrefab);

                        //ECB.SetComponent(i, BlockEntity, LocalTransform.FromPosition(new float3(BlockPositions[i].x, -1, BlockPositions[i].y))); // instead of -1 for the y, it should be SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel but you can't do this in a job, perhaps add another blob arary in the blob asset containing all the y values?

                        //Don't know how to replace this yet, todo asap though!
                        //ref BlockData PrefabBlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BiomeFeatures[FeatureIndex].FeaturePrefab, false).ValueRW;
                        //if (PrefabBlockInfo.HasDecorations)
                        //{
                        //    var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
                        //    if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
                        //    {
                        //        BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

                        //        DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
                        //        float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

                        //        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
                        //    }
                        //}
                    }
                }
            }
        }

        static unsafe ref T UnsafeElementAt<T>(NativeArray<T> array, int index) where T : struct
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }
    }

    [BurstCompile]
    struct AddEmptyBlocksToHashJob : IJob
    {
        [ReadOnly]
        public NativeList<int2> EmptySpaces;

        [WriteOnly]
        public NativeHashMap<int2, Entity> TilemapManager;

        public void Execute()
        {
            for(int i = 0; i < EmptySpaces.Length; i++)
            {
                TilemapManager.Add(EmptySpaces[i], Entity.Null);
            }
        }
    }

    [BurstCompile]
    public partial struct AddBlocksJob : IJobEntity
    {
        [ReadOnly]
        public int WorldIndex;

        [WriteOnly]
        public NativeHashMap<int2, Entity> GeneratedBlocks;

        void Execute(ref AddToWorldData BlockInfo, ref LocalToWorld BlockTransform, Entity entity)
        {
            if (BlockInfo.WorldIndex == WorldIndex)
            {
                GeneratedBlocks.Add((int2)BlockTransform.Position.xz, entity);
            }
        }
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(Map2DStart))]
public partial class MapMeshSystem : SystemBase
{
    NativeArray<VertexAttributeDescriptor> VertexAttributes;

    protected override void OnCreate()
    {
        RequireForUpdate<MapData>();

        VertexAttributes = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Persistent);
        VertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
    }

    protected override void OnUpdate()
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
        ref TilemapMeshData TilemapMeshInfo = ref SystemAPI.GetSingletonRW<TilemapMeshData>().ValueRW;

        if (TilemapMeshInfo.MakeMesh)
        {
            Entity MeshHolderEntity = SystemAPI.GetSingletonEntity<TilemapMeshHolderData>();
            ref var TilemapMeshComp = ref SystemAPI.GetComponentRW<MaterialMeshInfo>(MeshHolderEntity, false).ValueRW;
            var RenderMeshArrayInfo = EntityManager.GetSharedComponentManaged<RenderMeshArray>(MeshHolderEntity);

            Mesh TilemapMesh = RenderMeshArrayInfo.GetMesh(TilemapMeshComp);

            CreateMesh(TilemapMesh, ref TilemapMeshInfo);
        }
    }

    protected override void OnDestroy()
    {
        VertexAttributes.Dispose();
    }

    void CreateMesh(Mesh MeshToSet, ref TilemapMeshData TilemapMeshInfo)
    {
        int BlockQuantity = TilemapMeshInfo.BlocksInMesh.Length;

        Mesh.MeshDataArray OutputMeshArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData OutputMesh = OutputMeshArray[0];

        OutputMesh.SetVertexBufferParams(4 * BlockQuantity, VertexAttributes);

        OutputMesh.SetIndexBufferParams(6 * BlockQuantity, IndexFormat.UInt32);

        OutputMesh.subMeshCount = BlockQuantity;

        var ProcessMeshData = new ProcessMeshDataJob()
        {
            BlockPositions = TilemapMeshInfo.BlocksInMesh,
            OutputMesh = OutputMesh,
            BlockQuantity = BlockQuantity,
        };

        ProcessMeshData.ScheduleParallel(BlockQuantity, 64, new JobHandle()).Complete();

        Mesh.ApplyAndDisposeWritableMeshData(OutputMeshArray, MeshToSet, MeshUpdateFlags.Default);
    }

    struct Vertex // this has to match the VertexAttributes somehow
    {
        public float3 Pos;
    }

    [BurstCompile]
    struct ProcessMeshDataJob : IJobFor
    {
        [ReadOnly]
        public NativeList<int3> BlockPositions;

        //WriteOnly???
        public Mesh.MeshData OutputMesh;

        [ReadOnly]
        public int BlockQuantity;

        public void Execute(int i)
        {
            var Vertices = OutputMesh.GetVertexData<Vertex>();

            float3 BlockPosition = BlockPositions[i];

            UnsafeElementAt(Vertices, i * 4).Pos = BlockPosition + new float3(0.5f, 0, 0.5f); // top right
            UnsafeElementAt(Vertices, i * 4 + 1).Pos = BlockPosition + new float3(0.5f, 0, -0.5f); // top left
            UnsafeElementAt(Vertices, i * 4 + 2).Pos = BlockPosition + new float3(-0.5f, 0, 0.5f); // bottom right
            UnsafeElementAt(Vertices, i * 4 + 3).Pos = BlockPosition + new float3(-0.5f, 0, -0.5f); // bottom left

            var Indices = OutputMesh.GetIndexData<int>(); // shouldn't this be uint???

            Indices[i * 6] = i * 4 + 1;
            Indices[i * 6 + 1] = i * 4;
            Indices[i * 6 + 2] = i * 4 + 2;

            Indices[i * 6 + 3] = i * 4;
            Indices[i * 6 + 4] = i * 4 + 2;
            Indices[i * 6 + 5] = i * 4 + 3;

            SubMeshDescriptor SubMeshInfo = new()
            {
                baseVertex = 0, // for now this is correct, but will be an issue eventually
                //bounds = SubMeshBounds,
                //firstVertex = 0,
                indexCount = 6, // 2 triangles with each triangle needing 3
                indexStart = i*6, //potentially lol
                topology = MeshTopology.Triangles, // 3 indices per face
                //vertexCount = 4
            };

            OutputMesh.SetSubMesh(i, SubMeshInfo, MeshUpdateFlags.Default); //replace i with something, I don't know yet
        }

        static unsafe ref T UnsafeElementAt<T>(NativeArray<T> array, int index) where T : struct
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct Map2DEnd : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MapData>();
        state.RequireForUpdate<PlayerData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        if (MapInfo.RestartGame)
        {
            MapInfo.RestartGame = false;
        }

        if (MapInfo.HasMoved)
        {
            MapInfo.HasMoved = false;
        }
    }

    public void OnDestroy(ref SystemState state)
    {

    }
}
