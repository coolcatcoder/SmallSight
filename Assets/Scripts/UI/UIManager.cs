using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using TMPro;
using Unity.Collections;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI StatisticsText;

    public GameObject PerksAndCurses;

    public GameObject ContinueButton;

    int Cost = 0;

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

        //PlayerData PlayerInfo = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PlayerData>(PlayerEntity);
        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;

        switch (PlayerInfo.UIState)
        {
            case 0:
                StatisticsText.text = string.Format("Health: {0}\nStamina: {1}\nTeleports: {2}\nStrength: {3}\nCurrent Biome: WIP", PlayerInfo.VisibleStats.x, PlayerInfo.VisibleStats.y, PlayerInfo.VisibleStats.z, PlayerInfo.VisibleStats.w);
                break;

            case 1:
                StatisticsText.text = "Game Over!";
                ContinueButton.SetActive(true);
                break;

            case 2:
                StatisticsText.gameObject.SetActive(false);
                PerksAndCurses.SetActive(true);
                break;

            case 3:
                StatisticsText.gameObject.SetActive(true);
                PerksAndCurses.SetActive(false);
                PlayerInfo.UIState = 0;
                break;
        }

        //if (Stats.VisibleStats.x <= 0)
        //{
        //    StatisticsText.text = "Game Over!";
        //}
        //else
        //{
        //    StatisticsText.text = string.Format("Health: {0}\nStamina: {1}\nTeleports: {2}\nStrength: {3}\nCurrent Biome: WIP", Stats.VisibleStats.x, Stats.VisibleStats.y, Stats.VisibleStats.z, Stats.VisibleStats.w); //MapGenerator.GameMap.Biomes[MapGenerator.GameMap.CalculateBiome(transform.position)].BiomeName);
        //}
    }

    public void Continue()
    {
        //PlayerData PlayerInfo = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PlayerData>(PlayerEntity);
        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;

        if (PlayerInfo.UIState==1)
        {
            PlayerInfo.UIState = 2;
        }
        else if (PlayerInfo.UIState == 2)
        {
            if (Cost <= 0)
            {
                //do something
            }
        }
    }
}
