using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sharky.Builds.BuildingPlacement
{
    public class WallDataService
    {
        List<MapWallData> PartialMapWallData;
        List<MapWallData> BlockMapWallData;
        List<MapWallData> TerranMapWallData;

        public WallDataService()
        {
            PartialMapWallData = LoadMapWallData("partial");
            BlockMapWallData = LoadMapWallData("block");
            TerranMapWallData = LoadMapWallData("terran");
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
    }
}
