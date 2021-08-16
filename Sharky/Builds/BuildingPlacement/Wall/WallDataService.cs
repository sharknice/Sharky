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

        public WallDataService()
        {
            PartialMapWallData = LoadMapWallData("partial");
            BlockMapWallData = LoadMapWallData("block");
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

        public List<WallData> GetPartialWallData(string map)
        {
            var data = PartialMapWallData.FirstOrDefault(m => map.Replace(" ","").ToLower().Contains(m.MapName.ToLower()));
            if (data != null)
            {
                return data.WallData;
            }
            return null;
        }

        public List<WallData> GetBlockWallData(string map)
        {
            var data = BlockMapWallData.FirstOrDefault(m => map.Replace(" ", "").ToLower().Contains(m.MapName.ToLower()));
            if (data != null)
            {
                return data.WallData;
            }
            return null;
        }
    }
}
