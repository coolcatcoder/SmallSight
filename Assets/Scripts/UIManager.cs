using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using TMPro;
using Unity.Collections;
using UnityEngine.UIElements;

public partial class UIManager : SystemBase
{
    protected override void OnStartRunning()
    {
        Entity UIEntity = EntityManager.CreateEntity();
        EntityManager.AddComponent<UIData>(UIEntity);

        var BlankPerkButtons = new NativeArray<PerkButtonElement>(5, Allocator.Temp);
        EntityManager.AddBuffer<PerkButtonElement>(UIEntity).AddRange(BlankPerkButtons);
        BlankPerkButtons.Dispose();

        var BlankCurseButtons = new NativeArray<CurseButtonElement>(5, Allocator.Temp);
        EntityManager.AddBuffer<CurseButtonElement>(UIEntity).AddRange(BlankCurseButtons);
        BlankCurseButtons.Dispose();

        VisualElement root = Object.FindObjectOfType<UIDocument>().rootVisualElement;

        Button GameOverContinue = root.Q<Button>("GameOverContinue");
        GameOverContinue.clicked += () => Continue();

        Button PerksAndCursesContinue = root.Q<Button>("PerksAndCursesContinue");
        PerksAndCursesContinue.clicked += () => Continue();

        root.Q<Button>("P1").clicked += () => SelectPerkButton(1);
        root.Q<Button>("P2").clicked += () => SelectPerkButton(2);
        root.Q<Button>("P3").clicked += () => SelectPerkButton(3);
        root.Q<Button>("P4").clicked += () => SelectPerkButton(4);
        root.Q<Button>("P5").clicked += () => SelectPerkButton(5);

        root.Q<Button>("C1").clicked += () => SelectCurseButton(1);
        root.Q<Button>("C2").clicked += () => SelectCurseButton(2);
        root.Q<Button>("C3").clicked += () => SelectCurseButton(3);
        root.Q<Button>("C4").clicked += () => SelectCurseButton(4);
        root.Q<Button>("C5").clicked += () => SelectCurseButton(5);
    }

