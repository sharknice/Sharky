namespace Sharky.Builds.MacroServices
{
    public class BuildAddOnSwapService
    {
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        BuildingService BuildingService;
        IBuildingPlacement BuildingPlacement;

        public BuildAddOnSwapService(MacroData macroData, ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, BuildingService buildingService, IBuildingPlacement buildingPlacement)
        {
            MacroData = macroData;
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            BuildingService = buildingService;
            BuildingPlacement = buildingPlacement;
        }

        public IEnumerable<SC2Action> BuildAndSwapAddons()
        {
            var commands = new List<SC2Action>();

            foreach (var pair in MacroData.AddOnSwaps)
            {
                if (pair.Value.Started && !pair.Value.Completed)
                {
                    UpdateCommanders(pair.Value);
                    CheckCompletion(pair.Value);
                    if (!pair.Value.Completed)
                    {
                        commands.AddRange(SwapBuildings(pair.Value));
                    }
                }
            }

            if (!MacroData.AddOnSwaps.Any(s => s.Value.Started && !s.Value.Completed))
            {
                commands.AddRange(LandFloatingBuildings());
            }
            else
            {
                if (MacroData.AddOnSwaps.Count(s => s.Value.Started && !s.Value.Completed) > 1)
                {
                    Console.WriteLine("Cancelling AddonSwaps");
                    foreach (var pair in MacroData.AddOnSwaps)
                    {
                        pair.Value.Started = true;
                        pair.Value.Completed = true;
                    }
                }

            }


            return commands;
        }

        private IEnumerable<SC2Action> LandFloatingBuildings()
        {
            var commands = new List<SC2Action>();

            var emptyAddons = ActiveUnitData.SelfUnits.Values.Where(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_TECHLAB || a.Unit.UnitType == (uint)UnitTypes.TERRAN_REACTOR);
            List<UnitCalculation> usedAddons = new List<UnitCalculation>();

            foreach (var floater in ActiveUnitData.Commanders.Values.Where(c => (c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKSFLYING || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORYFLYING || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORTFLYING) && c.UnitRole == UnitRole.None))
            {
                if (!floater.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.LAND || o.AbilityId == (uint)Abilities.LAND_BARRACKS || o.AbilityId == (uint)Abilities.LAND_FACTORY || o.AbilityId == (uint)Abilities.LAND_STARPORT) && !(floater.LastAbility == Abilities.LAND && floater.LastOrderFrame > MacroData.Frame - 100))
                {
                    var freeAddon = emptyAddons.FirstOrDefault();
                    if (freeAddon != null)
                    {
                        var command = floater.Order(MacroData.Frame, Abilities.LAND, new Point2D { X = freeAddon.Position.X - 2.5f, Y = freeAddon.Position.Y + .5f });
                        if (command != null)
                        {
                            commands.AddRange(command);
                        }
                    }
                    else
                    {
                        var location = BuildingPlacement.FindPlacement(floater.UnitCalculation.Position.ToPoint2D(), (UnitTypes)floater.UnitCalculation.Unit.UnitType, 3);
                        var command = floater.Order(MacroData.Frame, Abilities.LAND, location);
                        if (command != null)
                        {
                            commands.AddRange(command);
                        }
                    }
                }
            }
            return commands;
        }

        private void CheckCompletion(AddOnSwap addOnSwap)
        {
            if (addOnSwap.AddOn != null && addOnSwap.AddOnBuilder != null && addOnSwap.AddOnTaker != null)
            {
                if (addOnSwap.AddOnBuilder.UnitCalculation.Unit.BuildProgress == 1 && addOnSwap.AddOnTaker.UnitCalculation.Unit.BuildProgress == 1)
                {
                    if (addOnSwap.AddOnTaker.UnitCalculation.Unit.HasAddOnTag && addOnSwap.AddOnTaker.UnitCalculation.Unit.AddOnTag == addOnSwap.AddOn.UnitCalculation.Unit.Tag)
                    {
                        addOnSwap.Completed = true;
                    }
                }
            }
        }

        List<SC2Action> SwapBuildings(AddOnSwap addOnSwap)
        {
            var commands = new List<SC2Action>();
            if (addOnSwap.Cancel)
            {
                if (addOnSwap.AddOnBuilder.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_REACTOR_BARRACKS || o.AbilityId == (uint)Abilities.BUILD_TECHLAB_BARRACKS || o.AbilityId == (uint)Abilities.BUILD_REACTOR_FACTORY || o.AbilityId == (uint)Abilities.BUILD_TECHLAB_FACTORY || o.AbilityId == (uint)Abilities.BUILD_REACTOR_STARPORT || o.AbilityId == (uint)Abilities.BUILD_TECHLAB_STARPORT))
                {
                    commands.AddRange(addOnSwap.AddOnBuilder.Order(MacroData.Frame, Abilities.CANCEL));
                }
                addOnSwap.Started = false;
            }

            if (addOnSwap.AddOnBuilder != null && addOnSwap.AddOn != null && addOnSwap.AddOn.UnitCalculation.Unit.BuildProgress == 1)
            {
                List<SC2Action> command = null;
                if (addOnSwap.AddOnBuilder.UnitCalculation.UnitTypeData.Name.Contains("Flying"))
                {
                    if (addOnSwap.TakerLocation != null)
                    {
                        if (addOnSwap.AddOnTaker != null &&
                            (addOnSwap.AddOnTaker.UnitCalculation.UnitTypeData.Name.Contains("Flying") || addOnSwap.AddOnTaker.UnitCalculation.Position.X != addOnSwap.TakerLocation.X || addOnSwap.AddOnTaker.UnitCalculation.Position.Y != addOnSwap.TakerLocation.Y))
                        {
                            if (!addOnSwap.AddOnBuilder.UnitCalculation.NearbyAllies.Any(a => a.Unit.Pos.X == addOnSwap.TakerLocation.X + 2.5f && a.Unit.Pos.Y == addOnSwap.TakerLocation.Y - .5f) || BuildingService.Blocked(addOnSwap.TakerLocation.X, addOnSwap.TakerLocation.Y, 1.5f, -.5f, addOnSwap.AddOnBuilder.UnitCalculation.Unit.Tag) || BuildingService.HasAnyCreep(addOnSwap.TakerLocation.X, addOnSwap.TakerLocation.Y, 1.5f))
                            {
                                var unitData = SharkyUnitData.BuildingData[addOnSwap.DesiredAddOnBuilder];
                                addOnSwap.TakerLocation = BuildingPlacement.FindPlacement(addOnSwap.TakerLocation, addOnSwap.DesiredAddOnBuilder, unitData.Size);
                            }
                            if (!addOnSwap.AddOnBuilder.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.LAND_BARRACKS || o.AbilityId == (uint)Abilities.LAND_FACTORY || o.AbilityId == (uint)Abilities.LAND_STARPORT))
                            {
                                command = addOnSwap.AddOnBuilder.Order(MacroData.Frame, Abilities.LAND, addOnSwap.TakerLocation);
                            }
                        }
                        else
                        {
                            command = addOnSwap.AddOnBuilder.Order(MacroData.Frame, Abilities.MOVE, addOnSwap.TakerLocation);
                        }
                    }
                }
                else
                {
                    if (!addOnSwap.AddOnBuilder.UnitCalculation.NearbyEnemies.Any(e => !e.Unit.IsFlying && MacroData.Frame == e.FrameLastSeen && Vector2.DistanceSquared(e.Position, addOnSwap.AddOnBuilder.UnitCalculation.Position) < 25))
                    {
                        command = addOnSwap.AddOnBuilder.Order(MacroData.Frame, Abilities.LIFT);
                    }
                }

                if (command != null)
                {
                    commands.AddRange(command);
                }
            }

            if (addOnSwap.AddOnTaker != null && addOnSwap.AddOnTaker.UnitCalculation.Unit.BuildProgress == 1 && !addOnSwap.AddOnTaker.UnitCalculation.Unit.HasAddOnTag)
            {
                List<SC2Action> command = null;
                if (addOnSwap.AddOnTaker.UnitCalculation.UnitTypeData.Name.Contains("Flying"))
                {
                    if (addOnSwap.AddOn != null && addOnSwap.AddOn.UnitCalculation.Unit.BuildProgress == 1 && 
                        (addOnSwap.AddOnBuilder.UnitCalculation.UnitTypeData.Name.Contains("Flying") || addOnSwap.AddOnBuilder.UnitCalculation.Position.X != addOnSwap.Location.X || addOnSwap.AddOnBuilder.UnitCalculation.Position.Y != addOnSwap.Location.Y))
                    {
                        command = addOnSwap.AddOnTaker.Order(MacroData.Frame, Abilities.LAND, addOnSwap.Location);
                    }
                    else
                    {
                        command = addOnSwap.AddOnTaker.Order(MacroData.Frame, Abilities.MOVE, addOnSwap.Location);
                    }
                }
                else
                {
                    if (addOnSwap.AddOn != null && addOnSwap.AddOnTaker.UnitCalculation.NearbyEnemies.Count(e => Vector2.DistanceSquared(e.Position, addOnSwap.AddOnTaker.UnitCalculation.Position) < 16) == 0)
                    {
                        command = addOnSwap.AddOnTaker.Order(MacroData.Frame, Abilities.LIFT);
                    }
                }

                if (command != null)
                {
                    commands.AddRange(command);
                }
            }

            return commands;
        }

        void UpdateCommanders(AddOnSwap addOnSwap)
        {
            if (addOnSwap.AddOnBuilder == null)
            {
                addOnSwap.AddOnBuilder = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)addOnSwap.DesiredAddOnBuilder);
                if (addOnSwap.AddOnBuilder != null)
                {
                    addOnSwap.Location = new Point2D { X = addOnSwap.AddOnBuilder.UnitCalculation.Unit.Pos.X, Y = addOnSwap.AddOnBuilder.UnitCalculation.Unit.Pos.Y };
                }
            }
            else if (addOnSwap.AddOnBuilder.UnitCalculation.Unit.HasAddOnTag)
            {
                addOnSwap.Location = new Point2D { X = addOnSwap.AddOnBuilder.UnitCalculation.Unit.Pos.X, Y = addOnSwap.AddOnBuilder.UnitCalculation.Unit.Pos.Y };
            }

            if (addOnSwap.AddOnTaker == null)
            {
                addOnSwap.AddOnTaker = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)addOnSwap.DesiredAddOnTaker && !c.UnitCalculation.Unit.HasAddOnTag);
                if (addOnSwap.AddOnTaker == null)
                {
                    addOnSwap.AddOnTaker = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)addOnSwap.DesiredAddOnTaker);
                }
                if (addOnSwap.AddOnTaker == null)
                {
                    addOnSwap.AddOnTaker = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.IsFlying && ((UnitTypes)c.UnitCalculation.Unit.UnitType).ToString().Contains(addOnSwap.DesiredAddOnTaker.ToString()));
                }
                if (addOnSwap.AddOnTaker != null)
                {
                    addOnSwap.TakerLocation = new Point2D { X = addOnSwap.AddOnTaker.UnitCalculation.Unit.Pos.X, Y = addOnSwap.AddOnTaker.UnitCalculation.Unit.Pos.Y };
                }
            }
            if (addOnSwap.AddOn == null && addOnSwap.AddOnBuilder != null && addOnSwap.AddOnBuilder.UnitCalculation.Unit.HasAddOnTag)
            {
                addOnSwap.AddOn = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)addOnSwap.AddOnType && c.UnitCalculation.Unit.Tag == addOnSwap.AddOnBuilder.UnitCalculation.Unit.AddOnTag);
                if (addOnSwap.AddOn != null)
                {
                    addOnSwap.AddOnLocation = new Point2D { X = addOnSwap.AddOn.UnitCalculation.Unit.Pos.X, Y = addOnSwap.AddOn.UnitCalculation.Unit.Pos.Y };
                }
            }
        }
    }
}
