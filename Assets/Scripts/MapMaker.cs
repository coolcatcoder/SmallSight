using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

    public int2 MaxTeleportBounds;
    public int2 MinTeleportBounds;

    public int WorldIndex = 0;
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
            WorldIndex = authoring.WorldIndex
        });
    }
}

//[ChunkSerializable]
public struct MapData : IComponentData
{
    public NativeHashMap<int2, Entity> GeneratedBlocks;
    public int MaxBlocks;

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

    public int WorldIndex;
}

[ChunkSerializable]
public struct MapDataWithoutBlocks : IComponentData
{
    //public NativeHashMap<int2, Entity> GeneratedBlocks;
    public int MaxBlocks;

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

    public int WorldIndex;
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
    public static Color GetBiomeColour(this MapData MapInfo, int2 Pos)
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
    public static float GetNoise(this MapData MapInfo, int2 Pos, BiomeData Biome) //what is this?
    {
        float2 SeededPos = Pos;
        SeededPos.x += MapInfo.Seed;
        return noise.snoise(SeededPos * (MapInfo.TerrainNoiseScale + Biome.ExtraTerrainNoiseScale));
    }
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

        MapInfo.GeneratedBlocks = new NativeHashMap<int2, Entity>(MapInfo.MaxBlocks, Allocator.Persistent);

