using Newtonsoft.Json;
using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Sharky.Builds.BuildingPlacement
{
    public class WallDataService
    {
        MapData MapData;
        BaseData BaseData;

        List<MapWallData> PartialMapWallData;
        List<MapWallData> BlockMapWallData;
        List<MapWallData> TerranMapWallData;

        ChokePointService ChokePointService;
        ChokePointsService ChokePointsService;

        Dictionary<string, List<WallData>> GeneratedWallData;

        public WallDataService(DefaultSharkyBot defaultSharkyBot)
        {
            MapData = defaultSharkyBot.MapData;
            BaseData = defaultSharkyBot.BaseData;
            ChokePointService = defaultSharkyBot.ChokePointService;
            ChokePointsService = defaultSharkyBot.ChokePointsService;

            PartialMapWallData = LoadMapWallData("partial");
            BlockMapWallData = LoadMapWallData("block");
            TerranMapWallData = LoadMapWallData("terran");

            GeneratedWallData = LoadGeneratedMapWallData();           
        }

        List<MapWallData> LoadMapWallData(string folder)
        {
            var mapWallData = new List<MapWallData>();
            var wallFolder = Directory.GetCurrentDirectory() + "/StaticData/wall/" + folder;
            if (Directory.Exists(wallFolder))
            {
                foreach (var fileName in Directory.GetFiles(wallFolder))
                {
                    using (StreamReader file = File.OpenText(fileName))
                    {
                        var wallData = JsonConvert.DeserializeObject<List<WallData>>(file.ReadToEnd());
                        var data = new MapWallData { MapName = Path.GetFileNameWithoutExtension(fileName), WallData = wallData };
                        mapWallData.Add(data);
                    }
                }
            }

            return mapWallData;
        }

        public Dictionary<string, List<WallData>> LoadGeneratedMapWallData()
        {
            var dictionary = new Dictionary<string, List<WallData>>();
            var wallFolder = GetGneratedWallDataFolder();
            Directory.CreateDirectory(wallFolder);
            foreach (var fileName in Directory.GetFiles(wallFolder))
            {
                using (StreamReader file = File.OpenText(fileName))
                {
                    var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                    var wallData = (List<WallData>)serializer.Deserialize(file, typeof(List<WallData>));
                    dictionary[Path.GetFileNameWithoutExtension(fileName)] = wallData;
                }
            }
            return dictionary;
        }

        public void SaveGeneratedMapWallData(string map, List<WallData> wallData)
        {
            var wallFolder = GetGneratedWallDataFolder();
            var fileName = GetGeneratedWallDataFileName(map, wallFolder);
            Console.WriteLine($"Saving wall data to {fileName}");

            Directory.CreateDirectory(wallFolder);
            string json = JsonConvert.SerializeObject(wallData, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });
            File.WriteAllText(fileName, json, Encoding.UTF8);
        }

        public List<WallData> GetWallData(string mapName)
        {
            if (GeneratedWallData.ContainsKey(mapName))
            {
                return GeneratedWallData[mapName];
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Calculating wall data");
            var wallData = new List<WallData>();
            AddTerranWallData(mapName, wallData);
            AddPartialWallData(mapName, wallData);
            AddBlockWallData(mapName, wallData);
            AddCalcultedWallData(wallData);
            stopwatch.Stop();
            Console.WriteLine($"Calculating wall data in {stopwatch.ElapsedMilliseconds} ms");
            SaveGeneratedMapWallData(mapName, wallData);
            return wallData;
        }

        private string GetGneratedWallDataFolder()
        {
            return Directory.GetCurrentDirectory() + "/data/wall/";
        }

        private static string GetGeneratedWallDataFileName(string map, string wallFolder)
        {
            return $"{wallFolder}{map}.json";
        }

        public void AddPartialWallData(string map, List<WallData> wallData)
        {
            var loadedData = PartialMapWallData.FirstOrDefault(m => map.Replace(" ","").ToLower().Contains(m.MapName.ToLower()));
            AddWallData(loadedData, wallData);
        }

        public void AddBlockWallData(string map, List<WallData> wallData)
        {
            var loadedData = BlockMapWallData.FirstOrDefault(m => map.Replace(" ", "").ToLower().Contains(m.MapName.ToLower()));
            AddWallData(loadedData, wallData);
        }

        public void AddTerranWallData(string map, List<WallData> wallData)
        {
            var loadedData = TerranMapWallData.FirstOrDefault(m => map.Replace(" ", "").ToLower().Contains(m.MapName.ToLower()));
            AddWallData(loadedData, wallData);
        }

        void AddWallData(MapWallData loadedData, List<WallData> wallData)
        {
            if (loadedData?.WallData != null)
            {
                foreach (var loaded in loadedData.WallData)
                {
                    var data = wallData.FirstOrDefault(d => d.BasePosition.X == loaded.BasePosition.X && d.BasePosition.Y == loaded.BasePosition.Y);
                    if (data != null)
                    {
                        wallData.Remove(data);
                    }
                    else
                    {
                        data = new WallData { BasePosition = loaded.BasePosition };
                    }

                    if (loaded.Bunkers != null)
                    {
                        data.Bunkers = loaded.Bunkers;
                    }
                    if (loaded.Depots != null)
                    {
                        data.Depots = loaded.Depots;
                    }
                    if (loaded.FullDepotWall != null)
                    {
                        data.FullDepotWall = loaded.FullDepotWall;
                    }
                    if (loaded.Production != null)
                    {
                        data.Production = loaded.Production;
                    }
                    if (loaded.ProductionWithAddon != null)
                    {
                        data.ProductionWithAddon = loaded.ProductionWithAddon;
                    }

                    if (loaded.Block != null)
                    {
                        data.Block = loaded.Block;
                    }
                    if (loaded.Pylons != null)
                    {
                        data.Pylons = loaded.Pylons;
                    }
                    if (loaded.WallSegments != null)
                    {
                        data.WallSegments = loaded.WallSegments;
                    }
                    if (loaded.Door != null)
                    {
                        data.Door = loaded.Door;
                    }

                    if (loaded.RampCenter != null)
                    {
                        data.RampCenter = loaded.RampCenter;
                    }

                    wallData.Add(data);
                }
            }
        }

        public void AddCalcultedWallData(List<WallData> wallData)
        {
            var oppositeBase = BaseData.EnemyBaseLocations.FirstOrDefault();
            var oppositeLocation = new Point2D { X = oppositeBase.Location.X + 4, Y = oppositeBase.Location.Y + 4 };
            var baseLocation = BaseData.BaseLocations.FirstOrDefault();
            if (baseLocation != null)
            {
                var data = wallData.FirstOrDefault(d => d.BasePosition.X == baseLocation.Location.X && d.BasePosition.Y == baseLocation.Location.Y);
                wallData.Remove(data);
                if (data == null) { data = new WallData { BasePosition = baseLocation.Location }; }
                data = AddCalculatedWallDataForBase(baseLocation, oppositeLocation, data);
                wallData.Add(data);
            }

            oppositeBase = BaseData.BaseLocations.FirstOrDefault();
            oppositeLocation = new Point2D { X = oppositeBase.Location.X + 4, Y = oppositeBase.Location.Y + 4 };
            baseLocation = BaseData.EnemyBaseLocations.FirstOrDefault();
            if (baseLocation != null)
            {
                var data = wallData.FirstOrDefault(d => d.BasePosition.X == baseLocation.Location.X && d.BasePosition.Y == baseLocation.Location.Y);
                wallData.Remove(data);
                if (data == null) { data = new WallData { BasePosition = baseLocation.Location }; }
                data = AddCalculatedWallDataForBase(baseLocation, oppositeLocation, data);
                wallData.Add(data);
            }
        }

        private WallData AddCalculatedWallDataForBase(BaseLocation baseLocation, Point2D oppositeLocation, WallData data)
        {
            var location = new Point2D { X = baseLocation.Location.X + 4, Y = baseLocation.Location.Y + 4 };
            var chokePoints = ChokePointsService.GetChokePoints(location, oppositeLocation, 0);
            var chokePoint = chokePoints.Good.FirstOrDefault();
            if (chokePoint != null && Vector2.DistanceSquared(chokePoint.Center, new Vector2(location.X, location.Y)) < 900)
            {
                var wallPoints = ChokePointService.GetWallOffPoints(chokePoint.Points);

                if (wallPoints != null)
                {
                    var wallCenter = new Vector2(wallPoints.Sum(p => p.X) / wallPoints.Count(), wallPoints.Sum(p => p.Y) / wallPoints.Count());

                    if (chokePoint.Center.X > wallCenter.X) // left to right
                    {
                        if (chokePoint.Center.Y < wallCenter.Y) // top to bottom
                        {
                            // start at left side of the top of the ramp
                            var baseX = wallPoints.OrderBy(w => w.X).First().X;
                            var baseY = wallPoints.OrderBy(w => w.X).First().Y;

                            if (data.FullDepotWall == null)
                            {
                                data.FullDepotWall = new List<Point2D> { new Point2D { X = baseX + 3, Y = baseY + 4 }, new Point2D { X = baseX, Y = baseY + 1 }, new Point2D { X = baseX + 1, Y = baseY + 3 } };
                            }
                            if (data.Depots == null)
                            {
                                data.Depots = new List<Point2D> { new Point2D { X = baseX + 3, Y = baseY + 4 }, new Point2D { X = baseX, Y = baseY + 1 } };
                            }
                            if (data.Production == null)
                            {
                                data.Production = new List<Point2D> { new Point2D { X = baseX + .5f, Y = baseY + 3.5f } };
                            }
                            if (data.ProductionWithAddon == null)
                            {
                                data.ProductionWithAddon = new List<Point2D> { new Point2D { X = baseX - 1.5f, Y = baseY + 3.5f } };
                            }
                            if (data.RampCenter == null)
                            {
                                data.RampCenter = new Point2D { X = baseX + 3, Y = baseY + 1 };
                            }

                            if (data.Pylons == null)
                            {
                                data.Pylons = new List<Point2D> { new Point2D { X = baseX - 2, Y = baseY + 6 } };
                            }
                            if (data.WallSegments == null)
                            {
                                data.WallSegments = new List<WallSegment>
                                {
                                    new WallSegment { Position = new Point2D { X = baseX + 2.5f, Y = baseY + 4.5f }, Size = 3 },
                                    new WallSegment { Position = new Point2D { X = baseX - .5f, Y = baseY + 2.5f }, Size = 3 }
                                };
                            }
                            if (data.Block == null)
                            {
                                data.Block = new Point2D { X = baseX - 1, Y = baseY };
                            }
                            if (data.Door == null)
                            {
                                data.Door = new Point2D { X = baseX + .5f, Y = baseY + .5f };
                            }
                        }
                        else // bottom to top
                        {
                            var baseX = wallPoints.First().X;
                            var baseY = wallPoints.First().Y;

                            if (data.FullDepotWall == null)
                            {
                                data.FullDepotWall = new List<Point2D> { new Point2D { X = baseX - 1, Y = baseY }, new Point2D { X = baseX, Y = baseY - 2 }, new Point2D { X = baseX + 2, Y = baseY - 3 } };
                            }
                            if (data.Depots == null)
                            {
                                data.Depots = new List<Point2D> { new Point2D { X = baseX - 1, Y = baseY }, new Point2D { X = baseX + 2, Y = baseY - 3 } };
                            }
                            if (data.Production == null)
                            {
                                data.Production = new List<Point2D> { new Point2D { X = baseX - .5f, Y = baseY - 2.5f } };
                            }
                            if (data.ProductionWithAddon == null)
                            {
                                data.ProductionWithAddon = new List<Point2D> { new Point2D { X = baseX - 2.5f, Y = baseY - 2.5f } };
                            }
                            if (data.RampCenter == null)
                            {
                                data.RampCenter = new Point2D { X = baseX + 2f, Y = baseY };
                            }

                            if (data.Pylons == null)
                            {
                                data.Pylons = new List<Point2D> { new Point2D { X = baseX - 3, Y = baseY - 3 } };
                            }
                            if (data.WallSegments == null)
                            {
                                data.WallSegments = new List<WallSegment>
                                {
                                    new WallSegment { Position = new Point2D { X = baseX - 1.5f, Y = baseY - .5f }, Size = 3 },
                                    new WallSegment { Position = new Point2D { X = baseX + .5f, Y = baseY - 3.5f }, Size = 3 }
                                };
                            }
                            if (data.Block == null)
                            {
                                data.Block = new Point2D { X = baseX + 3, Y = baseY - 4 };
                            }
                            if (data.Door == null)
                            {
                                data.Door = new Point2D { X = baseX + 2, Y = baseY - 3 };
                            }
                        }
                    }
                    else // right to left
                    {
                        if (chokePoint.Center.Y < wallCenter.Y) // top to bottom
                        {
                            var baseX = wallPoints.Last().X;
                            var baseY = wallPoints.Last().Y;

                            if (data.FullDepotWall == null)
                            {
                                data.FullDepotWall = new List<Point2D> { new Point2D { X = baseX, Y = baseY + 1 }, new Point2D { X = baseX - 1, Y = baseY + 3 }, new Point2D { X = baseX - 3, Y = baseY + 4 } };
                            }
                            if (data.Depots == null)
                            {
                                data.Depots = new List<Point2D> { new Point2D { X = baseX, Y = baseY + 1 }, new Point2D { X = baseX - 3, Y = baseY + 4 } };
                            }
                            if (data.Production == null)
                            {
                                data.Production = new List<Point2D> { new Point2D { X = baseX - .5f, Y = baseY + 3.5f } };
                            }
                            if (data.RampCenter == null)
                            {
                                data.RampCenter = new Point2D { X = baseX - 3.5f, Y = baseY + .5f };
                            }

                            if (data.Pylons == null)
                            {
                                data.Pylons = new List<Point2D> { new Point2D { X = baseX + 2, Y = baseY + 5 } };
                            }
                            if (data.WallSegments == null)
                            {
                                data.WallSegments = new List<WallSegment>
                                {
                                    new WallSegment { Position = new Point2D { X = baseX + .5f, Y = baseY + 1.5f }, Size = 3 },
                                    new WallSegment { Position = new Point2D { X = baseX - 1.5f, Y = baseY + 4.5f }, Size = 3 }
                                };
                            }
                            if (data.Block == null)
                            {
                                data.Block = new Point2D { X = baseX - 4, Y = baseY + 3 };
                            }
                            if (data.Door == null)
                            {
                                data.Door = new Point2D { X = baseX - 3, Y = baseY + 4 };
                            }
                        }
                        else // bottom to top
                        {
                            // TODO: set these numbers based on this orderd position, also need to do it for terran wall
                            var baseX = wallPoints.OrderByDescending(w => w.X).First().X;
                            var baseY = wallPoints.OrderByDescending(w => w.X).First().Y;

                            if (data.FullDepotWall == null)
                            {
                                data.FullDepotWall = new List<Point2D> { new Point2D { X = baseX - 2, Y = baseY - 3 }, new Point2D { X = baseX + 1, Y = baseY }, new Point2D { X = baseX, Y = baseY - 2 } };
                            }
                            if (data.RampCenter == null)
                            {
                                data.RampCenter = new Point2D { X = baseX - 2, Y = baseY };
                            }
                            if (data.Depots == null)
                            {
                                data.Depots = new List<Point2D> { new Point2D { X = baseX - 2, Y = baseY - 3 }, new Point2D { X = baseX + 1, Y = baseY } };
                            }
                            if (data.Production == null)
                            {
                                data.Production = new List<Point2D> { new Point2D { X = baseX + .5f, Y = baseY - 2.5f } };
                            }

                            if (data.Pylons == null)
                            {
                                data.Pylons = new List<Point2D> { new Point2D { X = baseX + 3, Y = baseY - 5 } };
                            }
                            if (data.WallSegments == null)
                            {
                                data.WallSegments = new List<WallSegment>
                                {
                                    new WallSegment { Position = new Point2D { X = baseX - 1.5f, Y = baseY - 3.5f }, Size = 3 },
                                    new WallSegment { Position = new Point2D { X = baseX + 1.5f, Y = baseY - 1.5f }, Size = 3 }
                                };
                            }
                            if (data.Block == null)
                            {
                                data.Block = new Point2D { X = baseX + 2, Y = baseY + 1 };
                            }
                            if (data.Door == null)
                            {
                                data.Door = new Point2D { X = baseX + .5f, Y = baseY + .5f };
                            }
                        }
                    }
                }
            }

            return data;
        }
    }
}
