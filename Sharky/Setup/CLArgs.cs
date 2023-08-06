namespace Sharky
{
    /* This class is used to load the CommandLine arguments for the bot.
     */
    public class CLArgs
    {
        private int gamePort;
        private int startPort;
        private string ladderServer = "127.0.0.1";
        private Race computerRace = Race.NoRace;
        private Difficulty computerDifficulty = Difficulty.VeryHard;

        public CLArgs(string[] args)
        {
            for (int i = 0; i < args.Count(); i += 2)
            {
                if (args[i] == "-g" || args[i] == "--GamePort")
                {
                    gamePort = int.Parse(args[i + 1]);
                }
                else if (args[i] == "-o" || args[i] == "--StartPort")
                {
                    startPort = int.Parse(args[i + 1]);
                }
                else if (args[i] == "-l" || args[i] == "--LadderServer")
                {
                    ladderServer = args[i + 1];
                }
                else if (args[i] == "--OpponentId")
                {
                    OpponentID = args[i + 1];
                }
                else if (args[i] == "-c" || args[i] == "--ComputerOpponent")
                {
                    if (computerRace == Race.NoRace)
                    {
                        computerRace = Race.Random;
                    }
                    computerDifficulty = Difficulty.VeryHard;
                    i--;
                }
                else if (args[i] == "-a" || args[i] == "--ComputerRace")
                {
                    if (args[i + 1] == "Protoss")
                    {
                        computerRace = Race.Protoss;
                    }
                    else if (args[i + 1] == "Terran")
                    {
                        computerRace = Race.Terran;
                    }
                    else if (args[i + 1] == "Zerg")
                    {
                        computerRace = Race.Zerg;
                    }
                    else if (args[i + 1] == "Random")
                    {
                        computerRace = Race.Random;
                    }
                }
                else if (args[i] == "-d" || args[i] == "--ComputerDifficulty")
                {
                    if (args[i + 1] == "VeryEasy")
                    {
                        computerDifficulty = Difficulty.VeryEasy;
                    }
                    if (args[i + 1] == "Easy")
                    {
                        computerDifficulty = Difficulty.Easy;
                    }
                    if (args[i + 1] == "Medium")
                    {
                        computerDifficulty = Difficulty.Medium;
                    }
                    if (args[i + 1] == "MediumHard")
                    {
                        computerDifficulty = Difficulty.MediumHard;
                    }
                    if (args[i + 1] == "Hard")
                    {
                        computerDifficulty = Difficulty.Hard;
                    }
                    if (args[i + 1] == "Harder")
                    {
                        computerDifficulty = Difficulty.Harder;
                    }
                    if (args[i + 1] == "VeryHard")
                    {
                        computerDifficulty = Difficulty.VeryHard;
                    }
                    if (args[i + 1] == "CheatVision")
                    {
                        computerDifficulty = Difficulty.CheatVision;
                    }
                    if (args[i + 1] == "CheatMoney")
                    {
                        computerDifficulty = Difficulty.CheatMoney;
                    }
                    if (args[i + 1] == "CheatInsane")
                    {
                        computerDifficulty = Difficulty.CheatInsane;
                    }

                    computerDifficulty = Difficulty.Easy;
                }
            }
        }

        public int GamePort { get => gamePort; set => gamePort = value; }
        public int StartPort { get => startPort; set => startPort = value; }
        public string LadderServer { get => ladderServer; set => ladderServer = value; }
        public Race ComputerRace { get => computerRace; set => computerRace = value; }
        public Difficulty ComputerDifficulty { get => computerDifficulty; set => computerDifficulty = value; }
        public string OpponentID { get; set; }
    }
}
