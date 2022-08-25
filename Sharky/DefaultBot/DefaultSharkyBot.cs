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
using Sharky.Macro;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using Sharky.Managers.Terran;
using Sharky.MicroControllers;
using Sharky.MicroControllers.Protoss;
using Sharky.MicroControllers.Terran;
using Sharky.MicroControllers.Zerg;
using Sharky.MicroTasks;
using Sharky.MicroTasks.Attack;
using Sharky.MicroTasks.Harass;
using Sharky.MicroTasks.Macro;
using Sharky.MicroTasks.Mining;
using Sharky.MicroTasks.Scout;
using Sharky.MicroTasks.Zerg;
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
        public FrameToTimeConverter FrameToTimeConverter { get; set; }
        public List<IManager> Managers { get; set; }

        public DebugManager DebugManager { get; set; }
        public ReportingManager ReportingManager { get; set; }

        public UnitDataManager UnitDataManager { get; set; }
        public MapManager MapManager { get; set; }
        public UnitManager UnitManager { get; set; }
        public EnemyRaceManager EnemyRaceManager { get; set; }
        public BaseManager BaseManager { get; set; }
        public TargetingManager TargetingManager { get; set; }
        public MacroManager MacroManager { get; set; }
        public NexusManager NexusManager { get; set; }
        public OrbitalManager OrbitalManager { get; set; }
        public RallyPointManager RallyPointManager { get; set; }
        public SupplyDepotManager SupplyDepotManager { get; set; }
        public ShieldBatteryManager ShieldBatteryManager { get; set; }
        public PhotonCannonManager PhotonCannonManager { get; set; }
        public ChatManager ChatManager { get; set; }
        public MicroManager MicroManager { get; set; }
        public EnemyStrategyManager EnemyStrategyManager { get; set; }
        public BuildManager BuildManager { get; set; }
        public AttackDataManager AttackDataManager { get; set; }

        public VespeneGasBuilder VespeneGasBuilder { get; set; }
        public UnitBuilder UnitBuilder { get; set; }
        public UpgradeResearcher UpgradeResearcher { get; set; }
        public SupplyBuilder SupplyBuilder { get; set; }
        public ProductionBuilder ProductionBuilder { get; set; }
        public TechBuilder TechBuilder { get; set; }
        public AddOnBuilder AddOnBuilder { get; set; }
        public BuildingMorpher BuildingMorpher { get; set; }
        public UnfinishedBuildingCompleter UnfinishedBuildingCompleter { get; set; }
        public CollisionCalculator CollisionCalculator { get; set; }
        public UpgradeDataService UpgradeDataService { get; set; }
        public BuildingDataService BuildingDataService { get; set; }
        public TrainingDataService TrainingDataService { get; set; }
        public AddOnDataService AddOnDataService { get; set; }
        public MorphDataService MorphDataService { get; set; }
        public MapDataService MapDataService { get; set; }
        public ChokePointService ChokePointService { get; set; }
        public ChokePointsService ChokePointsService { get; set; }
        public TargetPriorityService TargetPriorityService { get; set; }
        public BuildingService BuildingService { get; set; }
        public WallService WallService { get; set; }
        public TerranWallService TerranWallService { get; set; }
        public ProtossWallService ProtossWallService { get; set; }
        public BuildPylonService BuildPylonService { get; set; }
        public BuildDefenseService BuildDefenseService { get; set; }
        public BuildProxyService BuildProxyService { get; set; }
        public BuildAddOnSwapService BuildAddOnSwapService { get; set; }
        public ChatDataService ChatDataService { get; set; }
        public EnemyNameService EnemyNameService { get; set; }
        public EnemyPlayerService EnemyPlayerService { get; set; }
        public DefenseService DefenseService { get; set; }
        public EnemyAggressivityService EnemyAggressivityService { get; set; }
        public BuildMatcher BuildMatcher { get; set; }
        public RecordService RecordService { get; set; }
        public IBuildDecisionService BuildDecisionService { get; set; }
        public ProxyLocationService ProxyLocationService { get; set; }
        public UnitCountService UnitCountService { get; set; }
        public DamageService DamageService { get; set; }
        public TargetingService TargetingService { get; set; }
        public ChatService ChatService { get; set; }
        public DebugService DebugService { get; set; }
        public VersionService VersionService { get; set; }
        public UnitDataService UnitDataService { get; set; }
        public BuildingCancelService BuildingCancelService { get; set; }
        public BuildingRequestCancellingService BuildingRequestCancellingService { get; set; }
        public UpgradeRequestCancellingService UpgradeRequestCancellingService { get; set; }
        public AreaService AreaService { get; set; }
        public WallDataService WallDataService { get; set; }
        public SimCityService SimCityService { get; set; }
        public WorkerBuilderService WorkerBuilderService { get; set; }
        public CreepTumorPlacementFinder CreepTumorPlacementFinder { get; set; }

        public ActiveUnitData ActiveUnitData { get; set; }
        public MapData MapData { get; set; }
        public BuildOptions BuildOptions { get; set; }
        public MacroSetup MacroSetup { get; set; }
        public IBuildingPlacement ProtossBuildingPlacement { get; set; }
        public IBuildingPlacement WallOffPlacement { get; set; }
        public IBuildingPlacement TerranBuildingPlacement { get; set; }
        public IBuildingPlacement ProtossDefensiveGridPlacement { get; set; }
        public IBuildingPlacement ProtossProxyGridPlacement { get; set; }
        public IBuildingPlacement ProtectNexusPylonPlacement { get; set; }
        public IBuildingPlacement ProtectNexusCannonPlacement { get; set; }
        public IBuildingPlacement ProtectNexusBatteryPlacement { get; set; }
        public IBuildingPlacement MissileTurretPlacement { get; set; }
        public IBuildingPlacement ZergBuildingPlacement { get; set; }
        public IBuildingPlacement ZergGridPlacement { get; set; }
        public IBuildingPlacement BuildingPlacement { get; set; }
        public StasisWardPlacement StasisWardPlacement { get; set; }
        public IBuildingBuilder BuildingBuilder { get; set; }
        public TerranSupplyDepotGridPlacement TerranSupplyDepotGridPlacement { get; set; }
        public TerranProductionGridPlacement TerranProductionGridPlacement { get; set; }
        public TerranTechGridPlacement TerranTechGridPlacement { get; set; }
        public ProtossPylonGridPlacement ProtossPylonGridPlacement { get; set; }
        public ProtossProductionGridPlacement ProtossProductionGridPlacement { get; set; }
        public ResourceCenterLocator ResourceCenterLocator { get; set; }
        public AttackData AttackData { get; set; }
        public IBuildingPlacement WarpInPlacement { get; set; }
        public MacroData MacroData { get; set; }
        public Morpher Morpher { get; set; }
        public HttpClient HttpClient { get; set; }
        public ChatHistory ChatHistory { get; set; }
        public IPathFinder SharkyPathFinder { get; set; }
        public IPathFinder SharkySimplePathFinder { get; set; }
        public IPathFinder SharkyAdvancedPathFinder { get; set; }
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
        public PerformanceData PerformanceData { get; set; }
        public SharkyUnitData SharkyUnitData { get; set; }
        public MineralWalker MineralWalker { get; set; }
        public UnitTypeBuildClassifications UnitTypeBuildClassifications { get; set; }

        public DefaultSharkyBot(GameConnection gameConnection)
        {
            var debug = false;
#if DEBUG
            debug = true;
#endif

            var framesPerSecond = 22.4f;

            SharkyOptions = new SharkyOptions { Debug = debug, FramesPerSecond = framesPerSecond, TagsEnabled = true, BuildTagsEnabled = true, LogPerformance = false, GameStatusReportingEnabled = true, TagsAllChat = false };
            FrameToTimeConverter = new FrameToTimeConverter(SharkyOptions);
            MacroData = new MacroData();
            AttackData = new AttackData { ArmyFoodAttack = 30, ArmyFoodRetreat = 25, Attacking = false, UseAttackDataManager = true, CustomAttackFunction = true, RetreatTrigger = 1f, AttackTrigger = 1.5f, RequireDetection = false, RequireMaxOut = false, AttackWhenMaxedOut = true, AttackWhenOverwhelm = true, ContainTrigger = 1.5f, KillTrigger = 3f };
            TargetingData = new TargetingData { HiddenEnemyBase = false };
            BaseData = new BaseData();
            MapData = new MapData();
            ActiveChatData = new ActiveChatData();
            EnemyData = new EnemyData();
            PerformanceData = new PerformanceData();
            SharkyUnitData = new SharkyUnitData { CorrosiveBiles = new Dictionary<Point2D, uint>() };
            ActiveUnitData = new ActiveUnitData();

            UnitDataService = new UnitDataService(SharkyUnitData, SharkyOptions, MacroData);
            VersionService = new VersionService();
            UnitTypeBuildClassifications = new UnitTypeBuildClassifications();

            MineralWalker = new MineralWalker(BaseData);

            Managers = new List<IManager>();

            DebugService = new DebugService(SharkyOptions, ActiveUnitData);
            DebugManager = new DebugManager(gameConnection, SharkyOptions, DebugService);
            Managers.Add(DebugManager);

            ReportingManager = new ReportingManager(this);
            Managers.Add(ReportingManager);

            UpgradeDataService = new UpgradeDataService();
            BuildingDataService = new BuildingDataService();
            TrainingDataService = new TrainingDataService();
            AddOnDataService = new AddOnDataService();
            MorphDataService = new MorphDataService();       

            UnitDataManager = new UnitDataManager(UpgradeDataService, BuildingDataService, TrainingDataService, AddOnDataService, MorphDataService, SharkyUnitData);
            Managers.Add(UnitDataManager);

            MapDataService = new MapDataService(MapData);
            AreaService = new AreaService(MapDataService);
            TargetPriorityService = new TargetPriorityService(SharkyUnitData);
            CollisionCalculator = new CollisionCalculator();

            SharkyPathFinder = new SharkyPathFinder(new Roy_T.AStar.Paths.PathFinder(), MapData, MapDataService, DebugService);
            SharkySimplePathFinder = new SharkySimplePathFinder(MapDataService);
            SharkyAdvancedPathFinder = new SharkyAdvancedPathFinder(new Roy_T.AStar.Paths.PathFinder(), MapData, MapDataService, DebugService);
            NoPathFinder = new SharkyNoPathFinder();

            UnitCountService = new UnitCountService(ActiveUnitData, SharkyUnitData);
            DamageService = new DamageService();
            BuildingService = new BuildingService(MapData, ActiveUnitData, TargetingData, BaseData, SharkyUnitData);

            UnitManager = new UnitManager(ActiveUnitData, SharkyUnitData, SharkyOptions, TargetPriorityService, CollisionCalculator, MapDataService, DebugService, DamageService, UnitDataService);
            Managers.Add(UnitManager);

            HttpClient = new HttpClient();
            ChatHistory = new ChatHistory();
            ChatDataService = new ChatDataService();
            EnemyNameService = new EnemyNameService();
            EnemyPlayerService = new EnemyPlayerService(EnemyNameService);
            ChatService = new ChatService(ChatDataService, SharkyOptions, ActiveChatData, EnemyData);
            EnemyRaceManager = new EnemyRaceManager(ActiveUnitData, SharkyUnitData, EnemyData, SharkyOptions, ChatService);
            Managers.Add(EnemyRaceManager);

            ChokePointService = new ChokePointService(SharkyPathFinder, MapDataService, BuildingService);
            ChokePointsService = new ChokePointsService(SharkyPathFinder, ChokePointService);

            WallDataService = new WallDataService(this);
            MapManager = new MapManager(MapData, ActiveUnitData, SharkyOptions, SharkyUnitData, DebugService, WallDataService);
            Managers.Add(MapManager);

            BaseManager = new BaseManager(SharkyUnitData, ActiveUnitData, SharkyPathFinder, UnitCountService, BaseData);
            Managers.Add(BaseManager);

            TargetingManager = new TargetingManager(this);
            Managers.Add(TargetingManager);

            BuildOptions = new BuildOptions { StrictGasCount = false, StrictSupplyCount = false, StrictWorkerCount = false };
            MacroSetup = new MacroSetup();
            WallService = new WallService(this);
            TerranWallService = new TerranWallService(ActiveUnitData, MapData, BaseData, WallService);
            ProtossWallService = new ProtossWallService(SharkyUnitData, ActiveUnitData, WallService);
            WallOffPlacement = new HardCodedWallOffPlacement(ActiveUnitData, SharkyUnitData, MapData, BaseData, WallService, TerranWallService, ProtossWallService);
            ProtossPylonGridPlacement = new ProtossPylonGridPlacement(BaseData, MapDataService, DebugService, BuildingService);
            ProtossProductionGridPlacement = new ProtossProductionGridPlacement(BaseData, ActiveUnitData, MapDataService, DebugService, BuildingService);
            TerranProductionGridPlacement = new TerranProductionGridPlacement(BaseData, MapDataService, DebugService, BuildingService);
            ProtectNexusPylonPlacement = new ProtectNexusPylonPlacement(this);
            ProtectNexusCannonPlacement = new ProtectNexusCannonPlacement(this);
            ProtectNexusBatteryPlacement = new ProtectNexusBatteryPlacement(this);
            TerranTechGridPlacement = new TerranTechGridPlacement(BaseData, MapDataService, DebugService, BuildingService, TerranProductionGridPlacement);
            TerranSupplyDepotGridPlacement = new TerranSupplyDepotGridPlacement(BaseData, MapDataService, DebugService, BuildingService);
            MissileTurretPlacement = new MissileTurretPlacement(this);
            TerranBuildingPlacement = new TerranBuildingPlacement(ActiveUnitData, SharkyUnitData, BaseData, DebugService, BuildingService, WallOffPlacement, TerranWallService, TerranSupplyDepotGridPlacement, TerranProductionGridPlacement, TerranTechGridPlacement, MissileTurretPlacement);
            ProtossDefensiveGridPlacement = new ProtossDefensiveGridPlacement(this);
            ProtossProxyGridPlacement = new ProtossProxyGridPlacement(this);
            ProtossBuildingPlacement = new ProtossBuildingPlacement(ActiveUnitData, SharkyUnitData, BaseData, DebugService, MapDataService, BuildingService, WallOffPlacement, ProtossPylonGridPlacement, ProtossProductionGridPlacement, ProtectNexusPylonPlacement, TargetingData, ProtectNexusCannonPlacement, BuildOptions, ProtossDefensiveGridPlacement, ProtossProxyGridPlacement);
            ZergBuildingPlacement = new ZergBuildingPlacement(ActiveUnitData, SharkyUnitData, DebugService, BuildingService);
            ZergGridPlacement = new ZergGridPlacement(this);
            ResourceCenterLocator = new ResourceCenterLocator(this);
            BuildingPlacement = new BuildingPlacement(ProtossBuildingPlacement, TerranBuildingPlacement, ZergBuildingPlacement, ResourceCenterLocator, BaseData, SharkyUnitData, MacroData, UnitCountService);
            StasisWardPlacement = new StasisWardPlacement(this);
            WorkerBuilderService = new WorkerBuilderService(this);
            SimCityService = new SimCityService(this);
            BuildingBuilder = new BuildingBuilder(ActiveUnitData, TargetingData, BuildingPlacement, SharkyUnitData, BaseData, BuildingService, MapDataService, WorkerBuilderService);

            WarpInPlacement = new WarpInPlacement(ActiveUnitData, DebugService, MapData, MapDataService, BuildingService);
            
            Morpher = new Morpher(ActiveUnitData);
            BuildPylonService = new BuildPylonService(MacroData, BuildingBuilder, SharkyUnitData, ActiveUnitData, BaseData, TargetingData, BuildingService);
            BuildDefenseService = new BuildDefenseService(MacroData, BuildingBuilder, SharkyUnitData, ActiveUnitData, BaseData, TargetingData, BuildOptions, BuildingService);

            ChronoData = new ChronoData();
            NexusManager = new NexusManager(ActiveUnitData, SharkyUnitData, ChronoData);
            Managers.Add(NexusManager);
            ShieldBatteryManager = new ShieldBatteryManager(ActiveUnitData);
            Managers.Add(ShieldBatteryManager);
            PhotonCannonManager = new PhotonCannonManager(ActiveUnitData);
            Managers.Add(PhotonCannonManager);

            RallyPointManager = new RallyPointManager(ActiveUnitData, TargetingData, MapData, WallService);
            Managers.Add(RallyPointManager);

            OrbitalManager = new OrbitalManager(ActiveUnitData, BaseData, EnemyData, MacroData, UnitCountService, ChatService, ResourceCenterLocator, MapDataService, SharkyUnitData);
            Managers.Add(OrbitalManager);
            SupplyDepotManager = new SupplyDepotManager(ActiveUnitData);
            Managers.Add(SupplyDepotManager);

            ChatManager = new ChatManager(HttpClient, ChatHistory, SharkyOptions, ChatDataService, EnemyPlayerService, EnemyNameService, ChatService, ActiveChatData, FrameToTimeConverter, VersionService);
            Managers.Add(ChatManager);

            ProxyLocationService = new ProxyLocationService(BaseData, TargetingData, SharkyPathFinder, MapDataService, AreaService);
            TargetingService = new TargetingService(ActiveUnitData, MapDataService, BaseData, TargetingData);
            CreepTumorPlacementFinder = new CreepTumorPlacementFinder(this, SharkyPathFinder);

            EnemyAggressivityService = new EnemyAggressivityService(this);

            var individualMicroController = new IndividualMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);

            var adeptMicroController = new AdeptMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var adeptShadeMicroController = new AdeptShadeMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var archonMicroController = new ArchonMicroController(this, SharkySimplePathFinder, MicroPriority.AttackForward, false);
            var colossusMicroController = new ColossusMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var darkTemplarMicroController = new DarkTemplarMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var disruptorMicroController = new DisruptorMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false);
            var disruptorPhasedMicroController = new DisruptorPhasedMicroController(this, SharkySimplePathFinder, MicroPriority.AttackForward, false);
            var highTemplarMicroController = new HighTemplarMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false);
            var mothershipMicroController = new MothershipMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false);
            var oracleMicroController = new OracleMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var observerMicroController = new ObserverMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var phoenixMicroController = new PhoenixMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, true);
            var sentryMicroController = new SentryMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, true);
            var stalkerMicroController = new StalkerMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var tempestMicroController = new TempestMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var voidrayMicroController = new VoidRayMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var carrierMicroController = new CarrierMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var interceptorMicroController = new InterceptorMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var warpPrismpMicroController = new WarpPrismMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var zealotMicroController = new ZealotMicroController(this, SharkySimplePathFinder, MicroPriority.AttackForward, false);

            var zerglingMicroController = new ZerglingMicroController(this, SharkySimplePathFinder, MicroPriority.AttackForward, false);
            var banelingMicroController = new BanelingMicroController(this, SharkySimplePathFinder, MicroPriority.AttackForward, false);
            var roachMicroController = new RoachMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false, SharkyUnitData);
            var ravagerMicroController = new RavagerMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var lurkerMicroController = new LurkerMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var lurkerBurrowedMicroController = new LurkerBurrowedMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var overseerMicroController = new OverseerMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false);
            var infestorMicroController = new InfestorMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false);
            var infestorBurrowedMicroController = new InfestorBurrowedMicroController(this, SharkySimplePathFinder, MicroPriority.JustLive, false);
            var ultraliskMicroController = new UltraliskMicroController(this, SharkySimplePathFinder, MicroPriority.AttackForward, false);
            var swarmHostMicroController = new SwarmHostMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false);
            var locustMicroController = new LocustMicroController(this, SharkySimplePathFinder, MicroPriority.AttackForward, false);
            var corruptorMicroController = new CorruptorMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var broodlordMicroController = new BroodlordMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var viperMicroController = new ViperMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false);
            var queenMicroController = new QueenMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);

            var scvMicroController = new ScvMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var reaperMicroController = new ReaperMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var marineMicroController = new MarineMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var marauderMicroController = new MarauderMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var hellionMicroController = new HellionMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var cycloneMicroController = new CycloneMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false);
            var siegeTankMicroController = new SiegeTankMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var siegeTankSiegedMicroController = new SiegeTankSiegedMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var thorMicroController = new ThorMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var vikingMicroController = new VikingMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var vikingLandedMicroController = new VikingLandedMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var bansheeMicroController = new BansheeMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);
            var ravenMicroController = new RavenMicroController(this, SharkySimplePathFinder, MicroPriority.LiveAndAttack, false);

            var workerDefenseMicroController = new IndividualMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkySimplePathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, TargetingData, TargetingService, MicroPriority.LiveAndAttack, false, 3);
            var workerProxyScoutMicroController = new WorkerScoutMicroController(MapDataService, SharkyUnitData, ActiveUnitData, DebugService, SharkyAdvancedPathFinder, BaseData, SharkyOptions, DamageService, UnitDataService, TargetingData, TargetingService, MicroPriority.AttackForward, false);

            var oracleHarassMicroController = new OracleMicroController(this, SharkyAdvancedPathFinder, MicroPriority.LiveAndAttack, false);
            var reaperHarassMicroController = new ReaperMicroController(this, SharkyAdvancedPathFinder, MicroPriority.LiveAndAttack, false);

            var individualMicroControllers = new Dictionary<UnitTypes, IIndividualMicroController>
            {
                { UnitTypes.PROTOSS_ADEPT, adeptMicroController },
                { UnitTypes.PROTOSS_ADEPTPHASESHIFT, adeptShadeMicroController },
                { UnitTypes.PROTOSS_ARCHON, archonMicroController },
                { UnitTypes.PROTOSS_COLOSSUS, colossusMicroController },
                { UnitTypes.PROTOSS_DARKTEMPLAR, darkTemplarMicroController },
                { UnitTypes.PROTOSS_DISRUPTOR, disruptorMicroController },
                { UnitTypes.PROTOSS_DISRUPTORPHASED, disruptorPhasedMicroController },
                { UnitTypes.PROTOSS_HIGHTEMPLAR, highTemplarMicroController },
                { UnitTypes.PROTOSS_MOTHERSHIP, mothershipMicroController },
                { UnitTypes.PROTOSS_ORACLE, oracleMicroController },
                { UnitTypes.PROTOSS_PHOENIX, phoenixMicroController },
                { UnitTypes.PROTOSS_SENTRY, sentryMicroController },
                { UnitTypes.PROTOSS_STALKER, stalkerMicroController },
                { UnitTypes.PROTOSS_TEMPEST, tempestMicroController },
                { UnitTypes.PROTOSS_VOIDRAY, voidrayMicroController },
                { UnitTypes.PROTOSS_CARRIER, carrierMicroController },
                { UnitTypes.PROTOSS_INTERCEPTOR, interceptorMicroController },
                { UnitTypes.PROTOSS_WARPPRISM, warpPrismpMicroController },
                { UnitTypes.PROTOSS_WARPPRISMPHASING, warpPrismpMicroController },
                { UnitTypes.PROTOSS_ZEALOT, zealotMicroController },
                { UnitTypes.PROTOSS_OBSERVER, observerMicroController },

                { UnitTypes.ZERG_ZERGLING, zerglingMicroController },
                { UnitTypes.ZERG_BANELING, banelingMicroController },
                { UnitTypes.ZERG_ROACH, roachMicroController },
                { UnitTypes.ZERG_ROACHBURROWED, roachMicroController },
                { UnitTypes.ZERG_RAVAGER, ravagerMicroController },
                { UnitTypes.ZERG_LURKERMP, lurkerMicroController },
                { UnitTypes.ZERG_LURKERMPBURROWED, lurkerBurrowedMicroController },
                { UnitTypes.ZERG_OVERSEER, overseerMicroController },
                { UnitTypes.ZERG_INFESTOR, infestorMicroController },
                { UnitTypes.ZERG_INFESTORBURROWED, infestorBurrowedMicroController },
                { UnitTypes.ZERG_ULTRALISK, ultraliskMicroController },
                { UnitTypes.ZERG_SWARMHOSTMP, swarmHostMicroController },
                { UnitTypes.ZERG_LOCUSTMP, locustMicroController },
                { UnitTypes.ZERG_LOCUSTMPFLYING, locustMicroController },
                { UnitTypes.ZERG_BROODLING, locustMicroController },
                { UnitTypes.ZERG_CORRUPTOR, corruptorMicroController },
                { UnitTypes.ZERG_BROODLORD, broodlordMicroController },
                { UnitTypes.ZERG_VIPER, viperMicroController },
                { UnitTypes.ZERG_QUEEN, queenMicroController },

                { UnitTypes.TERRAN_SCV, scvMicroController },
                { UnitTypes.TERRAN_REAPER, reaperMicroController },
                { UnitTypes.TERRAN_MARINE, marineMicroController },
                { UnitTypes.TERRAN_MARAUDER, marauderMicroController },
                { UnitTypes.TERRAN_HELLION, hellionMicroController },
                { UnitTypes.TERRAN_CYCLONE, cycloneMicroController },
                { UnitTypes.TERRAN_SIEGETANK, siegeTankMicroController },
                { UnitTypes.TERRAN_SIEGETANKSIEGED, siegeTankSiegedMicroController },
                { UnitTypes.TERRAN_THOR, thorMicroController },
                { UnitTypes.TERRAN_VIKINGFIGHTER, vikingMicroController },
                { UnitTypes.TERRAN_VIKINGASSAULT, vikingLandedMicroController },
                { UnitTypes.TERRAN_BANSHEE, bansheeMicroController },
                { UnitTypes.TERRAN_RAVEN, ravenMicroController }
            };

            MicroData = new MicroData { IndividualMicroControllers = individualMicroControllers, IndividualMicroController = individualMicroController };

            DefenseService = new DefenseService(ActiveUnitData, TargetPriorityService);
            MicroController = new MicroController(MicroData);

            MicroTaskData = new MicroTaskData { MicroTasks = new Dictionary<string, IMicroTask>() };

            var defenseSquadTask = new DefenseSquadTask(ActiveUnitData, TargetingData, DefenseService, MicroController, new ArmySplitter(AttackData, TargetingData, ActiveUnitData, DefenseService, TargetingService, TerranWallService, MicroController), new List<DesiredUnitsClaim>(), 0, false);
            var workerScoutTask = new WorkerScoutTask(this, false, 0.5f);
            var workerScoutGasStealTask = new WorkerScoutGasStealTask(this, false, 0.5f);
            var reaperScoutTask = new ReaperScoutTask(this, false, 0.5f);
            var findHiddenBaseTask = new FindHiddenBaseTask(BaseData, TargetingData, MapDataService, individualMicroController, 15, false, 0.5f);
            var proxyScoutTask = new ProxyScoutTask(SharkyUnitData, TargetingData, BaseData, SharkyOptions, false, 0.5f, workerProxyScoutMicroController);
            var miningDefenseService = new MiningDefenseService(BaseData, ActiveUnitData, workerDefenseMicroController, DebugService, DamageService);
            var miningTask = new MiningTask(SharkyUnitData, BaseData, ActiveUnitData, 1, miningDefenseService, MacroData, BuildOptions, MicroTaskData, new MineralMiner(this), new GasMiner(this));
            var queenInjectTask = new QueenInjectTask(this, 1.0f, queenMicroController);
            var queenCreepTask = new QueenCreepTask(this, 1.1f, queenMicroController);
            var queenDefendTask = new QueenDefendTask(this, 1.1f, queenMicroController);
            var burrowBlockExpansions = new BurrowBlockExpansionsTask(this, 0.9f, new IndividualMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false), SharkyUnitData);
            var creepTumorTask = new CreepTumorTask(this, queenCreepTask, 1, 1.11f);
            var attackTask = new AttackTask(MicroController, TargetingData, ActiveUnitData, DefenseService, MacroData, AttackData, TargetingService, MicroTaskData, SharkyUnitData, new ArmySplitter(AttackData, TargetingData, ActiveUnitData, DefenseService, TargetingService, TerranWallService, MicroController), new EnemyCleanupService(MicroController, DamageService), 2);
            var adeptWorkerHarassTask = new AdeptWorkerHarassTask(BaseData, TargetingData, adeptMicroController, adeptShadeMicroController, false);
            var oracleWorkerHarassTask = new OracleWorkerHarassTask(this, oracleHarassMicroController, 1, false);
            var lateGameOracleHarassTask = new LateGameOracleHarassTask(BaseData, TargetingData, MapDataService, oracleHarassMicroController, 1, false);
            var reaperWorkerHarassTask = new ReaperWorkerHarassTask(BaseData, TargetingData, reaperHarassMicroController, 2, false);
            var bansheeHarassTask = new BansheeHarassTask(BaseData, TargetingData, MapDataService, bansheeMicroController, 2, false);
            var hallucinationScoutTask = new HallucinationScoutTask(TargetingData, BaseData, false, .5f);
            var wallOffTask = new WallOffTask(SharkyUnitData, ActiveUnitData, MacroData, MapData, WallService, ChatService, false, .25f);
            var permanentWallOffTask = new PermanentWallOffTask(SharkyUnitData, ActiveUnitData, MacroData, MapData, WallService, ChatService, false, .25f);
            var fullPylonWallOffTask = new FullPylonWallOffTask(this, false, .25f);
            var destroyWallOffTask = new DestroyWallOffTask(ActiveUnitData, false, .25f);
            var prePositionBuilderTask = new PrePositionBuilderTask(this, .25f);
            var repairTask = new RepairTask(this, .6f, true);
            var saveLiftableBuildingTask = new SaveLiftableBuildingTask(this, BuildingPlacement, .6f, true);
            var hellbatMorphTask = new HellbatMorphTask(this, false, 0.5f);
            var nexusRecallTask = new NexusRecallTask(this, false, 0.5f);
            var forceFieldRampTask = new ForceFieldRampTask(TargetingData, ActiveUnitData, MapData, WallService, MapDataService, false, 0.5f);
            var denyExpansionsTask = new DenyExpansionsTask(this, false, 1.1f);
            var darkTemplarHarassTask = new DarkTemplarHarassTask(BaseData, TargetingData, MapDataService, darkTemplarMicroController, 2, false);
            var defensiveZealotWarpInTask = new DefensiveZealotWarpInTask(this, false, .5f);
            var reaperMiningDefenseTask = new ReaperMiningDefenseTask(this, true, .5f);
            var overlordScoutTask = new OverlordScoutTask(this, true, 0.9f);
            var overlordProxyScoutTask = new SecondaryOverlordScoutingTask(this, true, 1.0f, new IndividualMicroController(this, SharkySimplePathFinder, MicroPriority.StayOutOfRange, false));
            var zerglingScoutTask = new ZerglingScoutTask(this, false, 1.0f);
            var scoutForSpineTask = new ScoutForSpineTask(this, false, 0.5f);
            var protossDoorTask = new ProtossDoorTask(this, false, -0.5f);


            MicroTaskData.MicroTasks[defenseSquadTask.GetType().Name] = defenseSquadTask;
            MicroTaskData.MicroTasks[workerScoutGasStealTask.GetType().Name] = workerScoutGasStealTask;
            MicroTaskData.MicroTasks[workerScoutTask.GetType().Name] = workerScoutTask;
            MicroTaskData.MicroTasks[reaperScoutTask.GetType().Name] = reaperScoutTask;
            MicroTaskData.MicroTasks[findHiddenBaseTask.GetType().Name] = findHiddenBaseTask;
            MicroTaskData.MicroTasks[proxyScoutTask.GetType().Name] = proxyScoutTask;
            MicroTaskData.MicroTasks[miningTask.GetType().Name] = miningTask;
            MicroTaskData.MicroTasks[queenInjectTask.GetType().Name] = queenInjectTask;
            MicroTaskData.MicroTasks[queenCreepTask.GetType().Name] = queenCreepTask;
            MicroTaskData.MicroTasks[queenDefendTask.GetType().Name] = queenDefendTask;
            MicroTaskData.MicroTasks[creepTumorTask.GetType().Name] = creepTumorTask;
            MicroTaskData.MicroTasks[attackTask.GetType().Name] = attackTask;
            MicroTaskData.MicroTasks[adeptWorkerHarassTask.GetType().Name] = adeptWorkerHarassTask;
            MicroTaskData.MicroTasks[oracleWorkerHarassTask.GetType().Name] = oracleWorkerHarassTask;
            MicroTaskData.MicroTasks[lateGameOracleHarassTask.GetType().Name] = lateGameOracleHarassTask;
            MicroTaskData.MicroTasks[reaperWorkerHarassTask.GetType().Name] = reaperWorkerHarassTask;
            MicroTaskData.MicroTasks[bansheeHarassTask.GetType().Name] = bansheeHarassTask;
            MicroTaskData.MicroTasks[hallucinationScoutTask.GetType().Name] = hallucinationScoutTask;
            MicroTaskData.MicroTasks[wallOffTask.GetType().Name] = wallOffTask;
            MicroTaskData.MicroTasks[permanentWallOffTask.GetType().Name] = permanentWallOffTask;
            MicroTaskData.MicroTasks[fullPylonWallOffTask.GetType().Name] = fullPylonWallOffTask;
            MicroTaskData.MicroTasks[destroyWallOffTask.GetType().Name] = destroyWallOffTask;
            MicroTaskData.MicroTasks[prePositionBuilderTask.GetType().Name] = prePositionBuilderTask;
            MicroTaskData.MicroTasks[repairTask.GetType().Name] = repairTask;
            MicroTaskData.MicroTasks[saveLiftableBuildingTask.GetType().Name] = saveLiftableBuildingTask;
            MicroTaskData.MicroTasks[hellbatMorphTask.GetType().Name] = hellbatMorphTask;
            MicroTaskData.MicroTasks[nexusRecallTask.GetType().Name] = nexusRecallTask;
            MicroTaskData.MicroTasks[forceFieldRampTask.GetType().Name] = forceFieldRampTask;
            MicroTaskData.MicroTasks[denyExpansionsTask.GetType().Name] = denyExpansionsTask;
            MicroTaskData.MicroTasks[darkTemplarHarassTask.GetType().Name] = darkTemplarHarassTask;
            MicroTaskData.MicroTasks[defensiveZealotWarpInTask.GetType().Name] = defensiveZealotWarpInTask;
            MicroTaskData.MicroTasks[reaperMiningDefenseTask.GetType().Name] = reaperMiningDefenseTask;
            MicroTaskData.MicroTasks[overlordScoutTask.GetType().Name] = overlordScoutTask;
            MicroTaskData.MicroTasks[overlordProxyScoutTask.GetType().Name] = overlordProxyScoutTask;
            MicroTaskData.MicroTasks[zerglingScoutTask.GetType().Name] = zerglingScoutTask;
            MicroTaskData.MicroTasks[scoutForSpineTask.GetType().Name] = scoutForSpineTask;
            MicroTaskData.MicroTasks[burrowBlockExpansions.GetType().Name] = burrowBlockExpansions;
            MicroTaskData.MicroTasks[protossDoorTask.GetType().Name] = protossDoorTask;

            MicroManager = new MicroManager(ActiveUnitData, MicroTaskData, SharkyOptions);
            Managers.Add(MicroManager);

            AttackDataManager = new AttackDataManager(AttackData, ActiveUnitData, attackTask, TargetPriorityService, TargetingData, MacroData, BaseData, DebugService);
            Managers.Add(AttackDataManager);

            BuildProxyService = new BuildProxyService(MacroData, BuildingBuilder, SharkyUnitData, ActiveUnitData, Morpher, MicroTaskData);
            BuildAddOnSwapService = new BuildAddOnSwapService(MacroData, ActiveUnitData, SharkyUnitData, BuildingService, BuildingPlacement);
            BuildingCancelService = new BuildingCancelService(ActiveUnitData, MacroData);
            UpgradeRequestCancellingService = new UpgradeRequestCancellingService(this);
            BuildingRequestCancellingService = new BuildingRequestCancellingService(ActiveUnitData, MacroData, UnitCountService);
            VespeneGasBuilder = new VespeneGasBuilder(this, BuildingBuilder);
            UnitBuilder = new UnitBuilder(this, WarpInPlacement);
            UpgradeResearcher = new UpgradeResearcher(this);
            SupplyBuilder = new SupplyBuilder(this, BuildingBuilder);
            ProductionBuilder = new ProductionBuilder(this, BuildingBuilder);
            TechBuilder = new TechBuilder(this, BuildingBuilder);
            AddOnBuilder = new AddOnBuilder(this, BuildingBuilder);
            BuildingMorpher = new BuildingMorpher(this);
            UnfinishedBuildingCompleter = new UnfinishedBuildingCompleter(this);
            MacroManager = new MacroManager(this);
            Managers.Add(MacroManager);

            EnemyStrategyHistory = new EnemyStrategyHistory();
            EnemyData.EnemyStrategies = new Dictionary<string, IEnemyStrategy>
            {
                [nameof(EnemyStrategies.Proxy)] = new EnemyStrategies.Proxy(this),
                [nameof(WorkerRush)] = new WorkerRush(this),
                [nameof(InvisibleAttacks)] = new InvisibleAttacks(this),
                [nameof(Air)] = new Air(this),

                [nameof(OneBase)] = new OneBase(this),
                [nameof(TwoBase)] = new TwoBase(this),

                [nameof(AdeptRush)] = new AdeptRush(this),
                [nameof(CannonRush)] = new CannonRush(this),
                [nameof(EnemyStrategies.Protoss.FourGate)] = new EnemyStrategies.Protoss.FourGate(this),
                [nameof(ProtossFastExpand)] = new ProtossFastExpand(this),
                [nameof(ProxyRobo)] = new ProxyRobo(this),
                [nameof(ProxyStargate)] = new ProxyStargate(this),
                [nameof(ProxyShieldBattery)] = new ProxyShieldBattery(this),
                [nameof(ZealotRush)] = new ZealotRush(this),
                [nameof(FleetBeaconTech)] = new FleetBeaconTech(this),
                [nameof(AirToss)] = new AirToss(this),
                [nameof(FastForge)] = new FastForge(this),
                [nameof(FastStargate)] = new FastStargate(this),

                [nameof(MarineRush)] = new MarineRush(this),
                [nameof(BunkerRush)] = new BunkerRush(this),
                [nameof(BunkerContain)] = new BunkerContain(this),
                [nameof(MassVikings)] = new MassVikings(this),
                [nameof(ThreeRax)] = new ThreeRax(this),
                [nameof(BansheeRush)] = new BansheeRush(this),

                [nameof(ZerglingRush)] = new ZerglingRush(this),
                [nameof(RoachRavager)] = new RoachRavager(this),
                [nameof(ZerglingDroneRush)] = new ZerglingDroneRush(this),
                [nameof(RoachRush)] = new RoachRush(this),
                [nameof(MutaliskRush)] = new MutaliskRush(this),
                [nameof(BurrowStrategy)] = new BurrowStrategy(this),
                [nameof(BanelingDrops)] = new BanelingDrops(this),
                [nameof(NydusNetworkStrategy)] = new NydusNetworkStrategy(this),
                [nameof(MassQueen)] = new MassQueen(this),
            };

            EnemyStrategyManager = new EnemyStrategyManager(this);
            Managers.Add(EnemyStrategyManager);

            EmptyCounterTransitioner = new EmptyCounterTransitioner();

            var antiMassMarine = new AntiMassMarine(this, EmptyCounterTransitioner);
            var fourGate = new Builds.Protoss.FourGate(this, EmptyCounterTransitioner);
            var nexusFirst = new NexusFirst(this, EmptyCounterTransitioner);
            var robo = new Robo(this, EmptyCounterTransitioner);
            var protossRobo = new ProtossRobo(this, EmptyCounterTransitioner);
            var everyProtossUnit = new EveryProtossUnit(this, EmptyCounterTransitioner);

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

            var massMarine = new MassMarines(this);
            var battleCruisers = new BattleCruisers(this);
            var everyTerranUnit = new EveryTerranUnit(this);
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

            var basicZerglingRush = new BasicZerglingRush(this);
            var everyZergUnit = new EveryZergUnit(this);
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

            MacroBalancer = new MacroBalancer(BuildOptions, ActiveUnitData, MacroData, SharkyUnitData, BaseData, UnitCountService);
            BuildChoices = new Dictionary<Race, BuildChoices>
            {
                { Race.Protoss, new BuildChoices { Builds = protossBuilds, BuildSequences = protossBuildSequences } },
                { Race.Terran, new BuildChoices { Builds = terranBuilds, BuildSequences = terranBuildSequences } },
                { Race.Zerg, new BuildChoices { Builds = zergBuilds, BuildSequences = zergBuildSequences } }
            };
            BuildMatcher = new BuildMatcher();
            RecordService = new RecordService(BuildMatcher);
            BuildDecisionService = new RecentBuildDecisionService(this);
            BuildManager = new BuildManager(this);
            Managers.Add(BuildManager);
        }
        public SharkyBot CreateBot(List<IManager> managers, DebugService debugService)
        {
            return new SharkyBot(managers, debugService, FrameToTimeConverter, SharkyOptions, PerformanceData);
        }
    }
}
