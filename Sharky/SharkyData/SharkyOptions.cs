namespace Sharky
{
    public class SharkyOptions
    {
        public bool Debug { get; set; }
        public float FramesPerSecond { get; set; }
        public bool TagsEnabled { get; set; }
        public bool TagsAllChat { get; set; }
        public bool BuildTagsEnabled { get; set; }
        public bool LogPerformance { get; set; }
        public bool GameStatusReportingEnabled { get; set; }

        /// <summary>
        /// generate pathing if it isn't included in the StaticData folder
        /// may take a couple minutes depending on the map and CPU
        /// without the pathing units may get stuck outside a walled off enemy base
        /// </summary>
        public bool GeneratePathing { get; set; }

        public string ChatApiUrl { get; set; }
        public bool ApiChatEnabled { get; set; }
    }
}
