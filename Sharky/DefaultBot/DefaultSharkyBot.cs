using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.Builds.BuildingPlacement;
using Sharky.Builds.MacroServices;
using Sharky.Builds.Protoss;
using Sharky.Builds.Terran;
using Sharky.Builds.Zerg;
using Sharky.Chat;
using Sharky.EnemyPlayer;
using Sharky.EnemyStrategies;
using Sharky.EnemyStrategies.Protoss;
using Sharky.EnemyStrategies.Terran;
using Sharky.EnemyStrategies.Zerg;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using Sharky.MicroControllers;
using Sharky.MicroControllers.Protoss;
using Sharky.MicroControllers.Zerg;
using Sharky.MicroTasks;
using Sharky.MicroTasks.Attack;
using Sharky.MicroTasks.Mining;
using Sharky.Pathing;
using Sharky.Proxy;
using Sharky.TypeData;
using System.Collections.Generic;
using System.Net.Http;

namespace Sharky.DefaultBot
{
    public class DefaultSharkyBot
    {
        public SharkyOptions SharkyOptions { get; set; }
        public List<IManager> Managers { get; set; }

        public DebugManager DebugManager { get; set; }

        public UnitDataManager UnitDataManager { get; set; }
        public MapManager MapManager { get; set; }
        public UnitManager UnitManager { get; set; }
        public EnemyRaceManager EnemyRaceManager { get; set; }
        public BaseManager BaseManager { get; set; }
        public TargetingManager TargetingManager { get; set; }
        public MacroManager MacroManager { get; set; }
        public NexusManager NexusManager { get; set; }
        public ChatManager ChatManager { get; set; }
        public MicroManager MicroManager { get; set; }
        public EnemyStrategyManager EnemyStrategyManager { get; set; }
        public BuildManager BuildManager { get; set; }
        public AttackDataManager AttackDataManager { get; set; }

        public CollisionCalculator CollisionCalculator { get; set; }
        public UpgradeDataService UpgradeDataService { get; set; }
        public BuildingDataService BuildingDataService { get; set; }
        public TrainingDataService TrainingDataService { get; set; }
        public AddOnDataService AddOnDataService { get; set; }
        public MorphDataService MorphDataService { get; set; }
        public MapDataService MapDataService { get; set; }
        public ChokePointService ChokePointService { get; set; }
        public TargetPriorityService TargetPriorityService { get; set; }
        public BuildingService BuildingService { get; set; }
        public BuildPylonService BuildPylonService { get; set; }
        public BuildDefenseService BuildDefenseService { get; set; }
        public BuildProxyService BuildProxyService { get; set; }
        public ChatDataService ChatDataService { get; set; }
        public EnemyNameService EnemyNameService { get; set; }
        public EnemyPlayerService EnemyPlayerService { get; set; }
        public DefenseService DefenseService { get; set; }
        public IBuildDecisionService BuildDecisionService { get; set; }
        public ProxyLocationService ProxyLocationService { get; set; }
        public UnitCountService UnitCountService { get; set; }
        public DamageService DamageService { get; set; }
        public TargetingService TargetingService { get; set; }
        public ChatService ChatService { get; set; }
        public DebugService DebugService { get; set; }
        public UnitDataService UnitDataService { get; set; }
        public BuildingCancelService BuildingCancelService { get; set; }

        public ActiveUnitData ActiveUnitData { get; set; }
        public MapData MapData { get; set; }
        public BuildOptions BuildOptions { get; set; }
        public MacroSetup MacroSetup { get; set; }
        public IBuildingPlacement ProtossBuildingPlacement { get; set; }
        public IBuildingPlacement TerranBuildingPlacement { get; set; }
        public IBuildingPlacement ZergBuildingPlacement { get; set; }
        public IBuildingPlacement BuildingPlacement { get; set; }
        public IBuildingBuilder BuildingBuilder { get; set; }
        public AttackData AttackData { get; set; }
        public IBuildingPlacement WarpInPlacement { get; set; }
        public MacroData MacroData { get; set; }
        public Morpher Morpher { get; set; }
        public HttpClient HttpClient { get; set; }
        public ChatHistory ChatHistory { get; set; }
        public IPathFinder SharkyPathFinder { get; set; }
        public IPathFinder SharkySimplePathFinder { get; set; }
        public IPathFinder NoPathFinder { get; set; }
        public EnemyStrategyHistory EnemyStrategyHistory { get; set; }
        public ICounterTransitioner EmptyCounterTransitioner { get; set; }
        public MacroBalancer MacroBalancer { get; set; }
        public Dictionary<Race, BuildChoices> BuildChoices { get; set; }

