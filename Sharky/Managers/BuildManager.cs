using SC2APIProtocol;
using Sharky.Builds;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharky.Managers
{
    public class BuildManager : SharkyManager
    {
        // do what build.cs and sharkbuild.cs does, has a SharkyBuild, switches builds, builds the actual units, or calls something to build the actual units
        BuildOptions BuildOptions;

        List<UnitTypes> Units;
        Dictionary<UnitTypes, int> DesiredUnitCounts;
        Dictionary<UnitTypes, bool> BuildUnits;

        List<UnitTypes> Production;
        Dictionary<UnitTypes, int> DesiredProductionCounts;
        Dictionary<UnitTypes, bool> BuildProduction;

        List<UnitTypes> Tech;
        Dictionary<UnitTypes, int> DesiredTechCounts;
        Dictionary<UnitTypes, bool> BuildTech;

        List<UnitTypes> DefensiveBuildings;
        Dictionary<UnitTypes, int> DesiredDefensiveBuildingsCounts;
        Dictionary<UnitTypes, bool> BuildDefensiveBuildings;

        Dictionary<UnitTypes, bool> DesiredUpgrades;

        public List<UnitTypes> NexusUnits;
        public List<UnitTypes> GatewayUnits;
        public List<UnitTypes> RoboticsFacilityUnits;
        public List<UnitTypes> StargateUnits;

        public List<UnitTypes> BarracksUnits;
        public List<UnitTypes> FactoryUnits;
        public List<UnitTypes> StarportUnits;

        Race ActualRace;

        public BuildManager(BuildOptions buildOptions)
        {
            BuildOptions = buildOptions;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var playerInfo in gameInfo.PlayerInfo)
            {
                if (playerInfo.PlayerId == playerId)
                {
                    ActualRace = playerInfo.RaceActual;
                }
            }
            SetupUnits(ActualRace);
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            return new List<SC2APIProtocol.Action>();
        }

        void SetupUnits(Race race)
        {
            Units = new List<UnitTypes>();

            if (race == Race.Protoss)
            {
                NexusUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PROBE, UnitTypes.PROTOSS_MOTHERSHIP };
                GatewayUnits = new List<UnitTypes> { UnitTypes.PROTOSS_ZEALOT, UnitTypes.PROTOSS_STALKER, UnitTypes.PROTOSS_SENTRY, UnitTypes.PROTOSS_ADEPT, UnitTypes.PROTOSS_HIGHTEMPLAR, UnitTypes.PROTOSS_DARKTEMPLAR };
                RoboticsFacilityUnits = new List<UnitTypes> { UnitTypes.PROTOSS_OBSERVER, UnitTypes.PROTOSS_IMMORTAL, UnitTypes.PROTOSS_WARPPRISM, UnitTypes.PROTOSS_COLOSSUS, UnitTypes.PROTOSS_DISRUPTOR };
                StargateUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PHOENIX, UnitTypes.PROTOSS_ORACLE, UnitTypes.PROTOSS_VOIDRAY, UnitTypes.PROTOSS_TEMPEST, UnitTypes.PROTOSS_CARRIER };

                Units.AddRange(NexusUnits);
                Units.AddRange(GatewayUnits);
                Units.AddRange(RoboticsFacilityUnits);
                Units.AddRange(StargateUnits);
                Units.Add(UnitTypes.PROTOSS_ARCHON);
            }
        }
    }
}
