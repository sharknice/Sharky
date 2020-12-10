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
        public IUnitManager UnitManager { get; set; }
        public EnemyRaceManager EnemyRaceManager { get; set; }
        public IBaseManager BaseManager { get; set; }
        public ITargetingManager TargetingManager { get; set; }
        public MacroManager MacroManager { get; set; }
        public NexusManager NexusManager { get; set; }
        public ChatManager ChatManager { get; set; }
        public MicroManager MicroManager { get; set; }
        public EnemyStrategyManager EnemyStrategyManager { get; set; }
        public BuildManager BuildManager { get; set; }

        public CollisionCalculator CollisionCalculator { get; set; }
        public UpgradeDataService UpgradeDataService { get; set; }
        public BuildingDataService BuildingDataService { get; set; }
        public TrainingDataService TrainingDataService { get; set; }
        public AddOnDataService AddOnDataService { get; set; }
        public MorphDataService MorphDataService { get; set; }
        public MapDataService MapDataService { get; set; }
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
        public Dictionary<string, IEnemyStrategy> EnemyStrategies { get; set; }
        public ICounterTransitioner EmptyCounterTransitioner { get; set; }
        public MacroBalancer MacroBalancer { get; set; }
        public Dictionary<Race, BuildChoices> BuildChoices { get; set; }

        public IIndividualMicroController IndividualMicroController { get; set; }
        public Dictionary<UnitTypes, IIndividualMicroController> IndividualMicroControllers { get; set; }
        public IMicroController MicroController { get; set; }
        public Dictionary<string, IMicroTask> MicroTasks { get; set; }

        public DefaultSharkyBot(GameConnection gameConnection)
        {
            var debug = false;
#if DEBUG
            debug = true;
#endif

            var framesPerSecond = 22.4f;

            SharkyOptions = new SharkyOptions { Debug = debug, FramesPerSecond = framesPerSecond };
            MacroData = new MacroData();

            Managers = new List<IManager>();


            DebugManager = new DebugManager(gameConnection, SharkyOptions);
            Managers.Add(DebugManager);

            UpgradeDataService = new UpgradeDataService();
            BuildingDataService = new BuildingDataService();
            TrainingDataService = new TrainingDataService();
            AddOnDataService = new AddOnDataService();
            MorphDataService = new MorphDataService();

            UnitDataManager = new UnitDataManager(UpgradeDataService, BuildingDataService, TrainingDataService, AddOnDataService, MorphDataService);
            Managers.Add(UnitDataManager);

            MapData = new MapData();
            MapDataService = new MapDataService(MapData);
            TargetPriorityService = new TargetPriorityService(UnitDataManager);
            CollisionCalculator = new CollisionCalculator();
            UnitManager = new UnitManager(UnitDataManager, SharkyOptions, TargetPriorityService, CollisionCalculator, MapDataService, DebugManager);
            MapManager = new MapManager(MapData, UnitManager, SharkyOptions);
            Managers.Add(MapManager);
            Managers.Add(UnitManager);

            EnemyRaceManager = new EnemyRaceManager(UnitManager, UnitDataManager);
            Managers.Add(EnemyRaceManager);

            SharkyPathFinder = new SharkyPathFinder(new Roy_T.AStar.Paths.PathFinder(), MapData, MapDataService, DebugManager);
            SharkySimplePathFinder = new SharkySimplePathFinder(MapDataService);
            NoPathFinder = new SharkyNoPathFinder();

            BaseManager = new BaseManager(UnitDataManager, UnitManager, SharkyPathFinder);
            Managers.Add(BaseManager);

            TargetingManager = new TargetingManager(UnitManager, UnitDataManager, MapDataService, BaseManager, MacroData);
            Managers.Add(TargetingManager);

            BuildOptions = new BuildOptions { StrictGasCount = false, StrictSupplyCount = false, StrictWorkerCount = false };
            MacroSetup = new MacroSetup();
            BuildingService = new BuildingService(MapData, UnitManager);
            ProtossBuildingPlacement = new ProtossBuildingPlacement(UnitManager, UnitDataManager, DebugManager, MapData, BuildingService);
            TerranBuildingPlacement = new TerranBuildingPlacement(UnitManager, UnitDataManager, DebugManager, BuildingService);
            ZergBuildingPlacement = new ZergBuildingPlacement(UnitManager, UnitDataManager, DebugManager, BuildingService);
            BuildingPlacement = new BuildingPlacement(ProtossBuildingPlacement, TerranBuildingPlacement, ZergBuildingPlacement, BaseManager, UnitManager, BuildingService, UnitDataManager);
            BuildingBuilder = new BuildingBuilder(UnitManager, TargetingManager, BuildingPlacement, UnitDataManager);

            AttackData = new AttackData { ArmyFoodAttack = 30, ArmyFoodRetreat = 25, Attacking = false, CustomAttackFunction = false };
            WarpInPlacement = new WarpInPlacement(UnitManager, DebugManager, MapData);
            
            Morpher = new Morpher(UnitManager, UnitDataManager, SharkyOptions);
            BuildPylonService = new BuildPylonService(MacroData, BuildingBuilder, UnitDataManager, UnitManager, BaseManager, TargetingManager);
            BuildDefenseService = new BuildDefenseService(MacroData, BuildingBuilder, UnitDataManager, UnitManager, BaseManager, TargetingManager);

            NexusManager = new NexusManager(UnitManager, UnitDataManager);
            Managers.Add(NexusManager);

            HttpClient = new HttpClient();
            ChatHistory = new ChatHistory();
            ChatDataService = new ChatDataService();
            EnemyNameService = new EnemyNameService();
            EnemyPlayerService = new EnemyPlayerService(EnemyNameService);
            ChatManager = new ChatManager(HttpClient, ChatHistory, SharkyOptions, ChatDataService, EnemyPlayerService, EnemyNameService);
            Managers.Add(ChatManager);

            ProxyLocationService = new ProxyLocationService(BaseManager, TargetingManager, SharkyPathFinder);

            IndividualMicroController = new IndividualMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.LiveAndAttack, false);

            var colossusMicroController = new ColossusMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false, CollisionCalculator);
            var darkTemplarMicroController = new DarkTemplarMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var disruptorMicroController = new DisruptorMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var disruptorPhasedMicroController = new DisruptorPhasedMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var mothershipMicroController = new MothershipMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var oraclepMicroController = new OracleMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var phoenixMicroController = new PhoenixMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var sentryMicroController = new SentryMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.StayOutOfRange, true);
            var stalkerMicroController = new StalkerMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var tempestMicroController = new TempestMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var voidrayMicroController = new VoidRayMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var warpPrismpMicroController = new WarpPrismMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var zealotMicroController = new ZealotMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);
            var observerMicroController = new IndividualMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.StayOutOfRange, true);

            var zerglingMicroController = new ZerglingMicroController(MapDataService, UnitDataManager, UnitManager, DebugManager, NoPathFinder, BaseManager, SharkyOptions, MicroPriority.AttackForward, false);

            IndividualMicroControllers = new Dictionary<UnitTypes, IIndividualMicroController>
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

            DefenseService = new DefenseService(UnitManager);
            MicroController = new MicroController(IndividualMicroControllers, IndividualMicroController);

            var defenseSquadTask = new DefenseSquadTask(UnitManager, TargetingManager, DefenseService, MicroController, new List<DesiredUnitsClaim>(), 0, false);
            var workerScoutTask = new WorkerScoutTask(UnitDataManager, TargetingManager, MapDataService, true, 0.5f, IndividualMicroController);
            var miningDefenseService = new MiningDefenseService(BaseManager, UnitManager);
            var miningTask = new MiningTask(UnitDataManager, BaseManager, UnitManager, 1, miningDefenseService);
            var queenInjectTask = new QueenInjectsTask(UnitManager, 1.1f);
            var attackTask = new AttackTask(MicroController, TargetingManager, UnitManager, DefenseService, MacroData, AttackData, 2);

            MicroTasks = new Dictionary<string, IMicroTask>
            {
                [defenseSquadTask.GetType().Name] = defenseSquadTask,
                [workerScoutTask.GetType().Name] = workerScoutTask,
                [miningTask.GetType().Name] = miningTask,
                [queenInjectTask.GetType().Name] = queenInjectTask,
                [attackTask.GetType().Name] = attackTask
            };

            MicroManager = new MicroManager(UnitManager, MicroTasks);
            Managers.Add(MicroManager);

            BuildProxyService = new BuildProxyService(MacroData, BuildingBuilder, UnitDataManager, UnitManager, BaseManager, TargetingManager, Morpher, MicroManager);
            MacroManager = new MacroManager(MacroSetup, UnitManager, UnitDataManager, BuildingBuilder, SharkyOptions, BaseManager, TargetingManager, AttackData, WarpInPlacement, MacroData, Morpher, BuildPylonService, BuildDefenseService, BuildProxyService);
            Managers.Add(MacroManager);

            EnemyStrategyHistory = new EnemyStrategyHistory();
            EnemyStrategies = new Dictionary<string, IEnemyStrategy>
            {
                ["Proxy"] = new EnemyStrategies.Proxy(EnemyStrategyHistory, ChatManager, UnitManager, SharkyOptions, TargetingManager),
                ["WorkerRush"] = new WorkerRush(EnemyStrategyHistory, ChatManager, UnitManager, SharkyOptions, TargetingManager),
                ["AdeptRush"] = new AdeptRush(EnemyStrategyHistory, ChatManager, UnitManager, SharkyOptions),
                ["MarineRush"] = new MarineRush(EnemyStrategyHistory, ChatManager, UnitManager, SharkyOptions),
                ["MassVikings"] = new MassVikings(EnemyStrategyHistory, ChatManager, UnitManager, SharkyOptions),
                ["ZerglingRush"] = new ZerglingRush(EnemyStrategyHistory, ChatManager, UnitManager, SharkyOptions)
            };

            EnemyStrategyManager = new EnemyStrategyManager(EnemyStrategies);
            Managers.Add(EnemyStrategyManager);

            EmptyCounterTransitioner = new EmptyCounterTransitioner(EnemyStrategyManager, SharkyOptions);

            var antiMassMarine = new AntiMassMarine(BuildOptions, MacroData, UnitManager, AttackData, ChatManager, NexusManager, EmptyCounterTransitioner);
            var fourGate = new FourGate(BuildOptions, MacroData, UnitManager, AttackData, ChatManager, NexusManager, UnitDataManager, EmptyCounterTransitioner);
            var nexusFirst = new NexusFirst(BuildOptions, MacroData, UnitManager, AttackData, ChatManager, NexusManager, EmptyCounterTransitioner);
            var robo = new Robo(BuildOptions, MacroData, UnitManager, AttackData, ChatManager, NexusManager, EnemyRaceManager, MicroManager, EmptyCounterTransitioner);
            var protossRobo = new ProtossRobo(BuildOptions, MacroData, UnitManager, AttackData, ChatManager, NexusManager, SharkyOptions, MicroManager, EnemyRaceManager, EmptyCounterTransitioner);
            var everyProtossUnit = new EveryProtossUnit(BuildOptions, MacroData, UnitManager, AttackData, ChatManager, NexusManager, EmptyCounterTransitioner);

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

            var massMarine = new MassMarines(BuildOptions, MacroData, UnitManager, AttackData, ChatManager);
            var battleCruisers = new BattleCruisers(BuildOptions, MacroData, UnitManager, AttackData, ChatManager);
            var everyTerranUnit = new EveryTerranUnit(BuildOptions, MacroData, UnitManager, AttackData, ChatManager);
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

            var basicZerglingRush = new BasicZerglingRush(BuildOptions, MacroData, UnitManager, AttackData, ChatManager, MicroManager);
            var everyZergUnit = new EveryZergUnit(BuildOptions, MacroData, UnitManager, AttackData, ChatManager);
            var zergBuilds = new Dictionary<string, ISharkyBuild>
            {
                [everyZergUnit.Name()] = everyZergUnit,
                [basicZerglingRush.Name()] = basicZerglingRush
            };
            var zergSequences = new List<List<string>>
            {
                new List<string> { everyZergUnit.Name() },
                //new List<string> { basicZerglingRush.Name(), everyZergUnit.Name() }
            };
            var zergBuildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = zergSequences,
                [Race.Zerg.ToString()] = zergSequences,
                [Race.Protoss.ToString()] = zergSequences,
                [Race.Random.ToString()] = zergSequences,
                ["Transition"] = zergSequences
            };

            MacroBalancer = new MacroBalancer(BuildOptions, UnitManager, MacroData, UnitDataManager);
            BuildChoices = new Dictionary<Race, BuildChoices>
            {
                { Race.Protoss, new BuildChoices { Builds = protossBuilds, BuildSequences = protossBuildSequences } },
                { Race.Terran, new BuildChoices { Builds = terranBuilds, BuildSequences = terranBuildSequences } },
                { Race.Zerg, new BuildChoices { Builds = zergBuilds, BuildSequences = zergBuildSequences } }
            };
            BuildDecisionService = new BuildDecisionService(ChatManager);
            BuildManager = new BuildManager(BuildChoices, DebugManager, MacroBalancer, BuildDecisionService, EnemyPlayerService, ChatHistory, EnemyStrategyHistory);
            Managers.Add(BuildManager);
        }
        public SharkyBot CreateBot(List<IManager> managers, DebugManager debugManager)
        {
            return new SharkyBot(managers, debugManager);
        }
    }
}
