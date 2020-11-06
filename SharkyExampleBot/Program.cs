using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildingPlacement;
using Sharky.Builds.Protoss;
using Sharky.Managers;
using Sharky.MicroControllers;
using Sharky.MicroTasks;
using Sharky.Pathing;
using System.Collections.Generic;

namespace SharkyExampleBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            var debug = false;
#if DEBUG
            debug = true;
#endif

            var framesPerSecond = 22.4f;

            GameConnection gameConnection = new GameConnection();

            var sharkyOptions = new SharkyOptions { Debug = debug, FramesPerSecond = framesPerSecond };

            var managers = new List<IManager>();

            var debugManager = new DebugManager(gameConnection, sharkyOptions);
            managers.Add(debugManager);
            var unitDataManager = new UnitDataManager();
            managers.Add(unitDataManager);
            var mapData = new MapData();
            var mapManager = new MapManager(mapData);
            managers.Add(mapManager);

            var unitManager = new UnitManager(unitDataManager, sharkyOptions);
            managers.Add(unitManager);

            var targetingManager = new TargetingManager();
            managers.Add(targetingManager);

            var buildOptions = new BuildOptions { StrictGasCount = false, StrictSupplyCount = false, StrictWorkerCount = false };
            var macroSetup = new MacroSetup();
            var protossBuildingPlacement = new ProtossBuildingPlacement(unitManager, unitDataManager, debugManager, mapData);
            var buildingPlacement = new BuildingPlacement(protossBuildingPlacement);
            var buildingBuilder = new BuildingBuilder(unitManager, targetingManager, buildingPlacement, unitDataManager);
            var baseManager = new BaseManager(unitDataManager);
            managers.Add(baseManager);

            var attackData = new AttackData();
            var warpInPlacement = new WarpInPlacement(unitManager, debugManager, mapData);
            var macroData = new MacroData();
            var macroManager = new MacroManager(macroSetup, unitManager, unitDataManager, buildingBuilder, sharkyOptions, baseManager, targetingManager, attackData, warpInPlacement, macroData);
            managers.Add(macroManager);
            
            var builds = new Dictionary<string, ISharkyBuild>();
            var antiMassMarine = new AntiMassMarine(buildOptions, macroData, unitManager);
            var sequences = new List<List<string>>();
            sequences.Add( new List<string> { antiMassMarine.Name() });
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

            var microTasks = new List<IMicroTask>
            {
                new AttackTask(new MicroController(), targetingManager, macroData, attackData),
                new MiningTask(unitDataManager, baseManager, unitManager)
            };
            var microManager = new MicroManager(unitManager, microTasks);
            managers.Add(microManager);

            var sharkyBot = new SharkyBot(managers, debugManager);

            var myRace = Race.Protoss;
            if (args.Length == 0)
            {
                gameConnection.RunSinglePlayer(sharkyBot, @"DeathAuraLE.SC2Map", myRace, Race.Terran, Difficulty.VeryHard).Wait();
            }
            else
            {
                gameConnection.RunLadder(sharkyBot, myRace, args).Wait();
            }
        }
    }
}
