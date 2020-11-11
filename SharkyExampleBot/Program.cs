using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildingPlacement;
using Sharky.Builds.Protoss;
using Sharky.Chat;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using Sharky.MicroControllers;
using Sharky.MicroTasks;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Net.Http;

namespace SharkyExampleBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            var gameConnection = new GameConnection();
            var sharkyBot = CreateBot(gameConnection);

            var myRace = Race.Protoss;
            if (args.Length == 0)
            {
                gameConnection.RunSinglePlayer(sharkyBot, @"DeathAuraLE.SC2Map", myRace, Race.Zerg, Difficulty.VeryHard).Wait();
            }
            else
            {
                gameConnection.RunLadder(sharkyBot, myRace, args).Wait();
            }
        }

        // TODO: defaultBot where you just pass in builds

        private static SharkyBot CreateBot(GameConnection gameConnection)
        {
            var debug = false;
#if DEBUG
            debug = true;
#endif

            var framesPerSecond = 22.4f;

            var sharkyOptions = new SharkyOptions { Debug = debug, FramesPerSecond = framesPerSecond };

            var managers = new List<IManager>();

            var debugManager = new DebugManager(gameConnection, sharkyOptions);
            managers.Add(debugManager);
            var unitDataManager = new UnitDataManager();
            managers.Add(unitDataManager);
            var mapData = new MapData();
            var mapManager = new MapManager(mapData);
            managers.Add(mapManager);

            var mapDataService = new MapDataService(mapData);

            var targetPriorityService = new TargetPriorityService(unitDataManager);
            var collisionCalculator = new CollisionCalculator();
            var unitManager = new UnitManager(unitDataManager, sharkyOptions, targetPriorityService, collisionCalculator, mapDataService);
            managers.Add(unitManager);

            var baseManager = new BaseManager(unitDataManager);
            managers.Add(baseManager);

            var targetingManager = new TargetingManager(unitManager, unitDataManager, mapDataService, baseManager);
            managers.Add(targetingManager);

            var buildOptions = new BuildOptions { StrictGasCount = false, StrictSupplyCount = false, StrictWorkerCount = false };
            var macroSetup = new MacroSetup();
            var protossBuildingPlacement = new ProtossBuildingPlacement(unitManager, unitDataManager, debugManager, mapData);
            var buildingPlacement = new BuildingPlacement(protossBuildingPlacement);
            var buildingBuilder = new BuildingBuilder(unitManager, targetingManager, buildingPlacement, unitDataManager);


            var attackData = new AttackData { ArmyFoodAttack = 30, Attacking = false, CustomAttackFunction = false };
            var warpInPlacement = new WarpInPlacement(unitManager, debugManager, mapData);
            var macroData = new MacroData();
            var macroManager = new MacroManager(macroSetup, unitManager, unitDataManager, buildingBuilder, sharkyOptions, baseManager, targetingManager, attackData, warpInPlacement, macroData);
            managers.Add(macroManager);

            var nexusManager = new NexusManager(unitManager, unitDataManager);
            managers.Add(nexusManager);

            var httpClient = new HttpClient();
            var chatHistory = new ChatHistory();
            var chatDataService = new ChatDataService();
            var chatManager = new ChatManager(httpClient, chatHistory, sharkyOptions, chatDataService);
            managers.Add(chatManager);

            var builds = new Dictionary<string, ISharkyBuild>();
            var antiMassMarine = new AntiMassMarine(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager);
            var fourGate = new FourGate(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager, unitDataManager);
            var sequences = new List<List<string>>();
            sequences.Add(new List<string> { fourGate.Name(), antiMassMarine.Name() });
            builds[fourGate.Name()] = fourGate;
            builds[antiMassMarine.Name()] = antiMassMarine;
            var buildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = sequences,
                [Race.Zerg.ToString()] = sequences,
                [Race.Protoss.ToString()] = sequences,
                [Race.Random.ToString()] = sequences,
                ["transition"] = sequences
            };

            var macroBalancer = new MacroBalancer(buildOptions, unitManager, macroData, unitDataManager);
            var buildChoices = new BuildChoices { Builds = builds, BuildSequences = buildSequences };
            var buildManager = new BuildManager(macroManager, buildChoices, debugManager, macroBalancer);
            managers.Add(buildManager);

            var sharkyPathFinder = new SharkyPathFinder(new Roy_T.AStar.Paths.PathFinder(), mapData, mapDataService);

            var individualMicroControllers = new Dictionary<UnitTypes, IIndividualMicroController>();
            var individualMicroController = new IndividualMicroController(mapDataService, unitDataManager, unitManager, debugManager, sharkyPathFinder, sharkyOptions, MicroPriority.LiveAndAttack, true);
            var microTasks = new List<IMicroTask>
            {
                new AttackTask(new MicroController(individualMicroControllers, individualMicroController), targetingManager, macroData, attackData),
                new MiningTask(unitDataManager, baseManager, unitManager)
            };
            var microManager = new MicroManager(unitManager, microTasks);
            managers.Add(microManager);

            return new SharkyBot(managers, debugManager);
        }
    }
}
