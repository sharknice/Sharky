using SC2APIProtocol;
using Sharky;
using Sharky.Managers;
using Sharky.MicroControllers;
using Sharky.MicroTasks;
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

            double framesPerSecond = 22.4;

            GameConnection gameConnection = new GameConnection();

            var sharkyOptions = new SharkyOptions { Debug = debug };

            var managers = new List<IManager>();

            var debugManager = new DebugManager(gameConnection, sharkyOptions);
            managers.Add(debugManager);
            var unitManager = new UnitManager();
            managers.Add(unitManager);

            var microTasks = new List<IMicroTask>();
            microTasks.Add(new AttackTask(new MicroController()));
            var microManager = new MicroManager(unitManager, microTasks);
            managers.Add(microManager);

            var sharkyBot = new SharkyBot(managers);

            

            if (args.Length == 0)
            {
                gameConnection.RunSinglePlayer(sharkyBot, @"DeathAuraLE.SC2Map", Race.Protoss, Race.Terran, Difficulty.VeryHard).Wait();
            }
            else
            {
                gameConnection.RunLadder(sharkyBot, Race.Protoss, args).Wait();
            }
        }
    }
}
