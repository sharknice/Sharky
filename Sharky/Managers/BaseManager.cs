using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers
{
    public class BaseManager : SharkyManager, IBaseManager
    {
        public List<BaseLocation> BaseLocations { get; private set; }
        public List<BaseLocation> SelfBases { get; private set; }
        public BaseLocation MainBase { get; private set; }

        ImageData PlacementGrid;

        UnitDataManager UnitDataManager;
        IUnitManager UnitManager;
        IPathFinder PathFinder;

        public BaseManager(UnitDataManager unitDataManager, IUnitManager unitManager, IPathFinder pathFinder)
        {
            UnitDataManager = unitDataManager;
            UnitManager = unitManager;
            PathFinder = pathFinder;
            BaseLocations = new List<BaseLocation>();
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            PlacementGrid = gameInfo.StartRaw.PlacementGrid;

            var mineralFields = new List<Unit>();
            foreach (var unit in observation.Observation.RawData.Units)
            {
                if (UnitDataManager.MineralFieldTypes.Contains((UnitTypes)unit.UnitType))
                {
                    mineralFields.Add(unit);
                }
            }
            mineralFields = mineralFields.OrderBy(m => m.Pos.X).ThenBy(m => m.Pos.Y).ToList();

            var mineralGroups = new Dictionary<ulong, int>();
            int currentSet = 0;
            foreach (var mineralField in mineralFields)
            {
                if (!mineralGroups.ContainsKey(mineralField.Tag))
                {
                    var baseLocation = new BaseLocation();
                    BaseLocations.Add(baseLocation);
                    mineralGroups.Add(mineralField.Tag, currentSet);
                    baseLocation.MineralFields.Add(mineralField);

                    for (int i = 0; i < baseLocation.MineralFields.Count; i++)
                    {
                        var mineralFieldA = baseLocation.MineralFields[i];
                        foreach (var closeMineralField in mineralFields)
                        {
                            if (mineralGroups.ContainsKey(closeMineralField.Tag))
                            {
                                continue;
                            }

                            if (Vector2.DistanceSquared(new Vector2(mineralFieldA.Pos.X, mineralFieldA.Pos.Y), new Vector2(closeMineralField.Pos.X, closeMineralField.Pos.Y)) <= 16)
                            {
                                mineralGroups.Add(closeMineralField.Tag, currentSet);
                                baseLocation.MineralFields.Add(closeMineralField);
                            }
                        }
                    }
                    currentSet++;
                }
            }

            var gasses = new List<Unit>();
            foreach (var unit in observation.Observation.RawData.Units)
            {
                if (UnitDataManager.GasGeyserTypes.Contains((UnitTypes)unit.UnitType))
                {
                    gasses.Add(unit);
                }
            }
            gasses = gasses.OrderBy(g => g.Pos.X).ThenBy(g => g.Pos.Y).ToList();

            foreach (var location in BaseLocations)
            {
                DetermineFinalLocation(location, gasses);
                SetMineralLineLocation(location);
            }

            var startingUnit = observation.Observation.RawData.Units.FirstOrDefault(u => u.Alliance == Alliance.Self && UnitDataManager.ResourceCenterTypes.Contains((UnitTypes)u.UnitType));

            //BaseLocations = BaseLocations.OrderBy(b => Vector2.DistanceSquared(new Vector2(startingUnit.Pos.X, startingUnit.Pos.Y), new Vector2(b.Location.X, b.Location.Y))).ToList();
            BaseLocations = BaseLocations.OrderBy(b => PathFinder.GetGroundPath(startingUnit.Pos.X + 4, startingUnit.Pos.Y + 4, b.Location.X, b.Location.Y, 0).Count()).ToList();
            MainBase = BaseLocations.FirstOrDefault();
            MainBase.ResourceCenter = startingUnit;
            SelfBases = new List<BaseLocation> { MainBase };
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            foreach (var tag in UnitManager.DeadUnits)
            {
                foreach (var baseLocation in BaseLocations)
                {
                    baseLocation.MineralFields.RemoveAll(m => m.Tag == tag);
                }
            }

            UpdateSelfBases();

            return new List<SC2APIProtocol.Action>();
        }

        void UpdateSelfBases()
        {
            if (SelfBases.Count() != UnitManager.EquivalentTypeCount(UnitTypes.PROTOSS_NEXUS) + UnitManager.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) + UnitManager.EquivalentTypeCount(UnitTypes.ZERG_HATCHERY))
            {
                var resourceCenters = UnitManager.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter));
                SelfBases = BaseLocations.Where(b => resourceCenters.Any(r => Vector2.DistanceSquared(new Vector2(r.Unit.Pos.X, r.Unit.Pos.Y), new Vector2(b.Location.X, b.Location.Y)) < 25)).ToList();
                foreach (var selfBase in SelfBases)
                {
                    selfBase.ResourceCenter = resourceCenters.FirstOrDefault(r => Vector2.DistanceSquared(new Vector2(r.Unit.Pos.X, r.Unit.Pos.Y), new Vector2(selfBase.Location.X, selfBase.Location.Y)) < 25).Unit;
                }
            }
            foreach (var selfBase in SelfBases)
            {
                if (UnitManager.SelfUnits.TryGetValue(selfBase.ResourceCenter.Tag, out UnitCalculation updatedUnit))
                {
                    if (updatedUnit != null)
                    {
                        selfBase.ResourceCenter = updatedUnit.Unit;
                    }
                }

                for (var index = 0; index < selfBase.MineralFields.Count; index++)
                {
                    if (selfBase.MineralFields[index].DisplayType == DisplayType.Snapshot)
                    {
                        var visibleMineral = UnitManager.NeutralUnits.FirstOrDefault(m => m.Value.Unit.DisplayType == DisplayType.Visible && m.Value.Unit.Pos.X == selfBase.MineralFields[index].Pos.X && m.Value.Unit.Pos.Y == selfBase.MineralFields[index].Pos.Y).Value;
                        if (visibleMineral != null)
                        {
                            selfBase.MineralFields[index] = visibleMineral.Unit;
                        }
                    }
                }

                for (var index = 0; index < selfBase.VespeneGeysers.Count; index++)
                {
                    if (selfBase.VespeneGeysers[index].DisplayType == DisplayType.Snapshot)
                    {
                        var visibleGeyser = UnitManager.NeutralUnits.FirstOrDefault(m => m.Value.Unit.DisplayType == DisplayType.Visible && m.Value.Unit.Pos.X == selfBase.VespeneGeysers[index].Pos.X && m.Value.Unit.Pos.Y == selfBase.VespeneGeysers[index].Pos.Y).Value;
                        if (visibleGeyser != null)
                        {
                            selfBase.VespeneGeysers[index] = visibleGeyser.Unit;
                        }
                    }
                }
            }
        }

        void SetMineralLineLocation(BaseLocation baseLocation)
        {
            var vectors = baseLocation.MineralFields.Select(m => new Vector2(m.Pos.X, m.Pos.Y));
            baseLocation.MineralLineLocation = new Point2D { X = vectors.Average(v => v.X), Y = vectors.Average(v => v.Y) };
        }

        void DetermineFinalLocation(BaseLocation baseLocation, List<Unit> gasses)
        {
            for (int i = 0; i < gasses.Count; i++)
            {
                foreach (var mineralField in baseLocation.MineralFields)
                {
                    if (Vector2.DistanceSquared(new Vector2(mineralField.Pos.X, mineralField.Pos.Y), new Vector2(gasses[i].Pos.X, gasses[i].Pos.Y)) <= 64)
                    {
                        baseLocation.VespeneGeysers.Add(gasses[i]);
                        gasses[i] = gasses[gasses.Count - 1];
                        gasses.RemoveAt(gasses.Count - 1);
                        i--;
                        break;
                    }
                }
            }

            if (baseLocation.VespeneGeysers.Count == 1)
            {
                for (int i = 0; i < gasses.Count; i++)
                {
                    if (Vector2.DistanceSquared(new Vector2(baseLocation.VespeneGeysers[0].Pos.X, baseLocation.VespeneGeysers[0].Pos.Y), new Vector2(gasses[i].Pos.X, gasses[i].Pos.Y)) <= 64)
                    {
                        baseLocation.VespeneGeysers.Add(gasses[i]);
                        gasses[i] = gasses[gasses.Count - 1];
                        gasses.RemoveAt(gasses.Count - 1);
                        i--;
                        break;
                    }
                }
            }

            float x = 0;
            float y = 0;
            foreach (var field in baseLocation.MineralFields)
            {
                x += (int)field.Pos.X;
                y += (int)field.Pos.Y;
            }
            x /= baseLocation.MineralFields.Count;
            y /= baseLocation.MineralFields.Count;

            // Round to nearest half position. bases are 5x5 and therefore always centered in the middle of a tile.
            x = (int)(x) + 0.5f;
            y = (int)(y) + 0.5f;

            // Temporary position, we still need a proper position.
            baseLocation.Location = new Point2D { X = x, Y = y };

            Unit closest = null;
            var closestDistance = 10000f;
            foreach (var mineralField in baseLocation.MineralFields)
            {
                var distance = Math.Abs(mineralField.Pos.X - baseLocation.Location.X) + Math.Abs(mineralField.Pos.Y - baseLocation.Location.Y);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = mineralField;
                }
            }

            // Move the estimated base position slightly away from the closest mineral.
            // This ensures that the base location will not end up on the far side of the minerals.
            if (closest.Pos.X < baseLocation.Location.X)
            {
                baseLocation.Location.X += 2;
            }
            else if (closest.Pos.X > baseLocation.Location.X)
            {
                baseLocation.Location.X -= 2;
            }
            if (closest.Pos.Y < baseLocation.Location.Y)
            {
                baseLocation.Location.Y += 2;
            }
            else if (closest.Pos.Y > baseLocation.Location.Y)
            {
                baseLocation.Location.Y -= 2;
            }

            var closestLocation = 1000000f;
            var approximateLocation = baseLocation.Location;
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j == 0 || j < i; j++)
                {
                    float maxDist;
                    Point2D newPos;
                    newPos = new Point2D { X = approximateLocation.X + i - j, Y = approximateLocation.Y + j };
                    maxDist = checkPosition(newPos, baseLocation);
                    if (maxDist < closestLocation)
                    {
                        baseLocation.Location = newPos;
                        closestLocation = maxDist;
                    }

                    newPos = new Point2D { X = approximateLocation.X + i - j, Y = approximateLocation.Y - j };
                    maxDist = checkPosition(newPos, baseLocation);
                    if (maxDist < closestLocation)
                    {
                        baseLocation.Location = newPos;
                        closestLocation = maxDist;
                    }

                    newPos = new Point2D { X = approximateLocation.X - i + j, Y = approximateLocation.Y + j };
                    maxDist = checkPosition(newPos, baseLocation);
                    if (maxDist < closestLocation)
                    {
                        baseLocation.Location = newPos;
                        closestLocation = maxDist;
                    }

                    newPos = new Point2D { X = approximateLocation.X - i + j, Y = approximateLocation.Y - j };
                    maxDist = checkPosition(newPos, baseLocation);
                    if (maxDist < closestLocation)
                    {
                        baseLocation.Location = newPos;
                        closestLocation = maxDist;
                    }
                }
            }
        }

        float checkPosition(Point2D position, BaseLocation location)
        {
            foreach (var mineralField in location.MineralFields)
            {
                if (Math.Abs(mineralField.Pos.X - position.X) + Math.Abs(mineralField.Pos.Y - position.Y) <= 10 && Math.Abs(mineralField.Pos.X - position.X) <= 5.5 && Math.Abs(mineralField.Pos.Y - position.Y) <= 5.5)
                {
                    return 100000000;
                }
            }
            foreach (var gas in location.VespeneGeysers)
            {
                if (Math.Abs(gas.Pos.X - position.X) + Math.Abs(gas.Pos.Y - position.Y) <= 11 && Math.Abs(gas.Pos.X - position.X) <= 6.1 && Math.Abs(gas.Pos.Y - position.Y) <= 6.1)
                {
                    return 100000000;
                }
                
                if (Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), new Vector2(position.X, position.Y)) >= 121)
                {
                    return 100000000;
                }
            }

            // Check if a resource center can actually be built here.
            for (float x = -2.5f; x < 2.5f + 0.1f; x++)
            {
                for (float y = -2.5f; y < 2.5f + 0.1f; y++)
                {
                    if (!GetTilePlacable((int)Math.Round(position.X + x), (int)Math.Round(position.Y + y)))
                    {
                        return 100000000;
                    }
                }
            }

            float maxDist = 0;
            foreach (var mineralField in location.MineralFields)
            {
                maxDist += Vector2.DistanceSquared(new Vector2(mineralField.Pos.X, mineralField.Pos.Y), new Vector2(position.X, position.Y));
            }

            foreach (var gas in location.VespeneGeysers)
            {
                maxDist += Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), new Vector2(position.X, position.Y));
            }
            return maxDist;
        }

        bool GetTilePlacable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= PlacementGrid.Size.X || y >= PlacementGrid.Size.Y)
            {
                return false;
            }
            int pixelID = x + y * PlacementGrid.Size.X;
            int byteLocation = pixelID / 8;
            int bitLocation = pixelID % 8;
            var result = ((PlacementGrid.Data[byteLocation] & 1 << (7 - bitLocation)) == 0) ? 0 : 1;
            return result != 0;
        }
    }
}
