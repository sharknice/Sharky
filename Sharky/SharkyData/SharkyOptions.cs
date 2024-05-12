namespace Sharky
{
    public class SharkyOptions
    {
        public bool Debug { get; set; }

        /// <summary>
        /// Show micro task assigned to the unit
        /// </summary>
        public bool DebugMicroTaskUnits { get; set; } = false;

        /// <summary>
        /// Debug creep spread map
        /// </summary>
        public bool DebugCreep { get; set; } = false;

        public float FramesPerSecond { get; set; }

        public TagOptions TagOptions { get; set; }

        public bool LogPerformance { get; set; }
        public bool GameStatusReportingEnabled { get; set; }

        /// <summary>
        /// generate pathing if it isn't included in the StaticData folder
        /// may take a couple minutes depending on the map and CPU
        /// without the pathing units may get stuck outside a walled off enemy base
        /// generated paths are in the data/pathing folder and can be zipped and added to staticdata/pathing
        /// </summary>
        public bool GeneratePathing { get; set; }
        /// <summary>
        /// Using (5, 10) over (10, 10) will result in higher precision pathing but 5X larger files which may be too large to upload to the ladder without donor status
        /// </summary>
        public (int, int) GeneratePathingPrecision { get; set; }

        public string ChatApiUrl { get; set; }
        public bool ApiChatEnabled { get; set; }
        public bool ApiChatOnlyUpdateEnabled { get; set; }
        public bool ControlCamera { get; set; }
    }
}
