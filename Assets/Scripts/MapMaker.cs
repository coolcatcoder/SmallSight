using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class MapMaker : MonoBehaviour
{
    public int MaxBlocks;
    public uint MaxSeed;

    public float3 MinBiomeSeed;
    public float3 MaxBiomeSeed;

    public float BiomeNoiseScale;

    public float TerrainNoiseScale;

    public int3 MaxTeleportBounds;
    public int3 MinTeleportBounds;

    public int WorldIndex = 0;

    public int BlockBatchSize = 32;

    public GameObject ColourMarker;
}

public class MapMakerBaker : Baker<MapMaker>
{
    public override void Bake(MapMaker authoring)
    {
        AddComponent(new MapDataWithoutBlocks
        {
            //GeneratedBlocks = new NativeHashMap<Unity.Mathematics.int2, Entity>(authoring.MaxBlocks, Allocator.Persistent),
            MaxBlocks = authoring.MaxBlocks,
            MaxSeed = authoring.MaxSeed,
            MinBiomeSeed = authoring.MinBiomeSeed,
            MaxBiomeSeed = authoring.MaxBiomeSeed,
            BiomeNoiseScale = authoring.BiomeNoiseScale,
            TerrainNoiseScale = authoring.TerrainNoiseScale,
            MaxTeleportBounds = authoring.MaxTeleportBounds,
            MinTeleportBounds = authoring.MinTeleportBounds,
            WorldIndex = authoring.WorldIndex,
            BlockBatchSize = authoring.BlockBatchSize,
            ColourMarker = GetEntity(authoring.ColourMarker)
        });
    }
}

//[ChunkSerializable]
public struct MapData : IComponentData
{
    public NativeHashMap<int2, Entity> GeneratedBlocks2D;
    public NativeHashMap<int3, Entity> GeneratedBlocks3D;

    public int MaxBlocks;

    public uint Seed;
    public uint MaxSeed;

    public float3 MinBiomeSeed;
    public float3 MaxBiomeSeed;

    public float3 BiomeSeed;
    public float BiomeNoiseScale;

    public float TerrainNoiseScale;

    public int3 MaxTeleportBounds;
    public int3 MinTeleportBounds;

    public Unity.Mathematics.Random RandStruct;

    public bool RestartGame;
    public bool KeepStats;

    public int WorldIndex;

    public bool Is3D;
    public float Quality;

    public EntityQuery ResetQuery;

    public int BlockBatchSize;

    public Optimisation OptimisationTechnique;
    public DebugFeatures DebugStuff;

    public Entity ColourMarker;

    public int FramesSinceLastMovement;
}

[ChunkSerializable]
public struct MapDataWithoutBlocks : IComponentData
{
    public int MaxBlocks;

    public uint Seed;
    public uint MaxSeed;

    public float3 MinBiomeSeed;
    public float3 MaxBiomeSeed;

    public float3 BiomeSeed;
    public float BiomeNoiseScale;

    public float TerrainNoiseScale;

    public int3 MaxTeleportBounds;
    public int3 MinTeleportBounds;

    public Unity.Mathematics.Random RandStruct;

    public bool RestartGame;

    public int WorldIndex;

    public int BlockBatchSize;

