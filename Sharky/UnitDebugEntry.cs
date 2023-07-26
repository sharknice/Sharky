namespace Sharky
{
    public class UnitDebugEntry
    {
        public int LastFrameUpdate { get; set; } = 0;
        public string Text { get; set; }
        public UnitCalculation UnitCalculation { get; set; }
        public Color Color { get; set; }
        public uint Size { get; set; }

        public UnitDebugEntry(int currentFrame, string text, UnitCalculation unitCalculation, Color color, uint size = 12)
        {
            LastFrameUpdate = currentFrame;
            Text = text;
            UnitCalculation = unitCalculation;
            Color = color;
            Size = size;
        }

        public void Draw(DebugService debugService)
        {
            debugService.DrawText(Text, UnitCalculation.Unit.Pos, Color, Size);
        }
    }
}
