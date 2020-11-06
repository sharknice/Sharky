using System;
using System.Collections.Generic;
using System.Text;

namespace Sharky.Pathing
{
    public class MapDataService
    {
        MapData MapData;

        public MapDataService(MapData mapData)
        {
            MapData = mapData;
        }

        public List<MapCell> GetCells(float x, float y, float radius)
        {
            return new List<MapCell>();
        }
    }
}
