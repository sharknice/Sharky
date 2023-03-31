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
                if (fileName.EndsWith(".zip"))
                {
                    using (var file = File.OpenRead(fileName))
                    using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            using (var stream = new StreamReader(entry.Open()))
                            {
                                var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                                var pathData = (List<PathData>)serializer.Deserialize(stream, typeof(List<PathData>));
                                dictionary[Path.GetFileNameWithoutExtension(fileName)] = pathData;
                            }
                        }
                    }
                }
                else
                {
                    using (StreamReader file = File.OpenText(fileName))
                    {
                        var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                        var pathData = (List<PathData>)serializer.Deserialize(file, typeof(List<PathData>));
                        dictionary[Path.GetFileNameWithoutExtension(fileName)] = pathData;
                    }
                }
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
