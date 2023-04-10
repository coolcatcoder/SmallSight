using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using TMPro;
using Unity.Collections;
using UnityEngine.UIElements;
using Unity.Burst;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(Map2DStart))]
public partial class UIManager : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PlayerData>();
        RequireForUpdate<MapData>();
        RequireForUpdate<MouseBlockMarkerData>();
    }

    protected override void OnStartRunning()
    {
        Entity UIEntity = EntityManager.CreateEntity();
        EntityManager.AddComponent<UIData>(UIEntity);

        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
        UIInfo.UIState = UIStatus.MainMenu;

        //ActiveMenus = new NativeList<Menu>(20, Allocator.Persistent);

        var BlankPerkButtons = new NativeArray<PerkButtonElement>(5, Allocator.Temp);
        EntityManager.AddBuffer<PerkButtonElement>(UIEntity).AddRange(BlankPerkButtons);
        BlankPerkButtons.Dispose();

        var BlankCurseButtons = new NativeArray<CurseButtonElement>(5, Allocator.Temp);
        EntityManager.AddBuffer<CurseButtonElement>(UIEntity).AddRange(BlankCurseButtons);
        BlankCurseButtons.Dispose();

        VisualElement root = Object.FindObjectOfType<UIDocument>().rootVisualElement;

        Button GameOverContinue = root.Q<Button>("GameOverContinue");
        GameOverContinue.clicked += () => Continue();

        root.Q<Button>("GoToMainMenu").clicked += () => OpenMenu(UIStatus.MainMenuGameOver);

        root.Q<Button>("StartGame").clicked += () => StartGame();

        root.Q<Button>("MainMenuContinue").clicked += () => OpenMenu(UIStatus.Dead); //MainMenuToPerksAndCurses();

        root.Q<Button>("StatsAlmanac").clicked += () => OpenMenu(UIStatus.Almanac);
        root.Q<Button>("GameOverAlmanac").clicked += () => OpenMenu(UIStatus.AlmanacDead);

        root.Q<Button>("SettingsBack").clicked += () => OpenMenu(UIStatus.MainMenu);
        root.Q<Button>("ChangeSettings").clicked += () => OpenMenu(UIStatus.Settings);

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

        root.Q<EnumField>("OptimisationStrategy").Init(Optimisation.None);
        root.Q<EnumField>("DebugFeatures").Init(DebugFeatures.None);
        root.Q<EnumField>("WorldDropDown").Init(AlmanacWorld.World0);

        root.Q<Button>("NextPage").clicked += () => TurnPage(1, root);
        root.Q<Button>("PreviousPage").clicked += () => TurnPage(-1, root);

    }

    protected override void OnUpdate()
    {
        VisualElement root = Object.FindObjectOfType<UIDocument>().rootVisualElement;
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
        ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;

        switch (UIInfo.UIState)
        {
            case UIStatus.MainMenu:
                if (!UIInfo.Setup)
                {
                    MainMenuSetup(root, ref PlayerInfo);
                    UIInfo.Setup = true;
                }
                //MainMenuUpdate(root, ref PlayerInfo);
                break;

            case UIStatus.Alive:
                if (!UIInfo.Setup)
                {
                    AliveMenuSetup(root);
                    UIInfo.Setup = true;
                }
                AliveMenuUpdate(root, ref PlayerInfo, ref UIInfo);
                break;

            case UIStatus.Dead:
                if (!UIInfo.Setup)
                {
                    DeadMenuSetup(root);
                    UIInfo.Setup = true;
                }
                //DeadMenuUpdate(root, ref PlayerInfo);
                break;

            case UIStatus.PerksAndCurses:
                if (!UIInfo.Setup)
                {
                    PerksAndCursesMenuSetup(root, ref PlayerInfo, ref UIInfo);
                    UIInfo.Setup = true;
                }
                PerksAndCursesMenuUpdate(root, ref UIInfo);
                
                //root.Q<Label>("Cost").text = $"Total Cost: {math.clamp(UIInfo.Cost, 0, int.MaxValue)}"; showing negatives should be fine, the user will understand
                break;

            case UIStatus.MainMenuGameOver:
                if (!UIInfo.Setup)
                {
                    DeadMainMenuSetup(root);
                    UIInfo.Setup = true;
                }
                //DeadMainMenuUpdate(root, ref PlayerInfo);
                break;

            case UIStatus.Almanac:
                if (!UIInfo.Setup)
                {
                    AlmanacMenuSetup(root, ref UIInfo);
                    UIInfo.Setup = true;
                }
                AlmanacMenuUpdate(root, ref PlayerInfo, ref UIInfo);
                break;

            case UIStatus.AlmanacDead:
                if (!UIInfo.Setup)
                {
                    DeadAlmanacMenuSetup(root, ref UIInfo);
                    UIInfo.Setup = true;
                }
                AlmanacMenuUpdate(root, ref PlayerInfo, ref UIInfo); // dead and alive are basically the same
                break;

            case UIStatus.Settings:
                if (!UIInfo.Setup)
                {
                    SettingsMenuSetup(root);
                    UIInfo.Setup = true;
                }
                SettingsMenuUpdate(root, ref PlayerInfo);
                break;
        }
    }

    protected override void OnDestroy()
    {
        //ActiveMenus.Dispose();
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
            PerkButton.VarToChange = PerkArray.ElementAt(RandomIndex).VarToChange;
            PerkButton.AmountToChange = PerkArray.ElementAt(RandomIndex).AmountToChange;
            PerkButton.SkillToSet = PerkArray.ElementAt(RandomIndex).SkillToSet;
        }
        else
        {
            PerkButton.PerkIndex = RandomIndex;
            PerkButton.CostToAdd = OneTimePerkArray.ElementAt(RandomIndex).Cost;
            PerkButton.Description = OneTimePerkArray.ElementAt(RandomIndex).Description;
            PerkButton.VarToChange = OneTimePerkArray.ElementAt(RandomIndex).VarToChange;
            PerkButton.AmountToChange = OneTimePerkArray.ElementAt(RandomIndex).AmountToChange;
            PerkButton.SkillToSet = OneTimePerkArray.ElementAt(RandomIndex).SkillToSet;
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
            CurseButton.VarToChange = CurseArray.ElementAt(RandomIndex).VarToChange;
            CurseButton.AmountToChange = CurseArray.ElementAt(RandomIndex).AmountToChange;
            CurseButton.SkillToSet = CurseArray.ElementAt(RandomIndex).SkillToSet;
        }
        else
        {
            CurseButton.CurseIndex = RandomIndex;
            CurseButton.CostToRemove = OneTimeCurseArray.ElementAt(RandomIndex).Cost;
            CurseButton.Description = OneTimeCurseArray.ElementAt(RandomIndex).Description;
            CurseButton.VarToChange = OneTimeCurseArray.ElementAt(RandomIndex).VarToChange;
            CurseButton.AmountToChange = OneTimeCurseArray.ElementAt(RandomIndex).AmountToChange;
            CurseButton.SkillToSet = OneTimeCurseArray.ElementAt(RandomIndex).SkillToSet;
            OneTimeIndicesChosen.Add(RandomIndex);
        }
    }

    #region Menus

    public void OpenMenu(UIStatus MenuToOpen)
    {
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
        UIInfo.UIState = MenuToOpen;
    }

    #region EmptyMenu

    public void EmptyMenuSetup(VisualElement root)
    {

    }

    public void EmptyMenuUpdate(VisualElement root, ref PlayerData PlayerInfo, ref UIData UIInfo)
    {

    }

    public void EmptyMenuOpen()
    {

    }

    #endregion

    #region MainMenu

    public void MainMenuSetup(VisualElement root, ref PlayerData PlayerInfo)
    {
        root.Q<VisualElement>("GameOver").style.display = DisplayStyle.None;
        root.Q<VisualElement>("Settings").style.display = DisplayStyle.None;
        root.Q<VisualElement>("MainMenu").style.display = DisplayStyle.Flex;
        SettingsMenuUpdate(root, ref PlayerInfo); // lazy...
    }

    public void MainMenuUpdate(VisualElement root, ref PlayerData PlayerInfo)
    {

    }

    #endregion

    #region DeadMainMenu

    public void DeadMainMenuSetup(VisualElement root)
    {
        root.Q<VisualElement>("GameOver").style.display = DisplayStyle.None;
        root.Q<VisualElement>("Settings").style.display = DisplayStyle.None;
        root.Q<VisualElement>("MainMenu").style.display = DisplayStyle.Flex;
        root.Q<Button>("StartGame").style.display = DisplayStyle.None;
        //root.Q<Button>("MainMenuAlmanac").style.display = DisplayStyle.Flex; don't have the almanac on the main menu, rather make the continue button lead to the game over screen again, which then has the option to go to the almanac should you want to
        root.Q<Button>("MainMenuContinue").style.display = DisplayStyle.Flex;

        root.Q<Button>("SettingsBack").clicked += () => OpenMenu(UIStatus.MainMenuGameOver);
    }

    public void DeadMainMenuUpdate(VisualElement root, ref PlayerData PlayerInfo)
    {

    }

    #endregion

    #region SettingsMenu

    public void SettingsMenuSetup(VisualElement root)
    {
        root.Q<VisualElement>("Settings").style.display = DisplayStyle.Flex;
        root.Q<VisualElement>("MainMenu").style.display = DisplayStyle.None;
    }

    public void SettingsMenuUpdate(VisualElement root, ref PlayerData PlayerInfo)
    {
        PlayerInfo.SecondsUntilHoldMovement = root.Q<Slider>("HoldDelay").value;
        PlayerInfo.HeldMovementDelay = root.Q<Slider>("DelayBetween").value;
        PlayerInfo.MinInputDetected = root.Q<Slider>("MinInputDetected").value;
        PlayerInfo.GenerationThickness = root.Q<SliderInt>("RenderDistance").value;

        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        MapInfo.OptimisationTechnique = (Optimisation)root.Q<EnumField>("OptimisationStrategy").value;
        MapInfo.DebugStuff = (DebugFeatures)root.Q<EnumField>("DebugFeatures").value;

        root.Q<SliderInt>("RandomDistance").style.display = DisplayStyle.None;
        root.Q<SliderInt>("RandomsPerFrame").style.display = DisplayStyle.None;

        root.Q<SliderInt>("BlocksPerFrame").style.display = DisplayStyle.None;
        root.Q<SliderInt>("MaxFrames").style.display = DisplayStyle.None;

        switch (MapInfo.OptimisationTechnique)
        {
            case Optimisation.None:
                break;

            case Optimisation.Random:
                root.Q<SliderInt>("RandomDistance").style.display = DisplayStyle.Flex;
                root.Q<SliderInt>("RandomsPerFrame").style.display = DisplayStyle.Flex;

                PlayerInfo.RandomDistance = root.Q<SliderInt>("RandomDistance").value;
                PlayerInfo.RandomsPerFrame = root.Q<SliderInt>("RandomsPerFrame").value;
                break;

            case Optimisation.Spiral:
                root.Q<SliderInt>("BlocksPerFrame").style.display = DisplayStyle.Flex;
                root.Q<SliderInt>("MaxFrames").style.display = DisplayStyle.Flex;

                MapInfo.MaxBlocksToSpiral = root.Q<SliderInt>("MaxFrames").value;
                MapInfo.BlocksToSpiral = root.Q<SliderInt>("BlocksPerFrame").value;
                break;
        }
        
    }

    #endregion

    #region AliveMenu

    public void AliveMenuSetup(VisualElement root)
    {
        root.Q<VisualElement>("PerksAndCurses").style.display = DisplayStyle.None;
        root.Q<VisualElement>("MainMenu").style.display = DisplayStyle.None;
        root.Q<VisualElement>("Almanac").style.display = DisplayStyle.None;
        root.Q<VisualElement>("Stats").style.display = DisplayStyle.Flex;
    }

    public void AliveMenuUpdate(VisualElement root, ref PlayerData PlayerInfo, ref UIData UIInfo)
    {
        root.Q<Label>("StatsText").text = $"Health: {PlayerInfo.VisibleStats.x}\nStamina: {PlayerInfo.VisibleStats.y}\nTeleports: {PlayerInfo.VisibleStats.z}\nStrength: {PlayerInfo.VisibleStats.w}\nKarma: {PlayerInfo.HiddenStats.y}";
        root.Q<Label>("BiomeText").text = $"Biome: {UIInfo.BiomeName}";
        root.Q<Label>("BiomeText").style.color = UIInfo.BiomeColour;

        ref LocalTransform MouseMarkerTransform = ref SystemAPI.GetComponentLookup<LocalTransform>().GetRefRW(SystemAPI.GetSingletonEntity<MouseBlockMarkerData>(), false).ValueRW;

        if (!SystemAPI.GetSingleton<MapData>().Is3D)
        {
            MouseMarkerTransform.Position = math.floor((float3)Camera.main.ScreenToWorldPoint(Input.mousePosition) + new float3(0.5f, 0, 0.5f));
            MouseMarkerTransform.Position.y = 4;
            //MouseMarkerTransform.Position.x += 1;
        }
    }

    #endregion

    #region DeadMenu

    public void DeadMenuSetup(VisualElement root)
    {
        root.Q<VisualElement>("Stats").style.display = DisplayStyle.None;
        root.Q<VisualElement>("GameOver").style.display = DisplayStyle.Flex;
        root.Q<VisualElement>("Almanac").style.display = DisplayStyle.None;
        root.Q<VisualElement>("MainMenu").style.display = DisplayStyle.None;
    }

    //public void DeadMenuUpdate(VisualElement root, ref PlayerData PlayerInfo, ref UIData UIInfo)
    //{

    //}

    #endregion

    #region PerksAndCursesMenu

    public void PerksAndCursesMenuSetup(VisualElement root, ref PlayerData PlayerInfo, ref UIData UIInfo)
    {
        root.Q<VisualElement>("GameOver").style.display = DisplayStyle.None;
        root.Q<VisualElement>("PerksAndCurses").style.display = DisplayStyle.Flex;

        UIInfo.Cost = (int)PlayerInfo.HiddenStats.y;

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

    public void PerksAndCursesMenuUpdate(VisualElement root, ref UIData UIInfo)
    {
        root.Q<Label>("Cost").text = $"Total Cost: {UIInfo.Cost}";
    }

    #endregion

    #region AlmanacMenu

    public void AlmanacMenuSetup(VisualElement root, ref UIData UIInfo)
    {
        root.Q<VisualElement>("Almanac").style.display = DisplayStyle.Flex;
        root.Q<VisualElement>("Stats").style.display = DisplayStyle.None;

        root.Q<Button>("AlmanacBack").clicked += () => OpenMenu(UIStatus.Alive);
        //root.Q<Button>("AlmanacMainMenu").clicked += () => SomethingGood(); No main menu.

        if (UIInfo.SavedWorldEntity == Entity.Null)
        {
            UIInfo.SavedWorldEntity = GetWorldPagesEntity(AlmanacWorld.World0);
            LoadPage(0, root, ref UIInfo);
        }
    }

    public void AlmanacMenuUpdate(VisualElement root, ref PlayerData PlayerInfo, ref UIData UIInfo)
    {
        AlmanacWorld CurrentWorld = (AlmanacWorld)root.Q<EnumField>("WorldDropDown").value;

        if (CurrentWorld != UIInfo.SavedWorld)
        {
            UIInfo.SavedWorldEntity = GetWorldPagesEntity(CurrentWorld);
            LoadPage(0, root, ref UIInfo);
            UIInfo.SavedWorld = CurrentWorld;
            UIInfo.SavedPageNum = 0;
            root.Q<IntegerField>("PageNum").value = 0;
            return;
        }

        int CurrentPage = root.Q<IntegerField>("PageNum").value;

        if (CurrentPage < 0)
        {
            CurrentPage = 0;
            root.Q<IntegerField>("PageNum").value = 0;
        }
        else if (CurrentPage >= UIInfo.SavedTotalPages)
        {
            CurrentPage = UIInfo.SavedTotalPages-1;
            root.Q<IntegerField>("PageNum").value = UIInfo.SavedTotalPages-1;
        }

        if (UIInfo.SavedPageNum != CurrentPage)
        {
            LoadPage(CurrentPage, root, ref UIInfo);
            UIInfo.SavedPageNum = CurrentPage;
        }
    }

    public void TurnPage(int PageAmount, VisualElement root)
    {
        root.Q<IntegerField>("PageNum").value += PageAmount;
    }

    public void LoadPage(int Page, VisualElement root, ref UIData UIInfo)
    {
        DynamicBuffer<PageElement> CurrentPages = SystemAPI.GetBuffer<PageElement>(UIInfo.SavedWorldEntity);
        ManagedIconData Icons = SystemAPI.ManagedAPI.GetComponent<ManagedIconData>(UIInfo.SavedWorldEntity);

        UIInfo.SavedTotalPages = CurrentPages.Length;

        root.Q<Label>("MainParagraph").text = CurrentPages.ElementAt(Page).Paragraph.ConvertToString();
        root.Q<Label>("PageTitle").text = CurrentPages.ElementAt(Page).Title.ConvertToString();
        root.Q<VisualElement>("PageIcon").style.backgroundImage = new StyleBackground(Icons.Icons[Page]);
    }

    public Entity GetWorldPagesEntity(AlmanacWorld DesiredWorld)
    {
        NativeReference<Entity> WEntity = new NativeReference<Entity>(Allocator.TempJob);
        AlmanacWorldEntityJob WJob = new AlmanacWorldEntityJob
        {
            DesiredWorld = DesiredWorld,
            WorldEntity = WEntity
        };

        Dependency = WJob.Schedule(Dependency);
        Dependency.Complete();

        Entity WorldEntity = WJob.WorldEntity.Value;
        WEntity.Dispose();

        if (WorldEntity == Entity.Null)
        {
            Debug.Log("What the hell?");
        }

        return WorldEntity;
    }

    [BurstCompile]
    public partial struct AlmanacWorldEntityJob : IJobEntity
    {
        [ReadOnly]
        public AlmanacWorld DesiredWorld;

        public NativeReference<Entity> WorldEntity;

        void Execute(ref AlmanacData WorldPages, Entity entity)
        {
            if (WorldPages.BelongsTo == DesiredWorld)
            {
                WorldEntity.Value = entity;
            }
        }
    }

    #endregion

    #region DeadAlmanacMenu

    public void DeadAlmanacMenuSetup(VisualElement root, ref UIData UIInfo)
    {
        root.Q<VisualElement>("Almanac").style.display = DisplayStyle.Flex;
        root.Q<VisualElement>("GameOver").style.display = DisplayStyle.None;

        root.Q<Button>("AlmanacBack").clicked += () => OpenMenu(UIStatus.Dead);
        //root.Q<Button>("AlmanacMainMenu").clicked += () => SomethingGood(); No main menu.

        if (UIInfo.SavedWorldEntity == Entity.Null)
        {
            UIInfo.SavedWorldEntity = GetWorldPagesEntity(AlmanacWorld.World0);
            LoadPage(0, root, ref UIInfo);
        }
    }

    public void DeadAlmanacMenuUpdate(VisualElement root, ref PlayerData PlayerInfo, ref UIData UIInfo)
    {

    }

    #endregion

    #endregion

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
            var OneTimeCurseArray = SystemAPI.GetSingletonBuffer<OneTimeCurseElement>();
            var CurseArray = SystemAPI.GetSingletonBuffer<CurseElement>();

            var PerkButtons = SystemAPI.GetSingletonBuffer<PerkButtonElement>();
            var OneTimePerkArray = SystemAPI.GetSingletonBuffer<OneTimePerkElement>();
            var PerkArray = SystemAPI.GetSingletonBuffer<PerkElement>();

            ref PlayerData PlayerInfo = ref SystemAPI.GetSingletonRW<PlayerData>().ValueRW;

            for (int i = 0; i < PerkButtons.Length; i++)
            {
                if (!PerkButtons[i].Selected)
                {
                    continue;
                }

                if (PerkButtons[i].OneTimeUsePerk)
                {
                    OneTimePerkArray.ElementAt(PerkButtons[i].PerkIndex).Used = true;
                }
                else
                {
                    PerkArray.ElementAt(PerkButtons[i].PerkIndex).AmountOwned++;
                }

                switch (PerkButtons[i].VarToChange)
                {
                    case Change.DefaultHealth:
                        PlayerInfo.DefaultVisibleStats.x += PerkButtons[i].AmountToChange;
                        break;

                    case Change.DefaultStamina:
                        PlayerInfo.DefaultVisibleStats.y += PerkButtons[i].AmountToChange;
                        break;

                    case Change.DefaultTeleports:
                        PlayerInfo.DefaultVisibleStats.z += PerkButtons[i].AmountToChange;
                        break;

                    case Change.DefaultStrength:
                        PlayerInfo.DefaultVisibleStats.w += PerkButtons[i].AmountToChange;
                        break;

                    case Change.DefaultVision:
                        PlayerInfo.DefaultHiddenStats.x += PerkButtons[i].AmountToChange;
                        break;

                    case Change.ChanceOfDangerousWarp:
                        PlayerInfo.ChanceOfDangerousWarp += PerkButtons[i].AmountToChange;
                        break;

                    case Change.Skills:
                        if (PerkButtons[i].AmountToChange == 1)
                        {
                            PlayerInfo.PlayerSkills |= PerkButtons[i].SkillToSet;
                        }
                        else if (PerkButtons[i].AmountToChange == -1)
                        {
                            PlayerInfo.PlayerSkills &= (~PerkButtons[i].SkillToSet);
                        }
                        else
                        {
                            Debug.Log("AmountToChange was not 1 nor -1 when expected... What happened?");
                        }
                        break;
                }
            }

            for (int i = 0; i < CurseButtons.Length; i++)
            {
                if (!CurseButtons[i].Selected)
                {
                    continue;
                }

                if (CurseButtons[i].OneTimeUseCurse)
                {
                    OneTimeCurseArray.ElementAt(CurseButtons[i].CurseIndex).Used = true;
                }
                else
                {
                    CurseArray.ElementAt(CurseButtons[i].CurseIndex).AmountOwned++;
                }

                switch (CurseButtons[i].VarToChange)
                {
                    case Change.DefaultHealth:
                        PlayerInfo.DefaultVisibleStats.x += CurseButtons[i].AmountToChange;
                        break;

                    case Change.DefaultStamina:
                        PlayerInfo.DefaultVisibleStats.y += CurseButtons[i].AmountToChange;
                        break;

                    case Change.DefaultTeleports:
                        PlayerInfo.DefaultVisibleStats.z += CurseButtons[i].AmountToChange;
                        break;

                    case Change.DefaultStrength:
                        PlayerInfo.DefaultVisibleStats.w += CurseButtons[i].AmountToChange;
                        break;

                    case Change.DefaultVision:
                        PlayerInfo.DefaultHiddenStats.x += CurseButtons[i].AmountToChange;
                        break;

                    case Change.ChanceOfDangerousWarp:
                        PlayerInfo.ChanceOfDangerousWarp += CurseButtons[i].AmountToChange;
                        break;

                    case Change.Skills:
                        if (CurseButtons[i].AmountToChange == 1)
                        {
                            PlayerInfo.PlayerSkills |= CurseButtons[i].SkillToSet;
                        }
                        else if (CurseButtons[i].AmountToChange == -1)
                        {
                            PlayerInfo.PlayerSkills &= (~CurseButtons[i].SkillToSet);
                        }
                        else
                        {
                            Debug.Log("AmountToChange was not 1 nor -1 when expected... What happened?");
                        }
                        break;
                }
            }

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

            //restart game
            ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;
            MapInfo.RestartGame = true;
        }
    }

    public void StartGame()
    {
        ref UIData UIInfo = ref SystemAPI.GetSingletonRW<UIData>().ValueRW;
        ref MapData MapInfo = ref SystemAPI.GetSingletonRW<MapData>().ValueRW;

        UIInfo.UIState = UIStatus.Alive;
        MapInfo.RestartGame = true;
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

    private UIStatus StoredState;

    public UIStatus UIState
    {
        get => StoredState;
        set
        {
            if (StoredState != value)
            {
                StoredState = value;
                Setup = false;
            }
        }
    }
    public bool Setup;
    public FixedString128Bytes BiomeName;
    public Color BiomeColour;

    public int SavedPageNum;
    public int SavedTotalPages;
    public Entity SavedWorldEntity;
    public AlmanacWorld SavedWorld;
}

