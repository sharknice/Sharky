using Newtonsoft.Json;
using Sharky.DefaultBot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace Sharky.Pathing
{
    public class BaseToBasePathingService
    {
        MapData MapData;
        SharkyOptions SharkyOptions;
        IPathFinder PathFinder;

        Dictionary<string, List<PathData>> GeneratedPathData;

        public BaseToBasePathingService(DefaultSharkyBot defaultSharkyBot)
        {
            MapData = defaultSharkyBot.MapData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;

            PathFinder = defaultSharkyBot.SharkyPathFinder;
            GeneratedPathData = LoadGeneratedMapPathData();
        }

        private Dictionary<string, List<PathData>> LoadGeneratedMapPathData()
        {
            var dictionary = new Dictionary<string, List<PathData>>();

            var folder = GetStaticPathingDataFolder();
            LoadPathData(dictionary, folder);

            folder = GetGeneratedPathingDataFolder();
            Directory.CreateDirectory(folder);
            LoadPathData(dictionary, folder);
            return dictionary;
        }

        private static void LoadPathData(Dictionary<string, List<PathData>> dictionary, string folder)
        {
            foreach (var fileName in Directory.GetFiles(folder))
            {
                using (StreamReader file = File.OpenText(fileName))
                {
                    var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                    var pathData = (List<PathData>)serializer.Deserialize(file, typeof(List<PathData>));
                    dictionary[Path.GetFileNameWithoutExtension(fileName)] = pathData;
                }
            }
        }

        private string GetStaticPathingDataFolder()
        {
            return Directory.GetCurrentDirectory() + "/StaticData/pathing/";
        }

        private string GetGeneratedPathingDataFolder()
        {
            return Directory.GetCurrentDirectory() + "/Data/pathing/";
        }

        private static string GetGeneratedPathDataFileName(string map, string pathingFolder)
        {
            return $"{pathingFolder}{map}.json";
        }

        public List<PathData> GetBaseToBasePathingData(string mapName)
        {
            if (GeneratedPathData.ContainsKey(mapName))
            {
                return GeneratedPathData[mapName];
            }

            if (!SharkyOptions.GeneratePathing)
            {
                return new List<PathData>();
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Calculating pathing data");
            var pathData = CalcultedPathData();
            stopwatch.Stop();
            Console.WriteLine($"Calculated pathing data in {stopwatch.ElapsedMilliseconds} ms");
            SaveGeneratedMapPathData(mapName, pathData);
            return pathData;
        }

        public void SaveGeneratedMapPathData(string map, List<PathData> pathData)
        {
            var pathingFolder = GetGeneratedPathingDataFolder();
            var fileName = GetGeneratedPathDataFileName(map, pathingFolder);
            Console.WriteLine($"Saving pathing data to {fileName}");

            Directory.CreateDirectory(pathingFolder);
            string json = JsonConvert.SerializeObject(pathData, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });
            File.WriteAllText(fileName, json, Encoding.UTF8);
        }

        public List<PathData> CalcultedPathData()
        {
            var pathData = new List<PathData>();

            for (int x = 10; x <= MapData.MapWidth - 10; x += 10)
            {
                for (int y = 10; y <= MapData.MapHeight - 10; y += 10)
                {
                    var start = new Vector2(x, y);

                    for (int endX = 5; endX <= MapData.MapWidth - 10; endX += 10)
                    {
                        for (int endY = 5; endY <= MapData.MapHeight - 10; endY += 10)
                        {
                            var end = new Vector2(endX, endY);
                            if (end.X == start.X && end.Y == start.Y) { continue; }

                            var path = PathFinder.GetGroundPath(start.X, start.Y, end.X, end.Y, 0);
                            if (path.Count == 0) { continue; }
                            var data = new PathData { StartPosition = start, EndPosition = end, Path = path };
                            pathData.Add(data);
                        }
                    }
                }
            }

            return pathData;
        }
    }
}
