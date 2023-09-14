namespace Sharky.Builds.BuildingPlacement
{
    public class GatewayCannonPlacement : IBuildingPlacement
    {
        BaseData BaseData;
        BuildingService BuildingService;
        ActiveUnitData ActiveUnitData;

        public GatewayCannonPlacement(DefaultSharkyBot defaultSharkyBot)
        {
            BaseData = defaultSharkyBot.BaseData;
            BuildingService = defaultSharkyBot.BuildingService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
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
                        if (unitType == UnitTypes.PROTOSS_PYLON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 2.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X + 1.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - .5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_GATEWAY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 2f, nearestBase.Location.Y + 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X + 1f, nearestBase.Location.Y + 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                    }
                    else if (nearestBase.Location.Y < nearestBase.MineralLineLocation.Y)
                    {
                        if (unitType == UnitTypes.PROTOSS_PYLON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 2.5f, nearestBase.Location.Y - 3.5f, .5f);
                            if (placement != null) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X - 1.5f, nearestBase.Location.Y - 3.5f, .5f);
                            if (placement != null) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 0.5f, nearestBase.Location.Y - 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 4.5f, nearestBase.Location.Y - 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_GATEWAY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 1f, nearestBase.Location.Y - 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X + 2f, nearestBase.Location.Y - 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                    }
                }
                else if (nearestBase.Location.X > nearestBase.MineralLineLocation.X)
                {
                    if (nearestBase.Location.Y == nearestBase.MineralLineLocation.Y)
                    {
                        if (nearestBase.VespeneGeysers != null && nearestBase.VespeneGeysers.Any(g => g.Pos.Y < nearestBase.Location.Y) && nearestBase.VespeneGeysers.Any(g => g.Pos.Y > nearestBase.Location.Y))
                        {
                            if (unitType == UnitTypes.PROTOSS_PYLON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y + 2.5f, .5f);
                                if (placement != null) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y - 1.5f, .5f);
                                if (placement != null) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 3.5f , nearestBase.Location.Y + 0.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y + 4.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_GATEWAY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 6f, nearestBase.Location.Y + 2f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X + 6f, nearestBase.Location.Y - 1f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                        }
                        else
                        {
                            if (unitType == UnitTypes.PROTOSS_PYLON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y + 2.5f, .5f);
                                if (placement != null) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y - 1.5f, .5f);
                                if (placement != null) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y + 0.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y + 4.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_GATEWAY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 6f, nearestBase.Location.Y + 2f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X + 6f, nearestBase.Location.Y - 1f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                        }
                    }
                    if (nearestBase.Location.Y > nearestBase.MineralLineLocation.Y)
                    {
                        if (unitType == UnitTypes.PROTOSS_PYLON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 2.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X + 1.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 0.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_GATEWAY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 2f, nearestBase.Location.Y + 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X + 1f, nearestBase.Location.Y + 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                    }
                    else if (nearestBase.Location.Y < nearestBase.MineralLineLocation.Y)
                    {
                        if (nearestBase.VespeneGeysers != null && nearestBase.VespeneGeysers.Any(g => g.Pos.Y < nearestBase.Location.Y) && nearestBase.VespeneGeysers.Any(g => g.Pos.Y > nearestBase.Location.Y))
                        {
                            if (unitType == UnitTypes.PROTOSS_PYLON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 2.5f, nearestBase.Location.Y - 3.5f, .5f);
                                if (placement != null) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X - 1.5f, nearestBase.Location.Y - 3.5f, .5f);
                                if (placement != null) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 0.5f, nearestBase.Location.Y - 3.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 4.5f, nearestBase.Location.Y - 3.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_GATEWAY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 1f, nearestBase.Location.Y - 6f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X + 2f, nearestBase.Location.Y - 6f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                        }
                        else
                        {
                            if (unitType == UnitTypes.PROTOSS_PYLON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 2.5f, nearestBase.Location.Y - 3.5f, .5f);
                                if (placement != null) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X - 1.5f, nearestBase.Location.Y - 3.5f, .5f);
                                if (placement != null) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X + 0.5f, nearestBase.Location.Y - 3.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y - 3.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_GATEWAY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 1f, nearestBase.Location.Y - 6f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X + 2f, nearestBase.Location.Y - 6f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                        }
                    }
                }
                else if (nearestBase.Location.X < nearestBase.MineralLineLocation.X)
                {
                    if (nearestBase.Location.Y == nearestBase.MineralLineLocation.Y)
                    {
                        if (nearestBase.VespeneGeysers != null && nearestBase.VespeneGeysers.Any(g => g.Pos.Y < nearestBase.Location.Y) && nearestBase.VespeneGeysers.Any(g => g.Pos.Y > nearestBase.Location.Y))
                        {
                            if (unitType == UnitTypes.PROTOSS_PYLON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y - 2.5f, .5f);
                                if (placement != null) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y + 1.5f, .5f);
                                if (placement != null) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y - .5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y - 4.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_GATEWAY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 6f, nearestBase.Location.Y - 2f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X - 6f, nearestBase.Location.Y + 1f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                        }
                        else
                        {
                            if (unitType == UnitTypes.PROTOSS_PYLON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y - 2.5f, .5f);
                                if (placement != null) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y + 1.5f, .5f);
                                if (placement != null) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y - .5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y - 4.5f, .5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                            if (unitType == UnitTypes.PROTOSS_GATEWAY)
                            {
                                var placement = GetPlacement(nearestBase.Location.X - 6f, nearestBase.Location.Y - 2f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }

                                placement = GetPlacement(nearestBase.Location.X - 6f, nearestBase.Location.Y + 1f, 1.5f);
                                if (placement != null && Powered(placement)) { return placement; }
                            }
                        }
                    }
                    else if (nearestBase.Location.Y > nearestBase.MineralLineLocation.Y)
                    {
                        if (unitType == UnitTypes.PROTOSS_PYLON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 2.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X + 1.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 0.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 3.5f, nearestBase.Location.Y + 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_GATEWAY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 2f, nearestBase.Location.Y + 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X + 1f, nearestBase.Location.Y + 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                    }
                    else
                    {
                        if (unitType == UnitTypes.PROTOSS_PYLON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 2.5f, nearestBase.Location.Y - 3.5f, .5f);
                            if (placement != null) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X - 1.5f, nearestBase.Location.Y - 3.5f, .5f);
                            if (placement != null) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                        {
                            var placement = GetPlacement(nearestBase.Location.X + 0.5f, nearestBase.Location.Y - 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_SHIELDBATTERY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 3.5f, nearestBase.Location.Y - 3.5f, .5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                        if (unitType == UnitTypes.PROTOSS_GATEWAY)
                        {
                            var placement = GetPlacement(nearestBase.Location.X - 1f, nearestBase.Location.Y - 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }

                            placement = GetPlacement(nearestBase.Location.X + 2f, nearestBase.Location.Y - 6f, 1.5f);
                            if (placement != null && Powered(placement)) { return placement; }
                        }
                    }
                }
            }

            return null;
        }

        Point2D GetPlacement(float x, float y, float radius)
        {
            var point = new Point2D { X = x, Y = y };
            if (BuildingService.AreaBuildable(point.X, point.Y, radius) && !BuildingService.Blocked(point.X, point.Y, radius, .2f) && !BuildingService.HasAnyCreep(point.X, point.Y, radius))
            {
                return point;
            }
            return null;
        }

        bool Powered(Point2D point)
        {
            var targetVector = point.ToVector2();
            return ActiveUnitData.Commanders.Values.Any(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1 && Vector2.DistanceSquared(c.UnitCalculation.Position, targetVector) <= 7 * 7);
        }
    }
}
