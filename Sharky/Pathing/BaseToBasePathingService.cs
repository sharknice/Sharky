using Newtonsoft.Json;
using Sharky.DefaultBot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sharky.Pathing
{
    public class BaseToBasePathingService
    {
        MapData MapData;
        SharkyOptions SharkyOptions;
        IPathFinder PathFinder;

        public BaseToBasePathingService(DefaultSharkyBot defaultSharkyBot)
        {
            MapData = defaultSharkyBot.MapData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;

            PathFinder = defaultSharkyBot.SharkyPathFinder;
        }

        private List<PathData> LoadGeneratedMapPathData(string mapName)
        {
            string[] folders = new string[]
            {
                GetStaticPathingDataFolder(),
                GetGeneratedPathingDataFolder()
            };
            foreach (var folder in folders)
            {
                string fileName = Path.Combine(folder, mapName);
                if (File.Exists(fileName))
                {
                    return LoadPathDataJson(fileName);
                }
                fileName = Path.Combine(folder, mapName + ".json");
                if (File.Exists(fileName))
                {
                    return LoadPathDataJson(fileName);
                }
                fileName = Path.Combine(folder, mapName + ".zip");
                if (File.Exists(fileName))
                {
                    return LoadPathDataZip(fileName);
                }
            }
            return null;
        }

        private static List<PathData> LoadPathDataZip(string fileName)
        {
            using (var file = File.OpenRead(fileName))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    using (var stream = new StreamReader(entry.Open()))
                    {
                        var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                        var pathData = serializer.Deserialize<List<PathData>>(new JsonTextReader(stream));
                        return pathData;
                    }
                }
            }
            return null;
        }

        private static List<PathData> LoadPathDataJson(string fileName)
        {
            using (StreamReader file = File.OpenText(fileName))
            {
                var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                var pathData = serializer.Deserialize<List<PathData>>(new JsonTextReader(file));
                return pathData;
            }
        }

        private string GetStaticPathingDataFolder()
        {
            return Directory.GetCurrentDirectory() + "/StaticData/pathing/";
        }

        private string GetGeneratedPathingDataFolder()
        {
            return Directory.GetCurrentDirectory() + "/data/pathing/";
        }

        private static string GetGeneratedPathDataFileName(string map, string pathingFolder)
        {
            return $"{pathingFolder}{map}.json";
        }

        public List<PathData> GetBaseToBasePathingData(string mapName)
        {
            var pathData = LoadGeneratedMapPathData(mapName);
            if (pathData != null)
            {
                return pathData;
            }

            if (!SharkyOptions.GeneratePathing)
            {
                return new List<PathData>();
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Calculating pathing data");
            pathData = CalcultedPathData();
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
            var pathData = new ConcurrentBag<PathData>();

            var numbers = new List<int>();
            for (int x = 5; x <= MapData.MapWidth - 5; x += 5)
            {
                numbers.Add(x);
            }

            Parallel.ForEach(numbers, x =>
                   {
                       var pathFinder = new Roy_T.AStar.Paths.PathFinder();
                       for (int y = 5; y <= MapData.MapHeight - 5; y += 5)
                       {
                           var start = new Vector2(x, y);

                           for (int endX = 5; endX <= MapData.MapWidth - 10; endX += 10)
                           {
                               for (int endY = 5; endY <= MapData.MapHeight - 10; endY += 10)
                               {
                                   var end = new Vector2(endX, endY);
                                   if (end.X == start.X && end.Y == start.Y) { continue; }

                                   var path = PathFinder.GetGroundPath(start.X, start.Y, end.X, end.Y, 0, pathFinder);
                                   if (path.Count == 0) { continue; }
                                   var data = new PathData { StartPosition = start, EndPosition = end, Path = path };
                                   pathData.Add(data);
                               }
                           }
                       }
                   });
            return pathData.ToList();
        }
    }
}
