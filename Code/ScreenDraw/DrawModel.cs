using System.Windows.Ink;

namespace RecordWin.Code.ScreenDraw
{
    public enum DrawMode
    {
        None = 0,
        Select,
        Pen,
        Text,
        Line,
        Arrow,
        Rectangle,
        Circle,
        Ray,
        Erase
    }
    public enum StrokesHistoryNodeType
    {
        Removed,
        Added
    }
    internal class StrokesHistoryNode
    {
        public StrokeCollection Strokes { get; private set; }
        public StrokesHistoryNodeType Type { get; private set; }

        public StrokesHistoryNode(StrokeCollection strokes, StrokesHistoryNodeType type)
        {
            Strokes = strokes;
            Type = type;
        }
    }
}