        public IIndividualMicroController IndividualMicroController { get; set; }
        public MicroData MicroData { get; set; }
        public IMicroController MicroController { get; set; }
        public MicroTaskData MicroTaskData { get; set; }
        public ChronoData ChronoData { get; set; }
        public TargetingData TargetingData { get; set; }
        public BaseData BaseData { get; set; }
        public ActiveChatData ActiveChatData { get; set; }
        public EnemyData EnemyData { get; set; }
        public SharkyUnitData SharkyUnitData { get; set; }

        public DefaultSharkyBot(GameConnection gameConnection)
        {
            var debug = false;
#if DEBUG
            debug = true;
#endif

            var framesPerSecond = 22.4f;

            SharkyOptions = new SharkyOptions { Debug = debug, FramesPerSecond = framesPerSecond };
            MacroData = new MacroData();
            AttackData = new AttackData { ArmyFoodAttack = 30, ArmyFoodRetreat = 25, Attacking = false, UseAttackDataManager = true, CustomAttackFunction = true };
            TargetingData = new TargetingData();
            BaseData = new BaseData();
            ActiveChatData = new ActiveChatData();
            EnemyData = new EnemyData();
            SharkyUnitData = new SharkyUnitData();

            UnitDataService = new UnitDataService(SharkyUnitData);

            Managers = new List<IManager>();

            DebugService = new DebugService(SharkyOptions);
            DebugManager = new DebugManager(gameConnection, SharkyOptions, DebugService);
            Managers.Add(DebugManager);

            UpgradeDataService = new UpgradeDataService();
            BuildingDataService = new BuildingDataService();
            TrainingDataService = new TrainingDataService();
            AddOnDataService = new AddOnDataService();
            MorphDataService = new MorphDataService();

            UnitDataManager = new UnitDataManager(UpgradeDataService, BuildingDataService, TrainingDataService, AddOnDataService, MorphDataService, SharkyUnitData);
            Managers.Add(UnitDataManager);

            MapData = new MapData();
            MapDataService = new MapDataService(MapData);
            TargetPriorityService = new TargetPriorityService(SharkyUnitData);
            CollisionCalculator = new CollisionCalculator();
            ActiveUnitData = new ActiveUnitData();
            UnitCountService = new UnitCountService(ActiveUnitData, SharkyUnitData);
            DamageService = new DamageService();
            
            UnitManager = new UnitManager(ActiveUnitData, SharkyUnitData, SharkyOptions, TargetPriorityService, CollisionCalculator, MapDataService, DebugService, DamageService, UnitDataService);
            MapManager = new MapManager(MapData, ActiveUnitData, SharkyOptions, SharkyUnitData);
            Managers.Add(MapManager);
            Managers.Add(UnitManager);

            EnemyRaceManager = new EnemyRaceManager(ActiveUnitData, SharkyUnitData, EnemyData);
            Managers.Add(EnemyRaceManager);

            SharkyPathFinder = new SharkyPathFinder(new Roy_T.AStar.Paths.PathFinder(), MapData, MapDataService, DebugService);
            SharkySimplePathFinder = new SharkySimplePathFinder(MapDataService);
            NoPathFinder = new SharkyNoPathFinder();
            ChokePointService = new ChokePointService(SharkyPathFinder, MapDataService);

            BaseManager = new BaseManager(SharkyUnitData, ActiveUnitData, SharkyPathFinder, UnitCountService, BaseData);
            Managers.Add(BaseManager);

            TargetingManager = new TargetingManager(SharkyUnitData, BaseData, MacroData, TargetingData, ChokePointService, DebugService);
            Managers.Add(TargetingManager);

            BuildOptions = new BuildOptions { StrictGasCount = false, StrictSupplyCount = false, StrictWorkerCount = false };
            MacroSetup = new MacroSetup();
            BuildingService = new BuildingService(MapData, ActiveUnitData);
            ProtossBuildingPlacement = new ProtossBuildingPlacement(ActiveUnitData, SharkyUnitData, DebugService, MapData, BuildingService);
            TerranBuildingPlacement = new TerranBuildingPlacement(ActiveUnitData, SharkyUnitData, DebugService, BuildingService);
            ZergBuildingPlacement = new ZergBuildingPlacement(ActiveUnitData, SharkyUnitData, DebugService, BuildingService);
            BuildingPlacement = new BuildingPlacement(ProtossBuildingPlacement, TerranBuildingPlacement, ZergBuildingPlacement, BaseData, ActiveUnitData, BuildingService, SharkyUnitData);
            BuildingBuilder = new BuildingBuilder(ActiveUnitData, TargetingData, BuildingPlacement, SharkyUnitData);

            WarpInPlacement = new WarpInPlacement(ActiveUnitData, DebugService, MapData);
            
            Morpher = new Morpher(ActiveUnitData);
            BuildPylonService = new BuildPylonService(MacroData, BuildingBuilder, SharkyUnitData, ActiveUnitData, BaseData, TargetingData);
            BuildDefenseService = new BuildDefenseService(MacroData, BuildingBuilder, SharkyUnitData, ActiveUnitData, BaseData, TargetingData);

            ChronoData = new ChronoData();
            NexusManager = new NexusManager(ActiveUnitData, SharkyUnitData, ChronoData);
            Managers.Add(NexusManager);

            HttpClient = new HttpClient();
            ChatHistory = new ChatHistory();
            ChatDataService = new ChatDataService();
            EnemyNameService = new EnemyNameService();
            EnemyPlayerService = new EnemyPlayerService(EnemyNameService);
            ChatService = new ChatService(ChatDataService, SharkyOptions, ActiveChatData);
            ChatManager = new ChatManager(HttpClient, ChatHistory, SharkyOptions, ChatDataService, EnemyPlayerService, EnemyNameService, ChatService, ActiveChatData);
            Managers.Add((IManager)ChatManager);

            ProxyLocationService = new ProxyLocationService(BaseData, TargetingData, SharkyPathFinder, MapDataService);

            var individualMicroController = new IndividualMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);

