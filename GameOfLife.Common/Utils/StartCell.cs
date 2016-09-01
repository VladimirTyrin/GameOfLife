namespace GameOfLife.Common.Utils
{
    public struct StartCell
    {
        public int X { get; set; }
        public int Y { get; set; }

        public StartCell(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