public struct PerkButtonElement : IBufferElementData
{
    public int PerkIndex;
    public bool OneTimeUsePerk;
    public bool Selected;
    public int CostToAdd;
    public FixedString128Bytes Description;
    public Change VarToChange;
    public float AmountToChange;
    public Skills SkillToSet;
}

public struct CurseButtonElement : IBufferElementData
{
    public int CurseIndex;
    public bool OneTimeUseCurse;
    public bool Selected;
    public int CostToRemove;
    public FixedString128Bytes Description;
    public Change VarToChange;
    public float AmountToChange;
    public Skills SkillToSet;
}

public enum UIStatus
{
    Alive = 0,
    Dead = 1,
    PerksAndCurses = 2,
    MainMenu = 3,
    MainMenuGameOver = 4,
    Almanac = 5,
    AlmanacDead = 6,
    Settings = 7
}

/*
 * UI States:
 * 0 : alive, in game
 * 1 : dead, but has not continued
 * 2 : perk and curse screen
 * 3 : main menu
 * 4 : main menu from game over screen (prevents people from avoiding karma)
 * 5 : Almanac
 * 6 : Almanac when you are dead
 * 7 : Settings
 */

public enum Optimisation
{
    None = 0,
    Random = 1,
    Spiral = 2
}

/*
 * Optimisations:
 * 0 : none
 * 1 : random generation of blocks every frame
 */

public enum DebugFeatures
{
    None = 0,
    ShowTrueBiomeColour = 1,
    NoWarps = 2,
}

/*
 * Debug Features:
 * 0 : none
 * 1 : Shows true colour of the biome in that block.
 */

public enum Change
{
    DefaultHealth,
    DefaultStamina,
    DefaultTeleports,
    DefaultStrength,
    DefaultVision,
    ChanceOfDangerousWarp,
    Skills
}

public enum AlmanacWorld
{
    World0,
    Desert,
    Ocean,
    Mushy
}