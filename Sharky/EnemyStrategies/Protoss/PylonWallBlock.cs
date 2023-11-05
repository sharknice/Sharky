namespace Sharky.EnemyStrategies.Protoss
{
    public class PylonWallBlock : EnemyStrategy
    {
        WallService WallService;
        MapData MapData;

        Vector2 BlockLocation;
        bool GotWall;

        public PylonWallBlock(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            WallService = defaultSharkyBot.WallService;
            MapData = defaultSharkyBot.MapData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (!GotWall)
            {
                GetWall();
                GotWall = true;
            }

            if (frame > SharkyOptions.FramesPerSecond * 60 * 5) { return false; }
            if (BlockLocation == Vector2.Zero) { return false; }

            if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Position, BlockLocation) < 49))
            {
                return true;
            }

            return false;
        }

        void GetWall()
        {
            var baseLocation = WallService.GetBaseLocation();
            if (baseLocation == null) { return; }

            if (MapData?.WallData != null)
            {
                var data = MapData.WallData.FirstOrDefault(d => d.BasePosition.X == baseLocation.X && d.BasePosition.Y == baseLocation.Y);
                if (data?.Pylons != null)
                {
                    BlockLocation = data.Pylons.FirstOrDefault().ToVector2();
                }
            }
        }
    }
}
