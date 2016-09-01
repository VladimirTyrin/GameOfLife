using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GameOfLife.Common;
using GameOfLife.Common.Enums;
using GameOfLife.Common.Utils;
using ITCC.Logging.Core;
using ITCC.UI.Loggers;
using ITCC.UI.Windows;

namespace GameOfLife.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Brush AliveBrush = Brushes.Green;
        private static readonly Brush DeadBrush = Brushes.Gray;
        private int _width;
        private int _height;
        private int _updateInterval;
        private Game _game;
        private bool _started;
        private bool _paused;
        private Label[,] _cellRectangles;
        private LogWindow _logWindow;
        private List<StartCell> _startCells;

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            StartLog();
#endif

            InitField(30, 30, 200, "StartCells.txt");
        }

        private void StartLog()
        {
            Logger.Level = LogLevel.Trace;
            var observableLogger = new ObservableLogger(1000, App.RunOnUiThread);
            Logger.RegisterReceiver(observableLogger);
            _logWindow = new LogWindow(observableLogger);
            _logWindow.Show();
            Logger.LogEntry("GAME", LogLevel.Info, "Game started");
            Activate();
        }

        private void InitField(int width, int height, int updateInterval, string fileName)
        {
            _width = width;
            _height = height;
            _updateInterval = updateInterval;

            var startCells = ReadStartCells(fileName);
            if (startCells == null)
            {
                MessageBox.Show("Error reading start cells");
                _startCells = new List<StartCell>();
            }
            else
            {
                _startCells = startCells;
            }

            for (var i = 0; i < height; ++i)
                FieldGrid.RowDefinitions.Add(new RowDefinition());

            for (var i = 0; i < width; ++i)
                FieldGrid.ColumnDefinitions.Add(new ColumnDefinition());
            _cellRectangles = new Label[height, width];
            for (var i = 0; i < height; ++i)
            {
                for (var j = 0; j < width; ++j)
                {
                    _cellRectangles[i, j] = new Label
                    {
                        Margin = new Thickness(0.5)
                    };
                    FieldGrid.Children.Add(_cellRectangles[i, j]);
                    Grid.SetRow(_cellRectangles[i, j], i);
                    Grid.SetColumn(_cellRectangles[i, j], j);
                    _cellRectangles[i, j].Background = _startCells.Any(sc => sc.X == i && sc.Y == j)
                            ? AliveBrush
                            : DeadBrush;
                    var iCopy = i + 1;
                    var jCopy = j + 1;
                    _cellRectangles[i, j].MouseLeftButtonUp += (sender, args) =>
                    {
                        App.RunOnUiThread(() =>
                        {
                            if (_started)
                            {
                                _game.ToggleCell(iCopy, jCopy);
                                _cellRectangles[iCopy - 1, jCopy - 1].Background = _cellRectangles[iCopy - 1, jCopy - 1].Background.Equals(DeadBrush)
                                    ? AliveBrush
                                    : DeadBrush;
                                return;
                            }
                            var startCell = _startCells.FirstOrDefault(sc => sc.X == iCopy && sc.Y == jCopy);
                            if (startCell == null)
                            {
                                _startCells.Add(new StartCell(iCopy, jCopy));
                                _cellRectangles[iCopy - 1, jCopy - 1].Background = AliveBrush;
                            }
                            else
                            {
                                _startCells.Remove(startCell);
                                _cellRectangles[iCopy - 1, jCopy - 1].Background = DeadBrush;
                            }
                            
                        });
                    };
                }
            }
        }

        private List<StartCell> ReadStartCells(string fileName)
        {
            try
            {
                var result = new List<StartCell>();
                using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            var parts = line.Split();
                            if (parts.Length != 2)
                                return null;

                            int x;
                            int y;
                            if (!int.TryParse(parts[0], out x))
                                return null;
                            if (!int.TryParse(parts[1], out y))
                                return null;

                            result.Add(new StartCell(x, y));
                            Logger.LogEntry("INIT", LogLevel.Debug, $"Got start cell {x} {y}");
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private void ShowEnded(object sender, EventArgs eventArgs)
        {
            App.RunOnUiThread(() =>
            {
                DiedLabel.Visibility = Visibility.Visible;
                Logger.LogEntry("GAME", LogLevel.Info, "Game ended");
            });
        }

        private void DrawField(object sender, GameUpdateEventArgs gameUpdateEventArgs)
        {
            Logger.LogEntry("GAME", LogLevel.Debug, $"Step {gameUpdateEventArgs.Step}");
            App.RunOnUiThread(() =>
            {
                AliveLabel.Content = $"Alive: {gameUpdateEventArgs.AliveCount}";
                StepLabel.Content = $"Step: {gameUpdateEventArgs.Step}";
                for (var i = 0; i < _height; ++i)
                {
                    for (var j = 0; j < _width; ++j)
                    {
                        _cellRectangles[i, j].Background = gameUpdateEventArgs.State[i, j] == CellState.Alive
                            ? AliveBrush
                            : DeadBrush;
                    }
                }
            });
        }

        private void ToggleStateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_started)
            {
                _started = true;
                _game = new Game(_width, _height, _updateInterval, _startCells);
                _game.Updated += DrawField;
                _game.Ended += ShowEnded;
                _game.Start();
                ToggleStateButton.Content = "Pause";
                return;
            }

            if (_paused)
            {
                ToggleStateButton.Content = "Pause";
                _game.Resume();
            }
            else
            {
                ToggleStateButton.Content = "Play";
                _game.Pause();
            }
            _paused = !_paused;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            try
            {
                _logWindow.Close();
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}
