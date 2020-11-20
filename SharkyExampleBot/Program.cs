using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.Builds.BuildingPlacement;
using Sharky.Builds.Protoss;
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
                gameConnection.RunSinglePlayer(sharkyBot, @"AutomatonLE.SC2Map", myRace, Race.Random, Difficulty.VeryHard).Wait();
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

            var enemyRaceManager = new EnemyRaceManager(unitManager, unitDataManager);
            managers.Add(enemyRaceManager);

            var baseManager = new BaseManager(unitDataManager, unitManager);
            managers.Add(baseManager);

            var targetingManager = new TargetingManager(unitManager, unitDataManager, mapDataService, baseManager);
            managers.Add(targetingManager);

            var buildOptions = new BuildOptions { StrictGasCount = false, StrictSupplyCount = false, StrictWorkerCount = false };
            var macroSetup = new MacroSetup();
            var buildingService = new BuildingService(mapData, unitManager);
            var protossBuildingPlacement = new ProtossBuildingPlacement(unitManager, unitDataManager, debugManager, mapData, buildingService);
            var buildingPlacement = new BuildingPlacement(protossBuildingPlacement, baseManager, unitManager, buildingService);
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
            var enemyNameService = new EnemyNameService();
            var enemyPlayerService = new EnemyPlayerService(enemyNameService);
            var chatManager = new ChatManager(httpClient, chatHistory, sharkyOptions, chatDataService, enemyPlayerService, enemyNameService);
            managers.Add(chatManager);

            var sharkyPathFinder = new SharkyPathFinder(new Roy_T.AStar.Paths.PathFinder(), mapData, mapDataService);
            var sharkySimplePathFinder = new SharkySimplePathFinder(mapDataService);
            var noPathFinder = new SharkyNoPathFinder();

            var individualMicroController = new IndividualMicroController(mapDataService, unitDataManager, unitManager, debugManager, noPathFinder, sharkyOptions, MicroPriority.LiveAndAttack, true);
        
            var zealotMicroController = new ZealotMicroController(mapDataService, unitDataManager, unitManager, debugManager, noPathFinder, sharkyOptions, MicroPriority.AttackForward, true);
            var sentryMicroController = new SentryMicroController(mapDataService, unitDataManager, unitManager, debugManager, noPathFinder, sharkyOptions, MicroPriority.StayOutOfRange, true);
            var observerMicroController = new IndividualMicroController(mapDataService, unitDataManager, unitManager, debugManager, noPathFinder, sharkyOptions, MicroPriority.StayOutOfRange, true);
            var individualMicroControllers = new Dictionary<UnitTypes, IIndividualMicroController>
            {
                { UnitTypes.PROTOSS_ZEALOT, zealotMicroController },
                { UnitTypes.PROTOSS_SENTRY, sentryMicroController },
                { UnitTypes.PROTOSS_OBSERVER, observerMicroController }
            };

            var defenseService = new DefenseService(unitManager);
            var microController = new MicroController(individualMicroControllers, individualMicroController);

            var defenseSquadTask = new DefenseSquadTask(unitManager, targetingManager, defenseService, microController, new List<DesiredUnitsClaim>(), 0, false);
            var workerScoutTask = new WorkerScoutTask(unitDataManager, targetingManager, mapDataService, true, 0.5f);
            var miningTask = new MiningTask(unitDataManager, baseManager, unitManager, 1);          
            var attackTask = new AttackTask(microController, targetingManager, unitManager, defenseService, macroData, attackData, 2);

            var microTasks = new Dictionary<string, IMicroTask>
            {
                [defenseSquadTask.GetType().Name] = defenseSquadTask,
                [workerScoutTask.GetType().Name] = workerScoutTask,
                [miningTask.GetType().Name] = miningTask,
                [attackTask.GetType().Name] = attackTask
            };

            var microManager = new MicroManager(unitManager, microTasks);
            managers.Add(microManager);

            var enemyStrategyHistory = new EnemyStrategyHistory();
            var enemyStrategies = new Dictionary<string, IEnemyStrategy>
            {
                ["Proxy"] = new Proxy(enemyStrategyHistory, chatManager, unitManager, sharkyOptions, targetingManager),
                ["WorkerRush"] = new WorkerRush(enemyStrategyHistory, chatManager, unitManager, sharkyOptions, targetingManager),
                ["AdeptRush"] = new AdeptRush(enemyStrategyHistory, chatManager, unitManager, sharkyOptions),
                ["MarineRush"] = new MarineRush(enemyStrategyHistory, chatManager, unitManager, sharkyOptions),
                ["MassVikings"] = new MassVikings(enemyStrategyHistory, chatManager, unitManager, sharkyOptions),
                ["ZerglingRush"] = new ZerglingRush(enemyStrategyHistory, chatManager, unitManager, sharkyOptions)
            };

            var enemyStrategyManager = new EnemyStrategyManager(enemyStrategies);
            managers.Add(enemyStrategyManager);

            var protossCounterTransitioner = new ProtossCounterTransitioner(enemyStrategyManager, sharkyOptions);

            var antiMassMarine = new AntiMassMarine(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager);
            var fourGate = new FourGate(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager, unitDataManager);
            var nexusFirst = new NexusFirst(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager, protossCounterTransitioner);
            var robo = new Robo(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager);
            var protossRobo = new ProtossRobo(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager, sharkyOptions, microManager, enemyRaceManager);

            var builds = new Dictionary<string, ISharkyBuild>
            {
                [nexusFirst.Name()] = nexusFirst,
                [robo.Name()] = robo,
                [protossRobo.Name()] = protossRobo,
                [fourGate.Name()] = fourGate,
                [antiMassMarine.Name()] = antiMassMarine
            };

            var sequences = new List<List<string>>
            {
                new List<string> { nexusFirst.Name(), robo.Name(), protossRobo.Name() },
                new List<string> { antiMassMarine.Name() },
                new List<string> { fourGate.Name() }
            };

            var buildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = sequences,
                [Race.Zerg.ToString()] = sequences,
                [Race.Protoss.ToString()] = sequences,
                [Race.Random.ToString()] = sequences,
                ["Transition"] = sequences
            };

            var macroBalancer = new MacroBalancer(buildOptions, unitManager, macroData, unitDataManager);
            var buildChoices = new Dictionary<Race, BuildChoices>
            {
                { Race.Protoss, new BuildChoices { Builds = builds, BuildSequences = buildSequences } }
            };
            var buildDecisionService = new BuildDecisionService(chatManager);
            var buildManager = new BuildManager(buildChoices, debugManager, macroBalancer, buildDecisionService, enemyPlayerService, chatHistory, enemyStrategyHistory);
            managers.Add(buildManager);

            return new SharkyBot(managers, debugManager);
        }
    }
}
