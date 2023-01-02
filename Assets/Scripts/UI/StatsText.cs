using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using TMPro;
using Unity.Collections;

public class StatsText : MonoBehaviour
{
    public TextMeshProUGUI StatisticsText;

    Entity PlayerEntity;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerEntity == Entity.Null)
        {
            EntityQuery PlayerQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<PlayerData>()
            .Build(World.DefaultGameObjectInjectionWorld.EntityManager);

            PlayerEntity = PlayerQuery.ToEntityArray(Allocator.Temp)[0];
        }

        PlayerData Stats = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PlayerData>(PlayerEntity);

        if (Stats.VisibleStats.x <= 0)
        {
            StatisticsText.text = "Game Over!";
        }
        else
        {
            StatisticsText.text = string.Format("Health: {0}\nStamina: {1}\nTeleports: {2}\nStrength: {3}\nCurrent Biome: WIP", Stats.VisibleStats.x, Stats.VisibleStats.y, Stats.VisibleStats.z, Stats.VisibleStats.w); //MapGenerator.GameMap.Biomes[MapGenerator.GameMap.CalculateBiome(transform.position)].BiomeName);
        }
    }
}