    public Entity ColourMarker;
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

public struct BlockGenerator2D
{
    public int2 Pos;
    public Entity BiomeEntity;
}

public struct BlockGenerator3D
{
    public int3 Pos;
    public Entity BiomeEntity;
}

public struct BlockGeneratorColourDebug
{
    public int2 Pos;
    public Entity BiomeEntity;
    public Color TrueColour;
}

public partial struct MapSystem : ISystem, ISystemStartStop
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InputData>();
        state.RequireForUpdate<PlayerData>();
        state.RequireForUpdate<MapDataWithoutBlocks>();
        state.RequireForUpdate<UIData>();
    }

    public void OnStartRunning(ref SystemState state)
    {
        SetupMapData(ref state);
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        MapInfo.GeneratedBlocks2D = new NativeHashMap<int2, Entity>(MapInfo.MaxBlocks, Allocator.Persistent);
        MapInfo.GeneratedBlocks3D = new NativeHashMap<int3, Entity>(MapInfo.MaxBlocks, Allocator.Persistent); // not a memory wise decision, this causes way more memory to be used than it should...

        MapInfo.RandomiseSeeds();

        MapInfo.ResetQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<DestroyOnRestartData>()
                .Build(ref state);

        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        PlayerInfo.VisibleStats = PlayerInfo.DefaultVisibleStats;
        PlayerInfo.HiddenStats = PlayerInfo.DefaultHiddenStats;
    }

    public void OnUpdate(ref SystemState state)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        if (MapInfo.DebugStuff == DebugFeatures.ShowTrueBiomeColour)
        {
            UpdateColourDebug(ref MapInfo, ref state);
        }
        else if (!MapInfo.Is3D)
        {
            Update2D(ref MapInfo, ref state);
        }
        else
        {
            Update3D(ref MapInfo, ref state);
        }
        
    }

    public void OnStopRunning(ref SystemState state)
    {
        
    }

    public void OnDestroy(ref SystemState state)
    {

    }

    #region Updates

    public void Update2D(ref MapData MapInfo, ref SystemState state)
    {
        if (MapInfo.RestartGame)
        {
            MapInfo.RestartGame = false;
            MapInfo.RandomiseSeeds();
            MapInfo.GeneratedBlocks2D.Clear();

            state.EntityManager.DestroyEntity(MapInfo.ResetQuery);

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

            AddBlocksJob AddBlocks = new AddBlocksJob
            {
                WorldIndex = MapInfo.WorldIndex,
                GeneratedBlocks = MapInfo.GeneratedBlocks2D
            };

            state.Dependency = AddBlocks.Schedule(state.Dependency);
            state.Dependency.Complete();

            ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
            UIInfo.UIState = UIStatus.Alive;
            UIInfo.Setup = false;

            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW.Position.xz = FindSafePos2D(ref state);

            ref WorldData WorldInfo = ref SystemAPI.GetSingletonBuffer<WorldData>().ElementAt(MapInfo.WorldIndex);
            RenderSettings.skybox.SetColor("_GroundColor", WorldInfo.BackGround);
            RenderSettings.skybox.SetColor("_SkyTint", WorldInfo.BackGround);

            SystemAPI.GetSingletonRW<PlayerData>().ValueRW.JustTeleported = true;
        }

        bool HasMoved = MovePlayer2D(ref state);

        //GenerateBlock((int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz, ref SystemAPI.GetSingletonRW<MapData>().ValueRW, ref state);

        PlayerData PlayerInfo = SystemAPI.GetSingleton<PlayerData>();
        int2 PlayerPos = (int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz;

        int BlocksToSearch = PlayerInfo.GenerationThickness * PlayerInfo.GenerationThickness;

        NativeList<BlockGenerator2D> BlocksToGenerate = new NativeList<BlockGenerator2D>(BlocksToSearch, Allocator.Persistent);

        var BlocksToGenerateJob = new BlocksToGenerate2DJob()
        {
            Blocks = BlocksToGenerate.AsParallelWriter(),
            GeneratedBlocks = MapInfo.GeneratedBlocks2D,
            GenerationPos = PlayerPos,
            GenerationThickness = PlayerInfo.GenerationThickness
        };

        BlocksToGenerateJob.Schedule(BlocksToSearch, MapInfo.BlockBatchSize).Complete();

        for (int i = 0; i < BlocksToGenerate.Length; i++)
        {
            BlocksToGenerate.ElementAt(i).BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(BlocksToGenerate[i].Pos), ref state);
        }

        for (int i = 0; i < BlocksToGenerate.Length; i++)
        {
            GenerateBlock2D(BlocksToGenerate[i], ref MapInfo, ref state);
        }

        if (MapInfo.OptimisationTechnique == Optimisation.Random && BlocksToGenerate.Length == 0)
        {
            for (int i = 0; i < PlayerInfo.RandomsPerFrame; i++)
            {
                BlockGenerator2D RandBlock = new()
                {
                    Pos = MapInfo.RandStruct.NextInt2(PlayerPos - new int2(PlayerInfo.RandomDistance, PlayerInfo.RandomDistance), PlayerPos + new int2(PlayerInfo.RandomDistance, PlayerInfo.RandomDistance))
                };

                if (!MapInfo.GeneratedBlocks2D.ContainsKey(RandBlock.Pos))
                {
                    RandBlock.BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(RandBlock.Pos), ref state);
                    GenerateBlock2D(RandBlock, ref MapInfo, ref state);
                }
            }
        }
        else if (MapInfo.OptimisationTechnique == Optimisation.Spiral && BlocksToGenerate.Length == 0)
        {
            Debug.Log("I haven't done this yet, sorry!");
        }

        BlocksToGenerate.Dispose();

        if (HasMoved)
        {
            //Debug.Log("The player is going places!");
            MapInfo.FramesSinceLastMovement = 0;
        }
        else
        {
            MapInfo.FramesSinceLastMovement++;
        }
    }

    public void Update3D(ref MapData MapInfo, ref SystemState state)
    {
        if (MapInfo.RestartGame)
        {
            MapInfo.RestartGame = false;
            MapInfo.RandomiseSeeds();
            MapInfo.GeneratedBlocks3D.Clear();

            state.EntityManager.DestroyEntity(MapInfo.ResetQuery);

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

            ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
            UIInfo.UIState = UIStatus.Alive;
            UIInfo.Setup = false;

            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW.Position = FindSafePos3D(ref state);

            ref WorldData WorldInfo = ref SystemAPI.GetSingletonBuffer<WorldData>().ElementAt(MapInfo.WorldIndex);
            RenderSettings.skybox.SetColor("_GroundColor", WorldInfo.BackGround);
            RenderSettings.skybox.SetColor("_SkyTint", WorldInfo.BackGround);

            SystemAPI.GetSingletonRW<PlayerData>().ValueRW.JustTeleported = true;
        }

        MovePlayer3D(ref state);

        PlayerData PlayerInfo = SystemAPI.GetSingleton<PlayerData>();

        int BlocksToSearch = PlayerInfo.GenerationThickness * PlayerInfo.GenerationThickness * PlayerInfo.GenerationThickness;

        NativeList<BlockGenerator3D> BlocksToGenerate = new NativeList<BlockGenerator3D>(BlocksToSearch, Allocator.Persistent);

        var BlocksToGenerateJob = new BlocksToGenerate3DJob()
        {
            Blocks = BlocksToGenerate.AsParallelWriter(),
            GeneratedBlocks = MapInfo.GeneratedBlocks3D,
            GenerationPos = (int3)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position,
            GenerationThickness = PlayerInfo.GenerationThickness,
            GenerationThicknessSquared = PlayerInfo.GenerationThickness*PlayerInfo.GenerationThickness
        };

        BlocksToGenerateJob.Schedule(BlocksToSearch, MapInfo.BlockBatchSize).Complete();

        for (int i = 0; i < BlocksToGenerate.Length; i++)
        {
            BlocksToGenerate.ElementAt(i).BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour3D(BlocksToGenerate[i].Pos), ref state);
        }

        for (int i = 0; i < BlocksToGenerate.Length; i++)
        {
            GenerateBlock3D(BlocksToGenerate[i], ref MapInfo, ref state);
        }

        BlocksToGenerate.Dispose();
    }

    public void UpdateColourDebug(ref MapData MapInfo, ref SystemState state)
    {
        if (MapInfo.RestartGame)
        {
            MapInfo.RestartGame = false;
            MapInfo.RandomiseSeeds();
            MapInfo.GeneratedBlocks2D.Clear();

            state.EntityManager.DestroyEntity(MapInfo.ResetQuery);

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

            ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
            UIInfo.UIState = UIStatus.Alive;
            UIInfo.Setup = false;

            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW.Position.xz = FindSafePos2D(ref state);

            ref WorldData WorldInfo = ref SystemAPI.GetSingletonBuffer<WorldData>().ElementAt(MapInfo.WorldIndex);
            RenderSettings.skybox.SetColor("_GroundColor", WorldInfo.BackGround);
            RenderSettings.skybox.SetColor("_SkyTint", WorldInfo.BackGround);

            SystemAPI.GetSingletonRW<PlayerData>().ValueRW.JustTeleported = true;
        }

        MovePlayerColourDebug(ref state, ref MapInfo);

        //GenerateBlock((int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz, ref SystemAPI.GetSingletonRW<MapData>().ValueRW, ref state);

        PlayerData PlayerInfo = SystemAPI.GetSingleton<PlayerData>();
        int2 PlayerPos = (int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz;

        int BlocksToSearch = PlayerInfo.GenerationThickness * PlayerInfo.GenerationThickness;

        NativeList<BlockGeneratorColourDebug> BlocksToGenerate = new NativeList<BlockGeneratorColourDebug>(BlocksToSearch, Allocator.Persistent);

        var BlocksToGenerateJob = new BlocksToGenerateColourDebugJob()
        {
            Blocks = BlocksToGenerate.AsParallelWriter(),
            GeneratedBlocks = MapInfo.GeneratedBlocks2D,
            GenerationPos = PlayerPos,
            GenerationThickness = PlayerInfo.GenerationThickness
        };

        BlocksToGenerateJob.Schedule(BlocksToSearch, MapInfo.BlockBatchSize).Complete();

        for (int i = 0; i < BlocksToGenerate.Length; i++)
        {
            BlocksToGenerate.ElementAt(i).BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(BlocksToGenerate[i].Pos), ref state);
            BlocksToGenerate.ElementAt(i).TrueColour = MapInfo.GetBiomeColour2D(BlocksToGenerate.ElementAt(i).Pos);
        }

        for (int i = 0; i < BlocksToGenerate.Length; i++)
        {
            GenerateBlockColourDebug(BlocksToGenerate[i], ref MapInfo, ref state);
        }

        if (MapInfo.OptimisationTechnique == Optimisation.Random && BlocksToGenerate.Length == 0)
        {
            for (int i = 0; i < PlayerInfo.RandomsPerFrame; i++)
            {
                BlockGeneratorColourDebug RandBlock = new()
                {
                    Pos = MapInfo.RandStruct.NextInt2(PlayerPos - new int2(PlayerInfo.RandomDistance, PlayerInfo.RandomDistance), PlayerPos + new int2(PlayerInfo.RandomDistance, PlayerInfo.RandomDistance))
                };

                if (!MapInfo.GeneratedBlocks2D.ContainsKey(RandBlock.Pos))
                {
                    RandBlock.BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(RandBlock.Pos), ref state);
                    RandBlock.TrueColour = MapInfo.GetBiomeColour2D(RandBlock.Pos);
                    GenerateBlockColourDebug(RandBlock, ref MapInfo, ref state);
                }
            }
        }

        BlocksToGenerate.Dispose();
    }

    #endregion

    public void SetupMapData(ref SystemState state)
    {
        ref MapDataWithoutBlocks MD = ref SystemAPI.GetSingletonRW<MapDataWithoutBlocks>().ValueRW;
        Entity MapEntity = SystemAPI.GetSingletonEntity<MapDataWithoutBlocks>();

        state.EntityManager.AddComponent<MapData>(MapEntity);
        state.EntityManager.SetComponentData(MapEntity, new MapData
        {
            MaxBlocks = MD.MaxBlocks,
            Seed = MD.Seed,
            MaxSeed = MD.MaxSeed,
            MinBiomeSeed = MD.MinBiomeSeed,
            MaxBiomeSeed = MD.MaxBiomeSeed,
            BiomeSeed = MD.BiomeSeed,
            BiomeNoiseScale = MD.BiomeNoiseScale,
            TerrainNoiseScale = MD.TerrainNoiseScale,
            MaxTeleportBounds = MD.MaxTeleportBounds,
            MinTeleportBounds = MD.MinTeleportBounds,
            RandStruct = MD.RandStruct,
            RestartGame = MD.RestartGame,
            WorldIndex = MD.WorldIndex,
            BlockBatchSize = MD.BlockBatchSize,
            ColourMarker = MD.ColourMarker
        });
    }

    public int2 FindSafePos2D(ref SystemState state)
    {
        var SafePos = false;
        int2 BPos = new();

        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        while (!SafePos)
        {
            BPos = MapInfo.RandStruct.NextInt2(MapInfo.MinTeleportBounds.xy, MapInfo.MaxTeleportBounds.xy);

            BlockGenerator2D BlockGen = new BlockGenerator2D
            {
                Pos = BPos,
                BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour2D(BPos), ref state)
            };

            if (!MapInfo.GeneratedBlocks2D.ContainsKey(BPos))
            {
                GenerateBlock2D(BlockGen, ref MapInfo, ref state);
            }

            if (MapInfo.GeneratedBlocks2D[BPos] == Entity.Null)
            {
                SafePos = true;
            }
            else if (SystemAPI.GetComponent<BlockData>(MapInfo.GeneratedBlocks2D[BPos]).TeleportSafe)
            {
                SafePos = true;
            }
        }

        return BPos;
    }

    public int3 FindSafePos3D(ref SystemState state)
    {
        var SafePos = false;
        int3 BPos = new();

        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        while (!SafePos)
        {
            BPos = MapInfo.RandStruct.NextInt3(MapInfo.MinTeleportBounds, MapInfo.MaxTeleportBounds);

            BlockGenerator3D BlockGen = new BlockGenerator3D
            {
                Pos = BPos,
                BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour3D(BPos), ref state)
            };

            if (!MapInfo.GeneratedBlocks3D.ContainsKey(BPos))
            {
                GenerateBlock3D(BlockGen, ref MapInfo, ref state);
            }

            if (MapInfo.GeneratedBlocks3D[BPos] == Entity.Null)
            {
                SafePos = true;
            }
            else if (SystemAPI.GetComponent<BlockData>(MapInfo.GeneratedBlocks3D[BPos]).TeleportSafe)
            {
                SafePos = true;
            }
        }

        return BPos;
    }

    #region Movement

    public bool MovePlayer2D(ref SystemState state) //dont like how the player and the map maker are in 1 file...
    {
        bool HasMoved = false;

        ref InputData InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

        if (PlayerInfo.VisibleStats.x <= 0 || UIInfo.UIState == UIStatus.MainMenu)
        {
            return false;
        }

        if (InputInfo.Teleport && PlayerInfo.VisibleStats.z > 0)
        {
            InputInfo.Teleport = false;
            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;

            PlayerTransform.Position.xz = FindSafePos2D(ref state);

            PlayerInfo.VisibleStats.z--;
            PlayerInfo.JustTeleported = true;

            return true;
        }

        if (InputInfo.Pressed || (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement)))
        {
            InputInfo.Pressed = false;
            if (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement))
            {
                InputInfo.TimeHeldFor = PlayerInfo.SecondsUntilHoldMovement - PlayerInfo.HeldMovementDelay;
            }

            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;
            float3 NewPos = PlayerTransform.Position;

            if (InputInfo.Movement.x >= PlayerInfo.MinInputDetected)
            {
                NewPos.x += 1;
            }
            else if (InputInfo.Movement.x <= -PlayerInfo.MinInputDetected)
            {
                NewPos.x -= 1;
            }

            if (InputInfo.Movement.y >= PlayerInfo.MinInputDetected)
            {
                NewPos.z += 1;
            }
            else if (InputInfo.Movement.y <= -PlayerInfo.MinInputDetected)
            {
                NewPos.z -= 1;
            }

            if (!math.all(NewPos == PlayerTransform.Position))
            {
                ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
                //ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

                if (MapInfo.GeneratedBlocks2D.TryGetValue((int2)NewPos.xz, out Entity BlockEntity))
                {
                    if (BlockEntity == Entity.Null)
                    {
                        PlayerTransform.Position = NewPos;
                        HasMoved = true;

                        if (PlayerInfo.PlayerSkills.HasFlag(Skills.Exhausted))
                        {
                            PlayerInfo.VisibleStats.y -= 2;
                        }
                        else
                        {
                            PlayerInfo.VisibleStats.y--;
                        }


                        if (PlayerInfo.VisibleStats.y < 0)
                        {
                            PlayerInfo.VisibleStats.x--;
                        }

                        if (PlayerInfo.VisibleStats.x <= 0)
                        {
                            UIInfo.UIState = UIStatus.Dead;
                        }

                        Color BiomeColour = MapInfo.GetBiomeColour2D((int2)NewPos.xz);
                        UIInfo.BiomeColour = BiomeColour;
                        UIInfo.BiomeName = SystemAPI.GetComponent<BiomeData>(GetBiomeEntity(MapInfo.WorldIndex, BiomeColour, ref state)).BiomeName;
                    }
                    else
                    {
                        BlockData BlockInfo = SystemAPI.GetComponent<BlockData>(BlockEntity);
                        if (PlayerInfo.VisibleStats.w >= BlockInfo.StrengthToWalkOn)
                        {
                            if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.SkillToCross) && !(PlayerInfo.PlayerSkills.HasFlag(SystemAPI.GetComponent<SkillToCrossBehaviourData>(BlockEntity).Skill)))
                            {
                                return false;
                            }

                            if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.SkillStats))
                            {
                                SkillStatsBehaviourData SkillStatsInfo = SystemAPI.GetComponent<SkillStatsBehaviourData>(BlockEntity);

                                if (PlayerInfo.PlayerSkills.HasFlag(SkillStatsInfo.Skill))
                                {
                                    PlayerInfo.VisibleStats += SkillStatsInfo.VisibleStatsChange;
                                    PlayerInfo.HiddenStats += SkillStatsInfo.HiddenStatsChange;
                                }
                                else
                                {
                                    PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
                                    PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;
                                }
                            }
                            else
                            {
                                PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
                                PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;
                            }

                            if (BlockInfo.ConsumeOnCollision && (!BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Replace)))
                            {
                                if (BlockInfo.DecorationEntity != Entity.Null)
                                {
                                    state.EntityManager.DestroyEntity(BlockInfo.DecorationEntity);
                                }

                                state.EntityManager.DestroyEntity(BlockEntity);
                                MapInfo.GeneratedBlocks2D[(int2)NewPos.xz] = Entity.Null;
                            }

                            if (BlockInfo.Behaviour != SpecialBehaviour.None)
                            {
                                if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Warp) && MapInfo.DebugStuff != DebugFeatures.NoWarps)
                                {
                                    MapInfo.RestartGame = true;
                                    MapInfo.KeepStats = true;

                                    bool IsDangerous = MapInfo.RandStruct.NextFloat() < (PlayerInfo.ChanceOfDangerousWarp / 100f);
                                    bool Outcome = !IsDangerous;
                                    int WorldIndex = 0;
                                    DynamicBuffer<WorldData> Worlds = SystemAPI.GetSingletonBuffer<WorldData>();

                                    while (Outcome != IsDangerous)
                                    {
                                        WorldIndex = MapInfo.RandStruct.NextInt(0, Worlds.Length);
                                        Outcome = Worlds[WorldIndex].Dangerous;
                                    }

                                    MapInfo.WorldIndex = WorldIndex;
                                }

                                if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Replace))
                                {
                                    Entity NewBlock = state.EntityManager.Instantiate(SystemAPI.GetComponent<ReplaceBehaviourData>(BlockEntity).ReplacementBlock);
                                    BlockData NewBlockInfo = SystemAPI.GetComponent<BlockData>(NewBlock);
                                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(NewBlock, false).ValueRW.Position = new float3(NewPos.x, NewBlockInfo.YLevel, NewPos.z);

                                    if (BlockInfo.DecorationEntity != Entity.Null)
                                    {
                                        state.EntityManager.DestroyEntity(BlockInfo.DecorationEntity);
                                    }
                                    state.EntityManager.DestroyEntity(BlockEntity);
                                    MapInfo.GeneratedBlocks2D[(int2)NewPos.xz] = NewBlock;
                                }
                            }

                            PlayerTransform.Position = NewPos;
                            HasMoved = true;

                            PlayerInfo.VisibleStats.y--;
                            if (PlayerInfo.VisibleStats.y < 0)
                            {
                                PlayerInfo.VisibleStats.x--;
                            }

                            if (PlayerInfo.VisibleStats.x <= 0)
                            {
                                UIInfo.UIState = UIStatus.Dead;
                            }

                            Color BiomeColour = MapInfo.GetBiomeColour2D((int2)NewPos.xz);
                            UIInfo.BiomeColour = BiomeColour;
                            UIInfo.BiomeName = SystemAPI.GetComponent<BiomeData>(GetBiomeEntity(MapInfo.WorldIndex, BiomeColour, ref state)).BiomeName;
                        }
                    }
                }
            }
            //else
            //{
            //    Debug.Log($"PT: {PlayerTransform.Position}\nNT: {NewPos}");
            //}
        }
        return HasMoved;
    }

    public void MovePlayer3D(ref SystemState state)
    {
        ref InputData InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

        if (PlayerInfo.VisibleStats.x <= 0 || UIInfo.UIState == UIStatus.MainMenu)
        {
            return;
        }

        if (InputInfo.Teleport && PlayerInfo.VisibleStats.z > 0)
        {
            InputInfo.Teleport = false;
            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;

            PlayerTransform.Position = FindSafePos3D(ref state);

            PlayerInfo.VisibleStats.z--;
            PlayerInfo.JustTeleported = true;
        }

        if (InputInfo.Pressed || (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement)))
        {
            InputInfo.Pressed = false;
            if (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement))
            {
                InputInfo.TimeHeldFor = PlayerInfo.SecondsUntilHoldMovement - PlayerInfo.HeldMovementDelay;
            }

            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;
            float3 NewPos = PlayerTransform.Position;

            if (InputInfo.Movement.x >= PlayerInfo.MinInputDetected)
            {
                NewPos.x += 1;
            }
            else if (InputInfo.Movement.x <= -PlayerInfo.MinInputDetected)
            {
                NewPos.x -= 1;
            }

            if (InputInfo.YMovement.y >= PlayerInfo.MinInputDetected)
            {
                NewPos.y += 1;
            }
            else if (InputInfo.YMovement.y <= -PlayerInfo.MinInputDetected)
            {
                NewPos.y -= 1;
            }

            if (InputInfo.Movement.y >= PlayerInfo.MinInputDetected)
            {
                NewPos.z += 1;
            }
            else if (InputInfo.Movement.y <= -PlayerInfo.MinInputDetected)
            {
                NewPos.z -= 1;
            }

            if (!math.all(NewPos == PlayerTransform.Position))
            {
                ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
                //ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

                if (MapInfo.GeneratedBlocks3D.TryGetValue((int3)NewPos, out Entity BlockEntity))
                {
                    if (BlockEntity == Entity.Null)
                    {
                        PlayerTransform.Position = NewPos;

                        if (PlayerInfo.PlayerSkills.HasFlag(Skills.Exhausted))
                        {
                            PlayerInfo.VisibleStats.y -= 2;
                        }
                        else
                        {
                            PlayerInfo.VisibleStats.y--;
                        }


                        if (PlayerInfo.VisibleStats.y < 0)
                        {
                            PlayerInfo.VisibleStats.x--;
                        }

                        if (PlayerInfo.VisibleStats.x <= 0)
                        {
                            UIInfo.UIState = UIStatus.Dead;
                        }

                        Color BiomeColour = MapInfo.GetBiomeColour3D((int3)NewPos);
                        UIInfo.BiomeColour = BiomeColour;
                        UIInfo.BiomeName = SystemAPI.GetComponent<BiomeData>(GetBiomeEntity(MapInfo.WorldIndex, BiomeColour, ref state)).BiomeName;
                    }
                    else
                    {
                        BlockData BlockInfo = SystemAPI.GetComponent<BlockData>(BlockEntity);
                        if (PlayerInfo.VisibleStats.w >= BlockInfo.StrengthToWalkOn)
                        {
                            if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.SkillToCross) && !(PlayerInfo.PlayerSkills.HasFlag(SystemAPI.GetComponent<SkillToCrossBehaviourData>(BlockEntity).Skill)))
                            {
                                return;
                            }

                            if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.SkillStats))
                            {
                                SkillStatsBehaviourData SkillStatsInfo = SystemAPI.GetComponent<SkillStatsBehaviourData>(BlockEntity);

                                if (PlayerInfo.PlayerSkills.HasFlag(SkillStatsInfo.Skill))
                                {
                                    PlayerInfo.VisibleStats += SkillStatsInfo.VisibleStatsChange;
                                    PlayerInfo.HiddenStats += SkillStatsInfo.HiddenStatsChange;
                                }
                                else
                                {
                                    PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
                                    PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;
                                }
                            }
                            else
                            {
                                PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
                                PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;
                            }

                            if (BlockInfo.ConsumeOnCollision && (!BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Replace)))
                            {
                                if (BlockInfo.DecorationEntity != Entity.Null)
                                {
                                    state.EntityManager.DestroyEntity(BlockInfo.DecorationEntity);
                                }

                                state.EntityManager.DestroyEntity(BlockEntity);
                                MapInfo.GeneratedBlocks3D[(int3)NewPos] = Entity.Null;
                            }

                            if (BlockInfo.Behaviour != SpecialBehaviour.None)
                            {
                                if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Warp))
                                {
                                    MapInfo.RestartGame = true;
                                    MapInfo.KeepStats = true;

                                    bool IsDangerous = MapInfo.RandStruct.NextFloat() < (PlayerInfo.ChanceOfDangerousWarp / 100f);
                                    bool Outcome = !IsDangerous;
                                    int WorldIndex = 0;
                                    DynamicBuffer<WorldData> Worlds = SystemAPI.GetSingletonBuffer<WorldData>();

                                    while (Outcome != IsDangerous)
                                    {
                                        WorldIndex = MapInfo.RandStruct.NextInt(0, Worlds.Length);
                                        Outcome = Worlds[WorldIndex].Dangerous;
                                    }

                                    MapInfo.WorldIndex = WorldIndex;
                                }

                                if (BlockInfo.Behaviour.HasFlag(SpecialBehaviour.Replace))
                                {
                                    Entity NewBlock = state.EntityManager.Instantiate(SystemAPI.GetComponent<ReplaceBehaviourData>(BlockEntity).ReplacementBlock);
                                    BlockData NewBlockInfo = SystemAPI.GetComponent<BlockData>(NewBlock);
                                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(NewBlock, false).ValueRW.Position = new float3(NewPos.x, NewPos.y, NewPos.z);

                                    //if (BlockInfo.DecorationEntity != Entity.Null)
                                    //{
                                    //    state.EntityManager.DestroyEntity(BlockInfo.DecorationEntity);
                                    //}
                                    state.EntityManager.DestroyEntity(BlockEntity);
                                    MapInfo.GeneratedBlocks3D[(int3)NewPos] = NewBlock;
                                }
                            }

                            PlayerTransform.Position = NewPos;

                            PlayerInfo.VisibleStats.y--;
                            if (PlayerInfo.VisibleStats.y < 0)
                            {
                                PlayerInfo.VisibleStats.x--;
                            }

                            if (PlayerInfo.VisibleStats.x <= 0)
                            {
                                UIInfo.UIState = UIStatus.Dead;
                            }

                            Color BiomeColour = MapInfo.GetBiomeColour3D((int3)NewPos);
                            UIInfo.BiomeColour = BiomeColour;
                            UIInfo.BiomeName = SystemAPI.GetComponent<BiomeData>(GetBiomeEntity(MapInfo.WorldIndex, BiomeColour, ref state)).BiomeName;
                        }
                    }
                }
            }
            //else
            //{
            //    Debug.Log($"PT: {PlayerTransform.Position}\nNT: {NewPos}");
            //}
        }
    }

    public void MovePlayerColourDebug(ref SystemState state, ref MapData MapInfo)
    {
        ref InputData InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

        if (PlayerInfo.VisibleStats.x <= 0 || UIInfo.UIState == UIStatus.MainMenu)
        {
            return;
        }

        if (InputInfo.Teleport && PlayerInfo.VisibleStats.z > 0)
        {
            InputInfo.Teleport = false;
            MapInfo.RestartGame = true;
            MapInfo.KeepStats = true;

            bool IsDangerous = MapInfo.RandStruct.NextFloat() < (PlayerInfo.ChanceOfDangerousWarp / 100f);
            bool Outcome = !IsDangerous;
            int WorldIndex = 0;
            DynamicBuffer<WorldData> Worlds = SystemAPI.GetSingletonBuffer<WorldData>();

            while (Outcome != IsDangerous)
            {
                WorldIndex = MapInfo.RandStruct.NextInt(0, Worlds.Length);
                Outcome = Worlds[WorldIndex].Dangerous;
            }

            MapInfo.WorldIndex = WorldIndex;
        }

        if (InputInfo.Pressed || (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement)))
        {
            InputInfo.Pressed = false;
            if (InputInfo.Held && (InputInfo.TimeHeldFor >= PlayerInfo.SecondsUntilHoldMovement))
            {
                InputInfo.TimeHeldFor = PlayerInfo.SecondsUntilHoldMovement - PlayerInfo.HeldMovementDelay;
            }

            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;
            float3 NewPos = PlayerTransform.Position;

            if (InputInfo.Movement.x >= PlayerInfo.MinInputDetected)
            {
                NewPos.x += 1;
            }
            else if (InputInfo.Movement.x <= -PlayerInfo.MinInputDetected)
            {
                NewPos.x -= 1;
            }

            if (InputInfo.Movement.y >= PlayerInfo.MinInputDetected)
            {
                NewPos.z += 1;
            }
            else if (InputInfo.Movement.y <= -PlayerInfo.MinInputDetected)
            {
                NewPos.z -= 1;
            }

            PlayerTransform.Position = NewPos;
        }
    }

    #endregion

    public void GenerateBlock2D(BlockGenerator2D BlockGenInfo, ref MapData MapInfo, ref SystemState state)
    {
        Entity BlockEntity = Entity.Null;

        //Entity BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour(BlockGenInfo.Pos), ref state);
        BiomeData Biome = SystemAPI.GetComponent<BiomeData>(BlockGenInfo.BiomeEntity);
        float BlockNoise = MapInfo.GetNoise2D(BlockGenInfo.Pos, Biome);
        //Debug.Log(BlockNoise);

        DynamicBuffer<BiomeFeature> BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BlockGenInfo.BiomeEntity);

        bool IsTerrain = false;

        for (int i = 0; i < BiomeFeatures.Length; i++)
        {
            if (BiomeFeatures[i].IsTerrain && ((BlockNoise >= BiomeFeatures[i].MinNoiseValue) && (BlockNoise < BiomeFeatures[i].MaxNoiseValue)))
            {
                IsTerrain = true;
                BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
                SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

                ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
                if (BlockInfo.HasDecorations)
                {
                    var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
                    if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
                    {
                        BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

                        DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
                        float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

                        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
                    }
                }
            }

            //BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BiomeEntity); this should not be required
        }

        if (!IsTerrain)
        {
            for (int i = 0; i < BiomeFeatures.Length; i++)
            {
                if ((!BiomeFeatures[i].IsTerrain) && (MapInfo.RandStruct.NextFloat() < BiomeFeatures[i].PercentChanceToSpawn / 100))
                {
                    BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

                    ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
                    if (BlockInfo.HasDecorations)
                    {
                        var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
                        if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
                        {
                            BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

                            DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
                            float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

                            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
                        }
                    }

                    break;
                }
            }
        }

        MapInfo.GeneratedBlocks2D.Add(BlockGenInfo.Pos, BlockEntity);
    }

    public void GenerateBlock3D(BlockGenerator3D BlockGenInfo, ref MapData MapInfo, ref SystemState state)
    {
        Entity BlockEntity = Entity.Null;

        //Entity BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour(BlockGenInfo.Pos), ref state);
        BiomeData Biome = SystemAPI.GetComponent<BiomeData>(BlockGenInfo.BiomeEntity);
        float BlockNoise = MapInfo.GetNoise3D(BlockGenInfo.Pos, Biome);
        //Debug.Log(BlockNoise);

        DynamicBuffer<BiomeFeature> BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BlockGenInfo.BiomeEntity);

        bool IsTerrain = false;

        for (int i = 0; i < BiomeFeatures.Length; i++)
        {
            if (BiomeFeatures[i].IsTerrain && ((BlockNoise >= BiomeFeatures[i].MinNoiseValue) && (BlockNoise < BiomeFeatures[i].MaxNoiseValue)))
            {
                IsTerrain = true;
                BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
                SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = BlockGenInfo.Pos; //new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

                //ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
                //if (BlockInfo.HasDecorations)
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

            //BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BiomeEntity); this should not be required
        }

        if (!IsTerrain)
        {
            for (int i = 0; i < BiomeFeatures.Length; i++)
            {
                if ((!BiomeFeatures[i].IsTerrain) && (MapInfo.RandStruct.NextFloat() < BiomeFeatures[i].PercentChanceToSpawn / 100))
                {
                    BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = BlockGenInfo.Pos; //new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

                    //ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
                    //if (BlockInfo.HasDecorations)
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

                    break;
                }
            }
        }

        MapInfo.GeneratedBlocks3D.Add(BlockGenInfo.Pos, BlockEntity);
    }

    public void GenerateBlockColourDebug(BlockGeneratorColourDebug BlockGenInfo, ref MapData MapInfo, ref SystemState state)
    {
        Entity EColour = state.EntityManager.Instantiate(MapInfo.ColourMarker);
        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(EColour, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, 4, BlockGenInfo.Pos.y);
        state.EntityManager.AddComponentData(EColour, new URPMaterialPropertyBaseColor { Value = new float4(BlockGenInfo.TrueColour.r, BlockGenInfo.TrueColour.g, BlockGenInfo.TrueColour.b, 1) });

        Entity BlockEntity = Entity.Null;

        //Entity BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour(BlockGenInfo.Pos), ref state);
        BiomeData Biome = SystemAPI.GetComponent<BiomeData>(BlockGenInfo.BiomeEntity);
        float BlockNoise = MapInfo.GetNoise2D(BlockGenInfo.Pos, Biome);
        //Debug.Log(BlockNoise);

        DynamicBuffer<BiomeFeature> BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BlockGenInfo.BiomeEntity);

        bool IsTerrain = false;

        for (int i = 0; i < BiomeFeatures.Length; i++)
        {
            if (BiomeFeatures[i].IsTerrain && ((BlockNoise >= BiomeFeatures[i].MinNoiseValue) && (BlockNoise < BiomeFeatures[i].MaxNoiseValue)))
            {
                IsTerrain = true;
                BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
                SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

                ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
                if (BlockInfo.HasDecorations)
                {
                    var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
                    if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
                    {
                        BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

                        DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
                        float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

                        SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
                    }
                }
            }

            //BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BiomeEntity); this should not be required
        }

        if (!IsTerrain)
        {
            for (int i = 0; i < BiomeFeatures.Length; i++)
            {
                if ((!BiomeFeatures[i].IsTerrain) && (MapInfo.RandStruct.NextFloat() < BiomeFeatures[i].PercentChanceToSpawn / 100))
                {
                    BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x, SystemAPI.GetComponent<BlockData>(BlockEntity).YLevel, BlockGenInfo.Pos.y);

                    ref BlockData BlockInfo = ref SystemAPI.GetComponentLookup<BlockData>().GetRefRW(BlockEntity, false).ValueRW;
                    if (BlockInfo.HasDecorations)
                    {
                        var DecorationBuffer = SystemAPI.GetBuffer<DecorationElement>(BlockEntity);
                        if (MapInfo.RandStruct.NextFloat() < BlockInfo.DecorationChance / 100)
                        {
                            BlockInfo.DecorationEntity = state.EntityManager.Instantiate(DecorationBuffer[MapInfo.RandStruct.NextInt(0, DecorationBuffer.Length)].DecorationEntity);

                            DecorationData DecorationInfo = SystemAPI.GetComponent<DecorationData>(BlockInfo.DecorationEntity);
                            float2 DecorationPos = MapInfo.RandStruct.NextFloat2(DecorationInfo.MinPos, DecorationInfo.MaxPos);

                            SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockInfo.DecorationEntity, false).ValueRW.Position = new float3(BlockGenInfo.Pos.x + DecorationPos.x, DecorationInfo.YLevel, BlockGenInfo.Pos.y + DecorationPos.y);
                        }
                    }

                    break;
                }
            }
        }

        MapInfo.GeneratedBlocks2D.Add(BlockGenInfo.Pos, BlockEntity);
    }

    public Entity GetBiomeEntity(int WorldIndex, Color BlockColour, ref SystemState state)
    {
        NativeReference<Entity> BEntity = new NativeReference<Entity>(Allocator.TempJob);
        BiomeEntityJob BJob = new BiomeEntityJob
        {
            BlockColour = BlockColour,
            WorldIndex = WorldIndex,
            BiomeEntity = BEntity
        };

        state.Dependency = BJob.Schedule(state.Dependency);
        //state.Dependency = BJob.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        Entity BiomeEntity = BJob.BiomeEntity.Value;
        BEntity.Dispose();

        if (BiomeEntity == Entity.Null)
        {
            BiomeEntity = GetDefaultBiomeEntity(WorldIndex, ref state);
        }

        return BiomeEntity;
    }

    public Entity GetDefaultBiomeEntity(int WorldIndex, ref SystemState state)
    {
        NativeReference<Entity> BEntity = new NativeReference<Entity>(Allocator.TempJob);
        DefaultBiomeJob BJob = new DefaultBiomeJob
        {
            WorldIndex = WorldIndex,
            BiomeEntity = BEntity
        };

        state.Dependency = BJob.Schedule(state.Dependency);
        state.Dependency.Complete();

        Entity BiomeEntity = BJob.BiomeEntity.Value;
        BEntity.Dispose();

        if (BiomeEntity == Entity.Null)
        {
            Debug.Log("Every world needs a default biome....");
        }

        return BiomeEntity;
    }

    [BurstCompile]
    public partial struct BiomeEntityJob : IJobEntity
    {
        [ReadOnly]
        public Color BlockColour;

        [ReadOnly]
        public int WorldIndex;

        public NativeReference<Entity> BiomeEntity;

        void Execute(ref BiomeData Biome, Entity entity)
        {
            if (Biome.WorldIndex == WorldIndex && math.distance(((float4)(Vector4)Biome.ColourSpawn).xyz, ((float4)(Vector4)BlockColour).xyz) <= Biome.MaxDistance)
            {
                BiomeEntity.Value = entity;
            }
        }
    }

    [BurstCompile]
    public partial struct DefaultBiomeJob : IJobEntity
    {
        [ReadOnly]
        public int WorldIndex;

        public NativeReference<Entity> BiomeEntity;

        void Execute(ref DefaultBiomeData DefaultBiome, Entity entity)
        {
            if (DefaultBiome.WorldIndex == WorldIndex)
            {
                BiomeEntity.Value = entity;
            }
        }
    }

    [BurstCompile]
    struct BlocksToGenerate2DJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeList<BlockGenerator2D>.ParallelWriter Blocks;
        [ReadOnly]
        public NativeHashMap<int2, Entity> GeneratedBlocks;
        [ReadOnly]
        public int2 GenerationPos;
        [ReadOnly]
        public int GenerationThickness;
        public void Execute(int i)
        {
            int2 Pos = IndexTo2DPos(i,GenerationThickness) + GenerationPos - new int2(GenerationThickness,GenerationThickness)/2;

            if (!GeneratedBlocks.ContainsKey(Pos))
            {
                Blocks.AddNoResize(new BlockGenerator2D { Pos = Pos });
            }
        }

        public static int2 IndexTo2DPos(int Index, int GenerationThickness)
        {
            return new int2(Index % GenerationThickness, Index / GenerationThickness);
        }
    }

    [BurstCompile]
    struct BlocksToGenerate3DJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeList<BlockGenerator3D>.ParallelWriter Blocks;
        [ReadOnly]
        public NativeHashMap<int3, Entity> GeneratedBlocks;
        [ReadOnly]
        public int3 GenerationPos;
        [ReadOnly]
        public int GenerationThickness;
        [ReadOnly]
        public int GenerationThicknessSquared;
        public void Execute(int i)
        {
            int3 Pos = IndexTo3DPos(i, GenerationThickness, GenerationThicknessSquared) + GenerationPos - new int3(GenerationThickness, GenerationThickness, GenerationThickness) / 2;

            if (!GeneratedBlocks.ContainsKey(Pos))
            {
                Blocks.AddNoResize(new BlockGenerator3D { Pos = Pos });
            }
        }

        public static int3 IndexTo3DPos(int Index, int GenerationThickness, int GenerationThicknessSquared)
        {
            return new int3(Index % GenerationThickness, Index / GenerationThickness % GenerationThickness, Index / GenerationThicknessSquared);
        }
    }

    [BurstCompile]
    struct BlocksToGenerateColourDebugJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeList<BlockGeneratorColourDebug>.ParallelWriter Blocks;
        [ReadOnly]
        public NativeHashMap<int2, Entity> GeneratedBlocks;
        [ReadOnly]
        public int2 GenerationPos;
        [ReadOnly]
        public int GenerationThickness;
        public void Execute(int i)
        {
            int2 Pos = IndexTo2DPos(i, GenerationThickness) + GenerationPos - new int2(GenerationThickness, GenerationThickness) / 2;

            if (!GeneratedBlocks.ContainsKey(Pos))
            {
                Blocks.AddNoResize(new BlockGeneratorColourDebug { Pos = Pos });
            }
        }

        public static int2 IndexTo2DPos(int Index, int GenerationThickness)
        {
            return new int2(Index % GenerationThickness, Index / GenerationThickness);
        }
    }

    [BurstCompile]
    public partial struct AddBlocksJob : IJobEntity
    {
        [ReadOnly]
        public int WorldIndex;

        [WriteOnly]
        public NativeHashMap<int2, Entity> GeneratedBlocks;

        void Execute(ref AddToWorldData BlockInfo, ref WorldTransform BlockTransform, Entity entity)
        {
            if (BlockInfo.WorldIndex == WorldIndex)
            {
                GeneratedBlocks.Add((int2)BlockTransform.Position.xz,entity);
            }
        }
    }
}
