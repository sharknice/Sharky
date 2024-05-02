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
            var simplifiedName = mapName.ToLower().Replace(" ", "");
            string[] folders = new string[]
            {
                GetCustomStaticPathingDataFolder(),
                GetStaticPathingDataFolder(),
                GetGeneratedPathingDataFolder()
            };
            foreach (var folder in folders)
            {
                string fileName = FilePath.Combine(folder, mapName);
                if (File.Exists(fileName))
                {
                    return LoadPathDataJson(fileName);
                }
                fileName = FilePath.Combine(folder, mapName + ".json");
                if (File.Exists(fileName))
                {
                    return LoadPathDataJson(fileName);
                }
                fileName = FilePath.Combine(folder, mapName + ".zip");
                if (File.Exists(fileName))
                {
                    return LoadPathDataZip(fileName);
                }

                if (Directory.Exists(folder))
                {
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        var simplifiedFileName = FilePath.GetFileName(file).ToLower().Replace(" ", "");
                        if (simplifiedName == simplifiedFileName)
                        {
                            return LoadPathDataJson(file);
                        }
                        if (simplifiedName + ".json" == simplifiedFileName)
                        {
                            return LoadPathDataJson(file);
                        }
                        if (simplifiedName + ".zip" == simplifiedFileName)
                        {
                            return LoadPathDataZip(file);
                        }

                        simplifiedFileName = simplifiedFileName.Replace(".zip", "").Replace(".json", "");
                        if (mapName.Replace(" ", "").ToLower().Contains(simplifiedFileName))
                        {
                            if (file.EndsWith(".json"))
                            {
                                return LoadPathDataJson(file);
                            }
                            else if (file.EndsWith(".zip"))
                            {
                                return LoadPathDataZip(file);
                            }
                        }
                    }
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

        private string GetCustomStaticPathingDataFolder()
        {
            return Directory.GetCurrentDirectory() + "/StaticData/pathing/custom/";
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
            for (int x = SharkyOptions.GeneratePathingPrecision.Item1; x <= MapData.MapWidth - SharkyOptions.GeneratePathingPrecision.Item1; x += SharkyOptions.GeneratePathingPrecision.Item1)
            {
                numbers.Add(x);
            }

            foreach (int x in numbers)
            {
                var pathFinder = new Roy_T.AStar.Paths.PathFinder();
                for (int y = SharkyOptions.GeneratePathingPrecision.Item1; y <= MapData.MapHeight - SharkyOptions.GeneratePathingPrecision.Item1; y += SharkyOptions.GeneratePathingPrecision.Item1)
                {
                    var start = new Vector2(x, y);

                    for (int endX = SharkyOptions.GeneratePathingPrecision.Item1; endX <= MapData.MapWidth - SharkyOptions.GeneratePathingPrecision.Item2; endX += SharkyOptions.GeneratePathingPrecision.Item2)
                    {
                        for (int endY = SharkyOptions.GeneratePathingPrecision.Item1; endY <= MapData.MapHeight - SharkyOptions.GeneratePathingPrecision.Item2; endY += SharkyOptions.GeneratePathingPrecision.Item2)
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
            }

            //Parallel.ForEach(numbers, x =>
            //       {
            //           var pathFinder = new Roy_T.AStar.Paths.PathFinder();
            //           for (int y = SharkyOptions.GeneratePathingPrecision.Item1; y <= MapData.MapHeight - SharkyOptions.GeneratePathingPrecision.Item1; y += SharkyOptions.GeneratePathingPrecision.Item1)
            //           {
            //               var start = new Vector2(x, y);

            //               for (int endX = SharkyOptions.GeneratePathingPrecision.Item1; endX <= MapData.MapWidth - SharkyOptions.GeneratePathingPrecision.Item2; endX += SharkyOptions.GeneratePathingPrecision.Item2)
            //               {
            //                   for (int endY = SharkyOptions.GeneratePathingPrecision.Item1; endY <= MapData.MapHeight - SharkyOptions.GeneratePathingPrecision.Item2; endY += SharkyOptions.GeneratePathingPrecision.Item2)
            //                   {
            //                       var end = new Vector2(endX, endY);
            //                       if (end.X == start.X && end.Y == start.Y) { continue; }

            //                       var path = PathFinder.GetGroundPath(start.X, start.Y, end.X, end.Y, 0, pathFinder);
            //                       if (path.Count == 0) { continue; }
            //                       var data = new PathData { StartPosition = start, EndPosition = end, Path = path };
            //                       pathData.Add(data);
            //                   }
            //               }
            //           }
            //       });
            return pathData.ToList();
        }
    }
}
