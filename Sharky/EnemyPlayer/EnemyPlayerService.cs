using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sharky.EnemyPlayer
{
    public class EnemyPlayerService : IEnemyPlayerService
    {
        EnemyNameService EnemyNameService;

        public List<EnemyPlayer> Enemies { get; private set; }
        List<Game> Games { get; set; }
        public Tournament Tournament { get; private set; }
        string DataFolder { get; set; }

        public EnemyPlayerService(EnemyNameService enemyNameService)
        {
            EnemyNameService = enemyNameService;

            DataFolder = Directory.GetCurrentDirectory() + "/data/";

            Tournament = LoadTournament();
            if (Tournament.Enabled)
            {
                DataFolder += Tournament.Folder + "/";
                if (!Directory.Exists(DataFolder))
                {
                    Directory.CreateDirectory(DataFolder);
                }
            }

            Enemies = LoadEnemies();
            var games = LoadGames();
            GetNames(games);
            AssignGames(games);
        }

        private void AssignGames(List<Game> games)
        {
            foreach (var game in games.OrderByDescending(g => g.DateTime))
            {
                var enemy = Enemies.FirstOrDefault(e => e.Id == game.EnemyId);
                if (enemy == null)
                {
                    Enemies.Add(new EnemyPlayer { Id = game.EnemyId, Games = new List<Game>(), ChatMatches = new List<string>() });
                    enemy = Enemies.FirstOrDefault(e => e.Id == game.EnemyId);
                }

                if (enemy.Games == null)
                {
                    enemy.Games = new List<Game>();
                }

                enemy.Games.Add(game);
            }
        }

        private void GetNames(List<Game> games)
        {
            foreach (var game in games)
            {
                if (Enemies.Any(e => e.Id == game.EnemyId))
                {
                    continue;
                }

                var name = EnemyNameService.GetNameFromGame(game, Enemies);
                if (name != string.Empty)
                {
                    var enemy = Enemies.FirstOrDefault(e => e.Name == name);
                    if (enemy != null)
                    {
                        enemy.Id = game.EnemyId;
                    }
                }
            }
        }

        private List<EnemyPlayer> LoadEnemies()
        {
            Enemies = LoadEnemies("StaticData/opponents/");
            return Enemies;
        }

        private List<Game> LoadGames()
        {
            Games = LoadGames(DataFolder + "games/");
            return Games;
        }

        private Tournament LoadTournament()
        {
            var tournament = new Tournament { Enabled = false };
            var tournamentFile = DataFolder + "Tournament.json";
            if (File.Exists(tournamentFile))
            {
                using (StreamReader file = File.OpenText(tournamentFile))
                {
                    var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                    tournament = (Tournament)serializer.Deserialize(file, typeof(Tournament));
                }
            }
            return tournament;
        }

        /// <summary>
        /// Load the enemies from json files
        /// </summary>
        private List<EnemyPlayer> LoadEnemies(string enemyFolder)
        {
            var enemies = new List<EnemyPlayer>();
            if (Directory.Exists(enemyFolder))
            {
                foreach (var fileName in Directory.GetFiles(enemyFolder))
                {
                    using (StreamReader file = File.OpenText(fileName))
                    {
                        var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                        var enemy = (EnemyPlayer)serializer.Deserialize(file, typeof(EnemyPlayer));
                        if (enemy.Games == null) { enemy.Games = new List<Game>(); }
                        if (enemy.ChatMatches == null) { enemy.ChatMatches = new List<string>(); }
                        enemies.Add(enemy);
                    }
                }
            }
            return enemies;
        }

        /// <summary>
        /// load all the games played from json files
        /// </summary>
        private List<Game> LoadGames(string gameFolder)
        {
            var games = new List<Game>();
            if (Directory.Exists(gameFolder))
            {
                foreach (var fileName in Directory.GetFiles(gameFolder))
                {
                    using (StreamReader file = File.OpenText(fileName))
                    {
                        var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                        var enemy = (Game)serializer.Deserialize(file, typeof(Game));
                        games.Add(enemy);
                    }
                }
            }
            return games;
        }

        /// <summary>
        /// save a game when the match ends, playerid, name, map, positions, the strategies and timestamps, chat log
        /// </summary>
        /// <param name="processedGame"></param>
        public void SaveGame(Game game)
        {
            string json = JsonConvert.SerializeObject(game, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });
            if (!Directory.Exists(DataFolder + "/games/"))
            {
                Directory.CreateDirectory(DataFolder + "/games/");
            }
            File.WriteAllText(DataFolder + "/games/" + DateTimeOffset.Now.ToUnixTimeMilliseconds() + ".json", json, Encoding.UTF8);
        }
    }
}
