using System;

namespace GameOfLife.Common.Utils
{
    public class StartCell : IEquatable<StartCell>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public StartCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(StartCell other) => X == other.X && Y == other.Y;
    }
}
