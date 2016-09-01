using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameOfLife.Common.Enums;

namespace GameOfLife.Common.Utils
{
    public class GameUpdateEventArgs
    {
        public GameUpdateEventArgs(CellState[,] state, int step, int aliveCount)
        {
            State = state;
            Step = step;
            AliveCount = aliveCount;
        }

        public CellState[,] State { get; }
        public int Step { get; }
        public int AliveCount { get; }
    }
}
