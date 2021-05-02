using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sharky.Builds.BuildingPlacement
{
    public class WallDataService
    {
        List<MapWallData> PartialMapWallData;

        public WallDataService()
        {
            PartialMapWallData = LoadPartialMapWallData();
        }

        List<MapWallData> LoadPartialMapWallData()
        {
            var mapWallData = new List<MapWallData>();
            var wallFolder = Directory.GetCurrentDirectory() + "/data/wall/partial";
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
    }
}