            var colossusMicroController = new ColossusMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false, CollisionCalculator);
            var darkTemplarMicroController = new DarkTemplarMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var disruptorMicroController = new DisruptorMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var disruptorPhasedMicroController = new DisruptorPhasedMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var mothershipMicroController = new MothershipMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var oraclepMicroController = new OracleMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var phoenixMicroController = new PhoenixMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var sentryMicroController = new SentryMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.StayOutOfRange, true);
            var stalkerMicroController = new StalkerMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var tempestMicroController = new TempestMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var voidrayMicroController = new VoidRayMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var warpPrismpMicroController = new WarpPrismMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false);
            var zealotMicroController = new ZealotMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.AttackForward, false);
            var observerMicroController = new IndividualMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.StayOutOfRange, true);

            var zerglingMicroController = new ZerglingMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.AttackForward, false);

            var workerDefenseMicroController = new IndividualMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, MicroPriority.LiveAndAttack, false, 3);

            var individualMicroControllers = new Dictionary<UnitTypes, IIndividualMicroController>
            {
                { UnitTypes.PROTOSS_COLOSSUS, colossusMicroController },
                { UnitTypes.PROTOSS_DARKTEMPLAR, darkTemplarMicroController },
                { UnitTypes.PROTOSS_DISRUPTOR, disruptorMicroController },
                { UnitTypes.PROTOSS_DISRUPTORPHASED, disruptorPhasedMicroController },
                { UnitTypes.PROTOSS_MOTHERSHIP, mothershipMicroController },
                { UnitTypes.PROTOSS_ORACLE, oraclepMicroController },
                { UnitTypes.PROTOSS_PHOENIX, phoenixMicroController },
                { UnitTypes.PROTOSS_SENTRY, sentryMicroController },
                { UnitTypes.PROTOSS_STALKER, stalkerMicroController },
                { UnitTypes.PROTOSS_TEMPEST, tempestMicroController },
                { UnitTypes.PROTOSS_VOIDRAY, voidrayMicroController },
                { UnitTypes.PROTOSS_WARPPRISM, warpPrismpMicroController },
                { UnitTypes.PROTOSS_WARPPRISMPHASING, warpPrismpMicroController },
                { UnitTypes.PROTOSS_ZEALOT, zealotMicroController },
                { UnitTypes.PROTOSS_OBSERVER, observerMicroController },

                { UnitTypes.ZERG_ZERGLING, zerglingMicroController }
            };

            MicroData = new MicroData { IndividualMicroControllers = individualMicroControllers, IndividualMicroController = individualMicroController };

            DefenseService = new DefenseService(ActiveUnitData);
            TargetingService = new TargetingService(ActiveUnitData, MapDataService, BaseData);
            MicroController = new MicroController(MicroData);

            var defenseSquadTask = new DefenseSquadTask(ActiveUnitData, TargetingData, DefenseService, MicroController, new List<DesiredUnitsClaim>(), 0, false);
            var workerScoutTask = new WorkerScoutTask(SharkyUnitData, TargetingData, MapDataService, false, 0.5f, workerDefenseMicroController);
            var proxyScoutTask = new ProxyScoutTask(SharkyUnitData, TargetingData, MapDataService, BaseData, false, 0.5f, workerDefenseMicroController);
            var miningDefenseService = new MiningDefenseService(BaseData, ActiveUnitData, workerDefenseMicroController, DebugService);
            var miningTask = new MiningTask(SharkyUnitData, BaseData, ActiveUnitData, 1, miningDefenseService, MacroData);
            var queenInjectTask = new QueenInjectsTask(ActiveUnitData, 1.1f, UnitCountService);
            var attackTask = new AttackTask(MicroController, TargetingData, ActiveUnitData, DefenseService, MacroData, AttackData, TargetingService, 2);

            MicroTaskData = new MicroTaskData
            {
                MicroTasks = new Dictionary<string, IMicroTask>
                {
                    [defenseSquadTask.GetType().Name] = defenseSquadTask,
                    [workerScoutTask.GetType().Name] = workerScoutTask,
                    [proxyScoutTask.GetType().Name] = proxyScoutTask,
                    [miningTask.GetType().Name] = miningTask,
                    [queenInjectTask.GetType().Name] = queenInjectTask,
                    [attackTask.GetType().Name] = attackTask
                }
            };

            MicroManager = new MicroManager(ActiveUnitData, MicroTaskData);
            Managers.Add(MicroManager);

            AttackDataManager = new AttackDataManager(AttackData, ActiveUnitData, attackTask, TargetPriorityService, TargetingData, MacroData, DebugService);
            Managers.Add(AttackDataManager);

            BuildProxyService = new BuildProxyService(MacroData, BuildingBuilder, SharkyUnitData, ActiveUnitData, Morpher, MicroTaskData);
            BuildingCancelService = new BuildingCancelService(ActiveUnitData, MacroData);
            MacroManager = new MacroManager(MacroSetup, ActiveUnitData, SharkyUnitData, BuildingBuilder, SharkyOptions, BaseData, TargetingData, AttackData, WarpInPlacement, MacroData, Morpher, BuildPylonService, BuildDefenseService, BuildProxyService, UnitCountService, BuildingCancelService);
            Managers.Add(MacroManager);

            EnemyStrategyHistory = new EnemyStrategyHistory();
            EnemyData.EnemyStrategies = new Dictionary<string, IEnemyStrategy>
            {
                ["Proxy"] = new EnemyStrategies.Proxy(EnemyStrategyHistory, ChatService, ActiveUnitData, SharkyOptions, TargetingData, DebugService, UnitCountService),
                ["WorkerRush"] = new WorkerRush(EnemyStrategyHistory, ChatService, ActiveUnitData, SharkyOptions, TargetingData, DebugService, UnitCountService),
                ["InvisibleAttacks"] = new InvisibleAttacks(EnemyStrategyHistory, ChatService, ActiveUnitData, SharkyOptions, DebugService, UnitCountService),
                ["AdeptRush"] = new AdeptRush(EnemyStrategyHistory, ChatService, ActiveUnitData, SharkyOptions, DebugService, UnitCountService),
                ["CannonRush"] = new CannonRush(EnemyStrategyHistory, ChatService, ActiveUnitData, SharkyOptions, TargetingData, DebugService, UnitCountService),
                ["MarineRush"] = new MarineRush(EnemyStrategyHistory, ChatService, ActiveUnitData, SharkyOptions, DebugService, UnitCountService),
                ["MassVikings"] = new MassVikings(EnemyStrategyHistory, ChatService, ActiveUnitData, SharkyOptions, DebugService, UnitCountService),
                ["ZerglingRush"] = new ZerglingRush(EnemyStrategyHistory, ChatService, ActiveUnitData, SharkyOptions, DebugService, UnitCountService)
            };

            EnemyStrategyManager = new EnemyStrategyManager(EnemyData);
            Managers.Add(EnemyStrategyManager);

            EmptyCounterTransitioner = new EmptyCounterTransitioner(EnemyData, SharkyOptions);

            var antiMassMarine = new AntiMassMarine(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, ChronoData, EmptyCounterTransitioner, UnitCountService);
            var fourGate = new FourGate(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, ChronoData, SharkyUnitData, EmptyCounterTransitioner, UnitCountService);
            var nexusFirst = new NexusFirst(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, ChronoData, EmptyCounterTransitioner, UnitCountService);
            var robo = new Robo(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, ChronoData, EnemyData, MicroTaskData, EmptyCounterTransitioner, UnitCountService);
            var protossRobo = new ProtossRobo(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, ChronoData, SharkyOptions, MicroTaskData, EnemyData, EmptyCounterTransitioner, UnitCountService);
            var everyProtossUnit = new EveryProtossUnit(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, ChronoData, EmptyCounterTransitioner, UnitCountService);

            var protossBuilds = new Dictionary<string, ISharkyBuild>
            {
                [everyProtossUnit.Name()] = everyProtossUnit,
                [nexusFirst.Name()] = nexusFirst,
                [robo.Name()] = robo,
                [protossRobo.Name()] = protossRobo,
                [fourGate.Name()] = fourGate,
                [antiMassMarine.Name()] = antiMassMarine
            };
            var protossSequences = new List<List<string>>
            {
                new List<string> { everyProtossUnit.Name() },
                new List<string> { nexusFirst.Name(), robo.Name(), protossRobo.Name() },
                new List<string> { antiMassMarine.Name() },
                new List<string> { fourGate.Name() }
            };
            var protossBuildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = protossSequences,
                [Race.Zerg.ToString()] = protossSequences,
                [Race.Protoss.ToString()] = protossSequences,
                [Race.Random.ToString()] = protossSequences,
                ["Transition"] = protossSequences
            };

            var massMarine = new MassMarines(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, UnitCountService);
            var battleCruisers = new BattleCruisers(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, UnitCountService);
            var everyTerranUnit = new EveryTerranUnit(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, MicroTaskData, UnitCountService);
            var terranBuilds = new Dictionary<string, ISharkyBuild>
            {
                [massMarine.Name()] = massMarine,
                [battleCruisers.Name()] = battleCruisers,
                [everyTerranUnit.Name()] = everyTerranUnit
            };
            var terranSequences = new List<List<string>>
            {
                new List<string> { massMarine.Name(), battleCruisers.Name() },
                new List<string> { everyTerranUnit.Name() },
                new List<string> { battleCruisers.Name() },
            };
            var terranBuildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = terranSequences,
                [Race.Zerg.ToString()] = terranSequences,
                [Race.Protoss.ToString()] = terranSequences,
                [Race.Random.ToString()] = terranSequences,
                ["Transition"] = terranSequences
            };

            var basicZerglingRush = new BasicZerglingRush(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, MicroTaskData, UnitCountService);
            var everyZergUnit = new EveryZergUnit(BuildOptions, MacroData, ActiveUnitData, AttackData, ChatService, UnitCountService);
            var zergBuilds = new Dictionary<string, ISharkyBuild>
            {
                [everyZergUnit.Name()] = everyZergUnit,
                [basicZerglingRush.Name()] = basicZerglingRush
            };
            var zergSequences = new List<List<string>>
            {
                new List<string> { everyZergUnit.Name() },
                new List<string> { basicZerglingRush.Name(), everyZergUnit.Name() }
            };
            var zergBuildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = zergSequences,
                [Race.Zerg.ToString()] = zergSequences,
                [Race.Protoss.ToString()] = zergSequences,
                [Race.Random.ToString()] = zergSequences,
                ["Transition"] = zergSequences
            };

            MacroBalancer = new MacroBalancer(BuildOptions, ActiveUnitData, MacroData, SharkyUnitData, UnitCountService);
            BuildChoices = new Dictionary<Race, BuildChoices>
            {
                { Race.Protoss, new BuildChoices { Builds = protossBuilds, BuildSequences = protossBuildSequences } },
                { Race.Terran, new BuildChoices { Builds = terranBuilds, BuildSequences = terranBuildSequences } },
                { Race.Zerg, new BuildChoices { Builds = zergBuilds, BuildSequences = zergBuildSequences } }
            };
            BuildDecisionService = new BuildDecisionService(ChatService);
            BuildManager = new BuildManager(BuildChoices, DebugService, MacroBalancer, BuildDecisionService, EnemyPlayerService, ChatHistory, EnemyStrategyHistory);
            Managers.Add(BuildManager);
        }
        public SharkyBot CreateBot(List<IManager> managers, DebugService debugService)
        {
            return new SharkyBot(managers, DebugService);
        }
    }
}