    protected override void OnUpdate()
    {
        VisualElement root = Object.FindObjectOfType<UIDocument>().rootVisualElement;
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
        PlayerData PlayerInfo = SystemAPI.GetSingleton<PlayerData>();

        switch (UIInfo.UIState)
        {
            case UIStatus.Alive:
                root.Q<VisualElement>("PerksAndCurses").visible = false;
                root.Q<VisualElement>("GameOver").visible = false;
                root.Q<VisualElement>("Stats").visible = true;
                root.Q<Label>("StatsText").text = $"Health: {PlayerInfo.VisibleStats.x}\nStamina: {PlayerInfo.VisibleStats.y}\nTeleports: {PlayerInfo.VisibleStats.z}\nStrength: {PlayerInfo.VisibleStats.w}";
                root.Q<Label>("BiomeText").text = $"Biome: {UIInfo.BiomeName}";
                root.Q<Label>("BiomeText").style.color = UIInfo.BiomeColour;
                break;

            case UIStatus.Dead:
                root.Q<VisualElement>("Stats").visible = false;
                root.Q<VisualElement>("GameOver").visible = true;
                break;

            case UIStatus.PerksAndCurses:
                root.Q<VisualElement>("GameOver").visible = false;
                root.Q<VisualElement>("PerksAndCurses").visible = true;
                root.Q<Label>("Cost").text = $"Total Cost: {math.clamp(UIInfo.Cost, 0, int.MaxValue)}";

                if (!UIInfo.Setup)
                {
                    UIInfo.Setup = true;

                    //Debug.Log("I should do some setup probably...");

                    var PerkButtons = SystemAPI.GetSingletonBuffer<PerkButtonElement>();
                    var CurseButtons = SystemAPI.GetSingletonBuffer<CurseButtonElement>();
                    var OneTimeIndicesChosen = new NativeList<int>(Allocator.Temp);

                    RandomisePerk(ref PerkButtons.ElementAt(0), OneTimeIndicesChosen);
                    root.Q<Button>("P1").text = $"{PerkButtons[0].Description} Cost: {PerkButtons[0].CostToAdd}";

                    RandomisePerk(ref PerkButtons.ElementAt(1), OneTimeIndicesChosen);
                    root.Q<Button>("P2").text = $"{PerkButtons[1].Description} Cost: {PerkButtons[1].CostToAdd}";

                    RandomisePerk(ref PerkButtons.ElementAt(2), OneTimeIndicesChosen);
                    root.Q<Button>("P3").text = $"{PerkButtons[2].Description} Cost: {PerkButtons[2].CostToAdd}";

                    RandomisePerk(ref PerkButtons.ElementAt(3), OneTimeIndicesChosen);
                    root.Q<Button>("P4").text = $"{PerkButtons[3].Description} Cost: {PerkButtons[3].CostToAdd}";

                    RandomisePerk(ref PerkButtons.ElementAt(4), OneTimeIndicesChosen);
                    root.Q<Button>("P5").text = $"{PerkButtons[4].Description} Cost: {PerkButtons[4].CostToAdd}";

                    OneTimeIndicesChosen.Clear();

                    RandomiseCurse(ref CurseButtons.ElementAt(0), OneTimeIndicesChosen);
                    root.Q<Button>("C1").text = $"{CurseButtons[0].Description} Cost: {CurseButtons[0].CostToRemove}";

                    RandomiseCurse(ref CurseButtons.ElementAt(1), OneTimeIndicesChosen);
                    root.Q<Button>("C2").text = $"{CurseButtons[1].Description} Cost: {CurseButtons[1].CostToRemove}";

                    RandomiseCurse(ref CurseButtons.ElementAt(2), OneTimeIndicesChosen);
                    root.Q<Button>("C3").text = $"{CurseButtons[2].Description} Cost: {CurseButtons[2].CostToRemove}";

                    RandomiseCurse(ref CurseButtons.ElementAt(3), OneTimeIndicesChosen);
                    root.Q<Button>("C4").text = $"{CurseButtons[3].Description} Cost: {CurseButtons[3].CostToRemove}";

                    RandomiseCurse(ref CurseButtons.ElementAt(4), OneTimeIndicesChosen);
                    root.Q<Button>("C5").text = $"{CurseButtons[4].Description} Cost: {CurseButtons[4].CostToRemove}";

                    OneTimeIndicesChosen.Dispose();
                }
                break;
        }
    }

    public void RandomisePerk(ref PerkButtonElement PerkButton, NativeList<int> OneTimeIndicesChosen)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
        var OneTimePerkArray = SystemAPI.GetSingletonBuffer<OneTimePerkElement>();
        var PerkArray = SystemAPI.GetSingletonBuffer<PerkElement>();

        int RandomIndex = MapInfo.RandStruct.NextInt(0, OneTimePerkArray.Length);