        MapInfo.RandomiseSeeds();

        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        PlayerInfo.VisibleStats = PlayerInfo.DefaultVisibleStats;
        PlayerInfo.HiddenStats = PlayerInfo.DefaultHiddenStats;
    }

    public void OnUpdate(ref SystemState state)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        if (MapInfo.RestartGame)
        {
            MapInfo.RestartGame = false;
            MapInfo.RandomiseSeeds();
            MapInfo.GeneratedBlocks.Clear();

            EntityQuery ResetQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<DestroyOnRestartData>()
                .Build(ref state); //save this in the MapData during Oncreate()

            state.EntityManager.DestroyEntity(ResetQuery);

            ref PlayerData PlayerInfoRW = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
            PlayerInfoRW.VisibleStats = PlayerInfoRW.DefaultVisibleStats;
            PlayerInfoRW.HiddenStats = PlayerInfoRW.DefaultHiddenStats;

            ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
            UIInfo.UIState = UIStatus.Alive;
        }

        MovePlayer(ref state);

        //GenerateBlock((int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz, ref SystemAPI.GetSingletonRW<MapData>().ValueRW, ref state);

        PlayerData PlayerInfo = SystemAPI.GetSingleton<PlayerData>();

        for (int x = -PlayerInfo.GenerationThickness; x <= PlayerInfo.GenerationThickness; x++)
        {
            for (int z = -PlayerInfo.GenerationThickness; z <= PlayerInfo.GenerationThickness; z++)
            {
                int2 WorldPos = (int2)SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<PlayerData>()).Position.xz;
                WorldPos.x += x;
                WorldPos.y += z;
                GenerateBlock(WorldPos, ref SystemAPI.GetSingletonRW<MapData>().ValueRW, ref state);
            }
        }
    }

    public void OnStopRunning(ref SystemState state)
    {

    }

    public void OnDestroy(ref SystemState state)
    {

    }

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
            WorldIndex = MD.WorldIndex
        });
    }

    public int2 FindSafePos(ref SystemState state)
    {
        var SafePos = false;
        int2 Pos = new();

        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        while (!SafePos)
        {
            Pos = MapInfo.RandStruct.NextInt2(MapInfo.MinTeleportBounds, MapInfo.MaxTeleportBounds);
            GenerateBlock(Pos, ref MapInfo, ref state);
            if (MapInfo.GeneratedBlocks[Pos] == Entity.Null)
            {
                SafePos = true;
            }
        }

        return Pos;
    }

    public void MovePlayer(ref SystemState state) //dont like how the player and the map maker are in 1 file...
    {
        ref InputData InputInfo = ref SystemAPI.GetSingletonRW<InputData>().ValueRW;
        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;

        if (PlayerInfo.VisibleStats.x <= 0)
        {
            return;
        }

        if (InputInfo.Teleport && PlayerInfo.VisibleStats.z > 0)
        {
            InputInfo.Teleport = false;
            ref LocalTransform PlayerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<PlayerData>(), false).ValueRW;

            PlayerTransform.Position.xz = FindSafePos(ref state);

            PlayerInfo.VisibleStats.z--;
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

            if (InputInfo.Movement.x >= 1)
            {
                NewPos.x += 1;
            }
            else if (InputInfo.Movement.x <= -1)
            {
                NewPos.x -= 1;
            }

            if (InputInfo.Movement.y >= 1)
            {
                NewPos.z += 1;
            }
            else if (InputInfo.Movement.y <= -1)
            {
                NewPos.z -= 1;
            }

            if (!math.all(NewPos == PlayerTransform.Position))
            {
                ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
                ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

                if (MapInfo.GeneratedBlocks.TryGetValue((int2)NewPos.xz, out Entity BlockEntity))
                {
                    if (BlockEntity == Entity.Null)
                    {
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

                        Color BiomeColour = MapInfo.GetBiomeColour((int2)NewPos.xz);
                        UIInfo.BiomeColour = BiomeColour;
                        UIInfo.BiomeName = SystemAPI.GetComponent<BiomeData>(GetBiomeEntity(MapInfo.WorldIndex, BiomeColour, ref state)).BiomeName;
                    }
                    else
                    {
                        BlockData BlockInfo = SystemAPI.GetComponent<BlockData>(BlockEntity);
                        if (PlayerInfo.VisibleStats.w >= BlockInfo.StrengthToWalkOn)
                        {
                            PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
                            PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;

                            if (BlockInfo.ConsumeOnCollision && (BlockInfo.Behaviour != SpecialBehaviour.Replace))
                            {
                                state.EntityManager.DestroyEntity(BlockEntity);
                                MapInfo.GeneratedBlocks[(int2)NewPos.xz] = Entity.Null;
                            }

                            switch (BlockInfo.Behaviour)
                            {
                                case SpecialBehaviour.Warp:
                                    MapInfo.RestartGame = true;

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

                                    break;

                                case SpecialBehaviour.Replace:
                                    Entity NewBlock = state.EntityManager.Instantiate(SystemAPI.GetComponent<ReplaceBehaviourData>(BlockEntity).ReplacementBlock);
                                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(NewBlock, false).ValueRW.Position = new float3(NewPos.x, -1, NewPos.z);
                                    state.EntityManager.DestroyEntity(BlockEntity);
                                    MapInfo.GeneratedBlocks[(int2)NewPos.xz] = NewBlock;
                                    break;
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

                            Color BiomeColour = MapInfo.GetBiomeColour((int2)NewPos.xz);
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

    public void GenerateBlock(int2 Pos, ref MapData MapInfo, ref SystemState state)
    {
        if (MapInfo.GeneratedBlocks.ContainsKey(Pos))
        {
            return;
        }

        Entity BlockEntity = Entity.Null;

        Entity BiomeEntity = GetBiomeEntity(MapInfo.WorldIndex, MapInfo.GetBiomeColour(Pos), ref state);
        BiomeData Biome = SystemAPI.GetComponent<BiomeData>(BiomeEntity);
        float BlockNoise = MapInfo.GetNoise(Pos, Biome);
        //Debug.Log(BlockNoise);

        DynamicBuffer<BiomeFeature> BiomeFeatures = SystemAPI.GetBuffer<BiomeFeature>(BiomeEntity);

        bool IsTerrain = false;

        for (int i = 0; i < BiomeFeatures.Length; i++)
        {
            if (BiomeFeatures[i].IsTerrain && ((BlockNoise >= BiomeFeatures[i].MinNoiseValue) && (BlockNoise < BiomeFeatures[i].MaxNoiseValue)))
            {
                IsTerrain = true;
                BlockEntity = state.EntityManager.Instantiate(BiomeFeatures[i].FeaturePrefab);
                SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(Pos.x, -1, Pos.y);
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
                    SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(BlockEntity, false).ValueRW.Position = new float3(Pos.x, -1, Pos.y);

                    break;
                }
            }
        }

        MapInfo.GeneratedBlocks.Add(Pos, BlockEntity);
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
        state.Dependency.Complete();

        Entity BiomeEntity = BJob.BiomeEntity.Value;
        BEntity.Dispose();

        if (BiomeEntity == Entity.Null)
        {
            BiomeEntity = SystemAPI.GetSingletonEntity<DefaultBiomeData>();
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
}
