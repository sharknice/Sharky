namespace Sharky.Builds.BuildingPlacement
{
    public class ProtectNexusPylonPlacement : IBuildingPlacement
    {
        BaseData BaseData;
        BuildingService BuildingService;

        public ProtectNexusPylonPlacement(DefaultSharkyBot defaultSharkyBot)
        {
            BaseData = defaultSharkyBot.BaseData;
            BuildingService = defaultSharkyBot.BuildingService;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool requireVision = false, bool allowBlockBase = true)
        {
            var reference = new Vector2(target.X, target.Y);
            var nearestBase = BaseData.BaseLocations.OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), reference)).FirstOrDefault();

            if (nearestBase != null && nearestBase.MineralLineLocation != null)
            {
                if (nearestBase.Location.X == nearestBase.MineralLineLocation.X)
                {
                    if (nearestBase.Location.Y > nearestBase.MineralLineLocation.Y)
                    {
                        var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y + 2.5f);
                        if (placement != null)
                        {
                            return placement;
                        }
                        else
                        {
                            return GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y + 2.5f);
                        }
                    }
                    else if (nearestBase.Location.Y < nearestBase.MineralLineLocation.Y)
                    {
                        var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y - 2.5f);
                        if (placement != null)
                        {
                            return placement;
                        }
                        else
                        {
                            return GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y - 2.5f);
                        }
                    }
                }
                else if (nearestBase.Location.X > nearestBase.MineralLineLocation.X)
                {
                    if (nearestBase.Location.Y == nearestBase.MineralLineLocation.Y)
                    {
                        if (nearestBase.VespeneGeysers != null && nearestBase.VespeneGeysers.Any(g => g.Pos.Y < nearestBase.Location.Y) && nearestBase.VespeneGeysers.Any(g => g.Pos.Y > nearestBase.Location.Y))
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 0.5f, nearestBase.Location.Y - 3.5f);
                            if (placement != null)
                            {
                                return placement;
                            }
                            else
                            {
                                return GetPlacement(nearestBase.Location.X + 0.5f, nearestBase.Location.Y + 3.5f);
                            }
                        }
                        else
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 2.5f, nearestBase.Location.Y + 3.5f);
                            if (placement != null)
                            {
                                return placement;
                            }
                            else
                            {
                                return GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y - 2.5f);
                            }
                        }
                    }
                    if (nearestBase.Location.Y > nearestBase.MineralLineLocation.Y)
                    {
                        var placement = GetPlacement(nearestBase.Location.X + 2.5f, nearestBase.Location.Y - 3.5f);
                        if (placement != null)
                        {
                            return placement;
                        }
                        else
                        {
                            return GetPlacement(nearestBase.Location.X - 2.5f, nearestBase.Location.Y + 3.5f);
                        }
                    }
                    else if (nearestBase.Location.Y < nearestBase.MineralLineLocation.Y)
                    {
                        var placement = GetPlacement(nearestBase.Location.X - 2.5f, nearestBase.Location.Y - 3.5f);
                        if (placement != null)
                        {
                            return placement;
                        }
                        else
                        {
                            return GetPlacement(nearestBase.Location.X + 2.5f, nearestBase.Location.Y + 3.5f);
                        }
                    }
                }
                else if (nearestBase.Location.X < nearestBase.MineralLineLocation.X)
                {
                    if (nearestBase.Location.Y == nearestBase.MineralLineLocation.Y)
                    {
                        if (nearestBase.VespeneGeysers != null && nearestBase.VespeneGeysers.Any(g => g.Pos.Y < nearestBase.Location.Y) && nearestBase.VespeneGeysers.Any(g => g.Pos.Y > nearestBase.Location.Y))
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 0.5f, nearestBase.Location.Y - 3.5f);
                            if (placement != null)
                            {
                                return placement;
                            }
                            else
                            {
                                return GetPlacement(nearestBase.Location.X - 0.5f, nearestBase.Location.Y + 3.5f);
                            }
                        }
                        else
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 2.5f, nearestBase.Location.Y - 3.5f);
                            if (placement != null)
                            {
                                return placement;
                            }
                            else
                            {
                                return GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y + 2.5f);
                            }
                        }
                    }
                    else if (nearestBase.Location.Y > nearestBase.MineralLineLocation.Y)
                    {
                        var placement = GetPlacement(nearestBase.Location.X - 2.5f, nearestBase.Location.Y - 3.5f);
                        if (placement != null)
                        {
                            return placement;
                        }
                        else
                        {
                            return GetPlacement(nearestBase.Location.X + 2.5f, nearestBase.Location.Y + 3.5f);
                        }
                    }
                    else
                    {
                        var placement = GetPlacement(nearestBase.Location.X + 2.5f, nearestBase.Location.Y - 3.5f);
                        if (placement != null)
                        {
                            return placement;
                        }
                        else
                        {
                            return GetPlacement(nearestBase.Location.X - 2.5f, nearestBase.Location.Y + 3.5f);
                        }
                    }
                }
            }

            return null;
        }

        Point2D GetPlacement(float x, float y)
        {
            var point = new Point2D { X = x, Y = y };

            if (BuildingService.AreaBuildable(point.X, point.Y, .5f) && !BuildingService.Blocked(point.X, point.Y, .5f, .2f) && !BuildingService.HasAnyCreep(point.X, point.Y, .5f))
            {
                return point;
            }
            return null;
        }
    }
}
