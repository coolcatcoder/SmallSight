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
}

public class MapMakerBaker : Baker<MapMaker>
{
    public override void Bake(MapMaker authoring)
    {
        AddComponent(new MapData
        {
            GeneratedBlocks = new NativeHashMap<Unity.Mathematics.int2, Entity>(authoring.MaxBlocks, Allocator.Persistent),
            MaxSeed = authoring.MaxSeed,
            MinBiomeSeed = authoring.MinBiomeSeed,
            MaxBiomeSeed = authoring.MaxBiomeSeed,
            BiomeNoiseScale = authoring.BiomeNoiseScale,
            TerrainNoiseScale = authoring.TerrainNoiseScale,
            MaxTeleportBounds = authoring.MaxTeleportBounds,
            MinTeleportBounds = authoring.MinTeleportBounds,
        });
    }
}

public struct MapData : IComponentData
{
    public NativeHashMap<int2, Entity> GeneratedBlocks;

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
    public static float GetNoise(this MapData MapInfo, int2 Pos, BiomeData Biome)
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
    }

    public void OnStartRunning(ref SystemState state)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
        MapInfo.RandomiseSeeds();

        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
        PlayerInfo.VisibleStats = PlayerInfo.DefaultVisibleStats;
        PlayerInfo.HiddenStats = PlayerInfo.DefaultHiddenStats;
    }

    public void OnUpdate(ref SystemState state) //set the UIData to have the biome name in it, do it from here, good luck, im tired, night....
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
                            SystemAPI.GetSingletonRW<UIData>().ValueRW.UIState = UIStatus.Dead;
                        }
                    }
                    else
                    {
                        BlockData BlockInfo = SystemAPI.GetComponent<BlockData>(BlockEntity);
                        if (PlayerInfo.VisibleStats.w >= BlockInfo.StrengthToWalkOn)
                        {
                            PlayerInfo.VisibleStats += BlockInfo.VisibleStatsChange;
                            PlayerInfo.HiddenStats += BlockInfo.HiddenStatsChange;

                            state.EntityManager.DestroyEntity(BlockEntity);
                            MapInfo.GeneratedBlocks[(int2)NewPos.xz] = Entity.Null;

                            PlayerTransform.Position = NewPos;

                            PlayerInfo.VisibleStats.y--;
                            if (PlayerInfo.VisibleStats.y < 0)
                            {
                                PlayerInfo.VisibleStats.x--;
                            }

                            if (PlayerInfo.VisibleStats.x <= 0)
                            {
                                SystemAPI.GetSingletonRW<UIData>().ValueRW.UIState = UIStatus.Dead;
                            }
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

        Entity BiomeEntity = GetBiomeEntity(MapInfo.GetBiomeColour(Pos), state);
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

    public Entity GetBiomeEntity(Color BlockColour, SystemState state)
    {
        NativeReference<Entity> BEntity = new NativeReference<Entity>(Allocator.TempJob);
        BiomeEntityJob BJob = new BiomeEntityJob
        {
            BlockColour = BlockColour,
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

    public partial struct BiomeEntityJob : IJobEntity
    {
        [ReadOnly]
        public Color BlockColour;

        public NativeReference<Entity> BiomeEntity;

        void Execute(ref BiomeData Biome, Entity entity)
        {
            if (math.distance(((float4)(Vector4)Biome.ColourSpawn).xyz, ((float4)(Vector4)BlockColour).xyz) <= Biome.MaxDistance/100)
            {
                BiomeEntity.Value = entity;
            }
        }
    }
}
