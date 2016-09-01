using System;
using System.Collections.Generic;
using System.Timers;
using GameOfLife.Common.Enums;
using GameOfLife.Common.Utils;

namespace GameOfLife.Common
{
    public class Game
    {
        #region public
        public Game(int width, int height, int updateInterval, IEnumerable<StartCell> startCells)
        {
            Width = width;
            Height = height;
            _oldField = new CellState[Width + 2, Height + 2];
            _newField = new CellState[Width + 2, Height + 2];
            foreach (var startCell in startCells)
            {
                _oldField[startCell.X, startCell.Y] = CellState.Alive;
            }
            _stepTimer = new Timer(updateInterval) {Enabled = false};
            _stepTimer.Elapsed += Update;
        }

        

        public void Start()
        {
            lock (_stateLock)
            {
                if (_started || _ended)
                    return;

                _started = true;
                _paused = false;
            }

            Updated?.Invoke(this, new GameUpdateEventArgs(_oldField, Step, AliveCount));
            _stepTimer.Start();
        }

        public void Pause()
        {
            lock (_stateLock)
            {
                if (_paused || !_started || _ended)
                    return;
                _paused = true;
            }

            _stepTimer.Enabled = false;
        }

        public void Resume()
        {
            lock (_stateLock)
            {
                if (! _paused || !_started || _ended)
                    return;
                _paused = false;
            }

            _stepTimer.Enabled = true;
        }

        public int Width { get; }
        public int Height { get; }
        public int UpdatePeriod { get; private set; }
        public int Step { get; private set; }
        public int AliveCount { get; private set; }
        public event EventHandler<GameUpdateEventArgs> Updated;
        public event EventHandler Ended; 
        #endregion

        #region private

        private void Update(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            AliveCount = 0;
            for (var i = 1; i < Width + 1; ++i)
            {
                for (var j = 1; j < Height + 1; ++j)
                {
                    var neighborCount = GetNeighborCount(i, j);
                    var newState = GetNewState(_oldField[i, j], neighborCount);
                    if (newState == CellState.Alive)
                        AliveCount++;
                    _newField[i, j] = newState;
                }
            }

            if (AliveCount == 0)
            {
                Ended?.Invoke(this, new EventArgs());
                _ended = true;
                _stepTimer.Enabled = false;
                
            }

            CopyNewFieldToOld();
            Step++;
            Updated?.Invoke(this, new GameUpdateEventArgs(_oldField, Step, AliveCount));
        }

        private void CopyNewFieldToOld()
        {
            for (var i = 1; i < Width + 1; ++i)
            {
                for (var j = 1; j < Height + 1; ++j)
                {
                    _oldField[i, j] = _newField[i, j];
                }
            }
        }

        private CellState GetNewState(CellState currentState, int neighborCount)
        {
            if (currentState == CellState.Alive)
            {
                if (neighborCount == 2 || neighborCount == 3)
                    return CellState.Alive;
            }
            else if (currentState == CellState.Dead)
            {
                if (neighborCount == 3)
                    return CellState.Alive;
            }

            return CellState.Dead;
        }

        private int GetNeighborCount(int x, int y)
        {
            var count = 0;
            for (var i = x - 1; i <= x + 1; i++)
            {
                for (var j = y - 1; j <= y + 1; ++j)
                {
                    if (i == x && j == y)
                        continue;
                    if (_oldField[i, j] == CellState.Alive)
                        ++count;
                }
            }
            return count;
        }

        private bool _started;
        private bool _paused;
        private bool _ended;
        private readonly Timer _stepTimer;
        private readonly object _stateLock = new object();
        private readonly CellState[,] _oldField;
        private readonly CellState[,] _newField;

        #endregion
    }
}
