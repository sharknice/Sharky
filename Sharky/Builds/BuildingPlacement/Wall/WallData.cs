﻿namespace Sharky.Builds.BuildingPlacement
{
    public class WallData
    {
        public Point2D BasePosition { get; set; }
        public List<WallSegment> WallSegments { get; set; }
        public List<Point2D> Pylons { get; set; }
        public Point2D Door { get; set; }
        public Point2D Block { get; set; }
        public List<Point2D> Production { get; set; }
        public List<Point2D> ProductionWithAddon { get; set; }
        public List<Point2D> Depots { get; set; }
        public List<Point2D> Bunkers { get; set; }
        public List<Point2D> FullDepotWall { get; set; }
        public Point2D RampCenter { get; set; }
        public Point2D RampBottom { get; set; }
    }
}