        ref OneTimePerkElement RandomOneTimePerk = ref OneTimePerkArray.ElementAt(MapInfo.RandStruct.NextInt(0, OneTimePerkArray.Length));
        if (OneTimePerkArray.ElementAt(RandomIndex).Used || OneTimeIndicesChosen.Contains(RandomIndex))
        {
            RandomIndex = MapInfo.RandStruct.NextInt(0, PerkArray.Length);

            PerkButton.PerkIndex = RandomIndex;
            PerkButton.CostToAdd = PerkArray.ElementAt(RandomIndex).Cost;
            PerkButton.Description = PerkArray.ElementAt(RandomIndex).Description;
        }
        else
        {
            PerkButton.PerkIndex = RandomIndex;
            PerkButton.CostToAdd = OneTimePerkArray.ElementAt(RandomIndex).Cost;
            PerkButton.Description = OneTimePerkArray.ElementAt(RandomIndex).Description;
            OneTimeIndicesChosen.Add(RandomIndex);
        }
    }

    public void RandomiseCurse(ref CurseButtonElement CurseButton, NativeList<int> OneTimeIndicesChosen)
    {
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
        var OneTimeCurseArray = SystemAPI.GetSingletonBuffer<OneTimeCurseElement>();
        var CurseArray = SystemAPI.GetSingletonBuffer<CurseElement>();

        int RandomIndex = MapInfo.RandStruct.NextInt(0, OneTimeCurseArray.Length);

        ref OneTimeCurseElement RandomOneTimeCurse = ref OneTimeCurseArray.ElementAt(MapInfo.RandStruct.NextInt(0, OneTimeCurseArray.Length));
        if (OneTimeCurseArray.ElementAt(RandomIndex).Used || OneTimeIndicesChosen.Contains(RandomIndex))
        {
            RandomIndex = MapInfo.RandStruct.NextInt(0, CurseArray.Length);

            CurseButton.CurseIndex = RandomIndex;
            CurseButton.CostToRemove = CurseArray.ElementAt(RandomIndex).Cost;
            CurseButton.Description = CurseArray.ElementAt(RandomIndex).Description;
        }
        else
        {
            CurseButton.CurseIndex = RandomIndex;
            CurseButton.CostToRemove = OneTimeCurseArray.ElementAt(RandomIndex).Cost;
            CurseButton.Description = OneTimeCurseArray.ElementAt(RandomIndex).Description;
            OneTimeIndicesChosen.Add(RandomIndex);
        }
    }

    public void Continue()
    {
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;

        if (UIInfo.UIState == UIStatus.Dead)
        {
            UIInfo.UIState = UIStatus.PerksAndCurses;
        }
        else if (UIInfo.UIState == UIStatus.PerksAndCurses && UIInfo.Cost <= 0)
        {
            //code to deal with perks and curses here

            VisualElement root = Object.FindObjectOfType<UIDocument>().rootVisualElement;
            var CurseButtons = SystemAPI.GetSingletonBuffer<CurseButtonElement>();
            var PerkButtons = SystemAPI.GetSingletonBuffer<PerkButtonElement>();

            CurseButtons.Clear(); //this part onwards should only happen after the perks and curses have been marked as used, and for the multiuse ones don't forget to just add 1 to number owned!
            PerkButtons.Clear();

            var BlankCurseButtons = new NativeArray<CurseButtonElement>(5, Allocator.Temp);
            var BlankPerkButtons = new NativeArray<PerkButtonElement>(5, Allocator.Temp);

            CurseButtons.AddRange(BlankCurseButtons);
            PerkButtons.AddRange(BlankPerkButtons);

            BlankCurseButtons.Dispose();
            BlankPerkButtons.Dispose();

            root.Q<Button>("P1").style.backgroundColor = Color.grey;
            root.Q<Button>("P1").style.color = Color.black;

            root.Q<Button>("P2").style.backgroundColor = Color.grey;
            root.Q<Button>("P2").style.color = Color.black;

            root.Q<Button>("P3").style.backgroundColor = Color.grey;
            root.Q<Button>("P3").style.color = Color.black;

            root.Q<Button>("P4").style.backgroundColor = Color.grey;
            root.Q<Button>("P4").style.color = Color.black;

            root.Q<Button>("P5").style.backgroundColor = Color.grey;
            root.Q<Button>("P5").style.color = Color.black;

            root.Q<Button>("C1").style.backgroundColor = Color.grey;
            root.Q<Button>("C1").style.color = Color.black;

            root.Q<Button>("C2").style.backgroundColor = Color.grey;
            root.Q<Button>("C2").style.color = Color.black;

            root.Q<Button>("C3").style.backgroundColor = Color.grey;
            root.Q<Button>("C3").style.color = Color.black;

            root.Q<Button>("C4").style.backgroundColor = Color.grey;
            root.Q<Button>("C4").style.color = Color.black;

            root.Q<Button>("C5").style.backgroundColor = Color.grey;
            root.Q<Button>("C5").style.color = Color.black;

            //reset game

            //ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;
            ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

            MapInfo.RestartGame = true;

            ////EntityQuery ResetQuery = new EntityQueryBuilder(Allocator.Temp)
            ////.WithAllRW<DestroyDuringReset>()
            ////.Build(this);
            ////EntityManager.DestroyEntity(ResetQuery);

            ////MapInfo.Chunks.Clear();

            //PlayerInfo.VisibleStats.x = 5;
            //PlayerInfo.VisibleStats.y = 10;

            //UIInfo.UIState = UIStatus.Alive;
            //UIInfo.Setup = false;
            //UIInfo.Cost = 1;

            //MapInfo.RandomiseSeeds();
        }
    }

    public void SelectPerkButton(int ButtonNum)
    {
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
        DynamicBuffer<PerkButtonElement> PerkButtons = SystemAPI.GetSingletonBuffer<PerkButtonElement>();
        PerkButtons.ElementAt(ButtonNum - 1).Selected = !PerkButtons[ButtonNum - 1].Selected;

        VisualElement root = Object.FindObjectOfType<UIDocument>().rootVisualElement;

        if (PerkButtons[ButtonNum - 1].Selected)
        {
            root.Q<Button>($"P{ButtonNum}").style.backgroundColor = Color.cyan;
            root.Q<Button>($"P{ButtonNum}").style.color = Color.magenta;

            UIInfo.Cost += PerkButtons[ButtonNum - 1].CostToAdd;
        }
        else
        {
            root.Q<Button>($"P{ButtonNum}").style.backgroundColor = Color.grey;
            root.Q<Button>($"P{ButtonNum}").style.color = Color.black;

            UIInfo.Cost -= PerkButtons[ButtonNum - 1].CostToAdd;
        }
    }

    public void SelectCurseButton(int ButtonNum)
    {
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
        DynamicBuffer<CurseButtonElement> CurseButtons = SystemAPI.GetSingletonBuffer<CurseButtonElement>();
        CurseButtons.ElementAt(ButtonNum - 1).Selected = !CurseButtons[ButtonNum - 1].Selected;

        VisualElement root = Object.FindObjectOfType<UIDocument>().rootVisualElement;

        if (CurseButtons[ButtonNum - 1].Selected)
        {
            root.Q<Button>($"C{ButtonNum}").style.backgroundColor = Color.red;
            root.Q<Button>($"C{ButtonNum}").style.color = Color.cyan;

            UIInfo.Cost -= CurseButtons[ButtonNum - 1].CostToRemove;
        }
        else
        {
            root.Q<Button>($"C{ButtonNum}").style.backgroundColor = Color.grey;
            root.Q<Button>($"C{ButtonNum}").style.color = Color.black;

            UIInfo.Cost += CurseButtons[ButtonNum - 1].CostToRemove;
        }
    }
}

public struct UIData : IComponentData
{
    public int Cost;
    public UIStatus UIState;
    public bool Setup;
    public FixedString128Bytes BiomeName;
    public Color BiomeColour;
}

public struct PerkButtonElement : IBufferElementData
{
    public int PerkIndex;
    public bool OneTimeUsePerk;
    public bool Selected;
    public int CostToAdd;
    public FixedString128Bytes Description;
}

public struct CurseButtonElement : IBufferElementData
{
    public int CurseIndex;
    public bool OneTimeUseCurse;
    public bool Selected;
    public int CostToRemove;
    public FixedString128Bytes Description;
}

public enum UIStatus
{
    Alive = 0,
    Dead = 1,
    PerksAndCurses = 2,
}

/*
 * UI States:
 * 0 : alive, in game
 * 1 : dead, but has not continued
 * 2 : perk and curse screen
 */