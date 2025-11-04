using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;


namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für EngineValueGraphUserControl.xaml
    /// </summary>
    public partial class EngineValueGraphUserControl : UserControl
    {
        private  Dictionary<string, Dictionary<int, decimal>> _valuesPerEngine = new Dictionary<string, Dictionary<int, decimal>>();
        private readonly Dictionary<string, int> _engineToColor = new Dictionary<string, int>();
        private readonly List<Color> _colors = new List<Color>();
        private decimal _minMoveValue = 0;
        private decimal _maxMoveValue = 0;
        private int _valuesCount = 0;
        private Configuration _configuration = null;
        private double  _topDownLength;
        private double _middleLineLength;
        private double _midTopPosition;
        private const decimal MaxValueLimit = 9;
        private FileLogger _fileLogger;
        private bool _showValueText = true;
        private bool _waitingForNextMove;
        private int _moveIndex = 0;

        public EngineValueGraphUserControl()
        {
            InitializeComponent();
            _colors.Add(Colors.DarkGoldenrod);
            _colors.Add(Colors.Black);
            _colors.Add(Colors.CornflowerBlue);
            _colors.Add(Colors.Coral);
            _colors.Add(Colors.Green);
        }

        public void SetConfiguration(Configuration configuration, FileLogger fileLogger)
        {
            _configuration = configuration;
            _fileLogger = fileLogger;
        }

        public void ClearAll()
        {
            _valuesPerEngine.Clear();
            _engineToColor.Clear();
            _minMoveValue = 0;
            _maxMoveValue = 0;
            _valuesCount = 0;
            _moveIndex = 0;
            _waitingForNextMove = false;
            DrawValues();
        }

        public void NewGame()
        {
            _minMoveValue = 0;
            _maxMoveValue = 0;
            _valuesCount = 0;
            _moveIndex = 0;
            _waitingForNextMove = false;
            var dictionaryKeys = _valuesPerEngine.Keys;
            _valuesPerEngine = new Dictionary<string, Dictionary<int, decimal>>();
            foreach (var engineName in dictionaryKeys)
            {
                _valuesPerEngine.Add(engineName, new Dictionary<int, decimal>());
            }
        }


        public void ShowValueText(bool showValueText)
        {
            _showValueText = showValueText;
        }

        public void RemoveEngineName(string engineName)
        {
            _valuesPerEngine.Remove(engineName);
            _engineToColor.Remove(engineName);
            _minMoveValue = 0;
            _maxMoveValue = 0;
            _valuesCount = 0;
            foreach (var dictionaryKey in _valuesPerEngine.Keys)
            {
                var maxValueIndex = _valuesPerEngine[dictionaryKey].Max(v => v.Key);
                if (maxValueIndex > _valuesCount)
                {
                    _valuesCount = maxValueIndex;
                }

                foreach (var moveValue in _valuesPerEngine[dictionaryKey])
                {
                    if (moveValue.Value > _maxMoveValue)
                    {
                        _maxMoveValue = moveValue.Value;
                    }

                    if (moveValue.Value < _minMoveValue)
                    {
                        _minMoveValue = moveValue.Value;
                    }
                }
            }
            AdjustValues();
            DrawValues();
        }

        public void AddEngineName(string engineName, int color)
        {
            if (!_valuesPerEngine.ContainsKey(engineName))
            {
                _valuesPerEngine.Add(engineName, new Dictionary<int, decimal>());
                _engineToColor.Add(engineName, color);
            }
        }

        public void BestMoveBy(string engineName)
        {
            if (!_engineToColor.ContainsKey(engineName))
            {
                return;
            }

            if (_engineToColor[engineName] == Fields.COLOR_WHITE)
            {
                _waitingForNextMove = true;

            }
            if (_engineToColor[engineName] == Fields.COLOR_BLACK)
            {
                if (_waitingForNextMove)
                {
                    _moveIndex++;
                    _waitingForNextMove = false;
                }
            }

        }

        public void AddValue(string engineName, decimal moveValue)
        {

            if (!_valuesPerEngine.TryGetValue(engineName, out var valueList))
            {
                return;
            }

            if (moveValue > MaxValueLimit)
            {
                moveValue = MaxValueLimit;
            }

            if (moveValue < -MaxValueLimit)
            {
                moveValue = -MaxValueLimit;
            }

            valueList[_moveIndex] = moveValue;

            _maxMoveValue = 0;
            _minMoveValue = 0;
            foreach (var keyValuePair in _valuesPerEngine)
            {
                if (keyValuePair.Value.Count > 0)
                {
                    var aMaxValue = keyValuePair.Value.Max(v => v.Value);
                    var aMinValue = keyValuePair.Value.Min(v => v.Value);
                    if (aMaxValue > _maxMoveValue)
                    {
                        _maxMoveValue = aMaxValue;
                    }

                    if (aMinValue < _minMoveValue)
                    {
                        _minMoveValue = aMinValue;
                    }
                }
            }

            if (_moveIndex > _valuesCount)
            {
                _valuesCount = _moveIndex;
            }

            AdjustValues();
            Dispatcher?.Invoke(DrawValues);

        }

        private void AdjustValues()
        {
            if (_maxMoveValue < 2)
            {
                _maxMoveValue = 2;
            }
            if (_minMoveValue > -2)
            {
                _minMoveValue = -2;
            }

            if (_maxMoveValue > MaxValueLimit)
            {
                _maxMoveValue = MaxValueLimit;
            }
            if (_minMoveValue < -MaxValueLimit)
            {
                _minMoveValue = -MaxValueLimit;
            }
            if (Math.Abs(_minMoveValue) > _maxMoveValue)
            {
                _maxMoveValue = Math.Abs(_minMoveValue);
            }
            else
            {
                _minMoveValue = -_maxMoveValue;
            }

            if (_valuesCount == 0)
            {
                _valuesCount = 1;
            }
            _valuesCount = (int)(Math.Ceiling(_valuesCount / 10.0d) * 10);
        }

        private void DrawScaleLines()
        {
            var lineTopDown = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 0.2,
                Width = 2,
                Height = _topDownLength,
                Fill = Brushes.DarkGray
            };
            var lineMiddle = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 0.2,
                Width = _middleLineLength,
                Height = 1,
                Fill = Brushes.Blue
            };
            canvas.Children.Add(lineTopDown);
            canvas.Children.Add(lineMiddle);
            Canvas.SetTop(lineTopDown, 0);
            Canvas.SetLeft(lineTopDown, 1);
            Canvas.SetTop(lineMiddle, _midTopPosition);
            Canvas.SetLeft(lineMiddle, 1);
            for (var i = 1; i <= _maxMoveValue; i++)
            {
                var lineY = new Rectangle
                {
                    Stroke = Brushes.Green,
                    StrokeThickness = 0.4,
                    Width = 6,
                    Height = 2,
                    Fill = Brushes.Green
                };
                canvas.Children.Add(lineY);
                var topPosition = _midTopPosition - (_midTopPosition / Decimal.ToDouble(_maxMoveValue) * (i));
                Canvas.SetTop(lineY, topPosition);
                Canvas.SetLeft(lineY, 1);
            }
            for (var i = 1; i <= Math.Abs(_minMoveValue); i++)
            {
                var lineY = new Rectangle
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 0.4,
                    Width = 6,
                    Height = 2,
                    Fill = Brushes.Red
                };
                canvas.Children.Add(lineY);
                var topPosition = _midTopPosition + (_midTopPosition / Math.Abs(Decimal.ToDouble(_minMoveValue)) * Math.Abs(i));
                Canvas.SetTop(lineY, topPosition);
                Canvas.SetLeft(lineY, 1);
            }
        }

        public void DrawValues()
        {
            _topDownLength = this.Height;
            _middleLineLength = this.Width - 10 + (_valuesCount * 10);
            _middleLineLength = this.Width - 10 ;
            _midTopPosition = _topDownLength / 2;
            canvas.Children.Clear();
            DrawScaleLines();
            int cIndex = 2;
            foreach (var dictionaryKey in _valuesPerEngine.Keys.OrderBy(k => k))
            {
                var colorIndex = 0;
                if (_engineToColor[dictionaryKey] == Fields.COLOR_WHITE)
                {
                    colorIndex = 0;
                } else if (_engineToColor[dictionaryKey] == Fields.COLOR_BLACK)
                {
                    colorIndex = 1;
                }
                else
                {
                    if (cIndex >= _colors.Count)
                    {
                        cIndex = 2;
                    }
                    colorIndex = cIndex;
                    cIndex++;
                }

                if (_valuesPerEngine[dictionaryKey].Keys.Count == 0)
                {
                    continue;
                }
                var maxValueIndex = _valuesPerEngine[dictionaryKey].Max(v => v.Key);
                var minValueIndex = _valuesPerEngine[dictionaryKey].Min(v => v.Key);

                var xStart = 2 + (_middleLineLength / _valuesCount * (minValueIndex));
                xStart += colorIndex * 3;
                var yStart = _midTopPosition;
                int ellipseCount = 0;
                for (var i = minValueIndex; i <= maxValueIndex; i++)
                {
                    if (!_valuesPerEngine[dictionaryKey].ContainsKey(i))
                    {
                        continue;
                    }
                    var aValue = _valuesPerEngine[dictionaryKey][i];
                    var lineY = new Line
                    {
                        Stroke = new SolidColorBrush(_colors[colorIndex]),
                        StrokeThickness = 2,
                        X1 = xStart,
                        Y1 = yStart,
                        X2 =  2 + (_middleLineLength / _valuesCount * (i)),
                        Y2 = aValue > 0
                            ? _midTopPosition - (_midTopPosition / Decimal.ToDouble(_maxMoveValue) * Decimal.ToDouble(aValue))
                            : _midTopPosition + (_midTopPosition / Decimal.ToDouble(Math.Abs(_minMoveValue)) * Decimal.ToDouble( Math.Abs(aValue)))
                    };
                   
                    lineY.X2 += colorIndex * 3;
                    xStart = lineY.X2;
                    yStart = lineY.Y2;
                    canvas.Children.Add(lineY);
                    var ellipse = new Ellipse
                    {
                        Stroke = new SolidColorBrush(_colors[colorIndex]),
                        StrokeThickness = 1,
                        Width = 3, Height = 3,
                        Fill = new SolidColorBrush(_colors[colorIndex])
                    };
                    Canvas.SetTop(lineY, 0);
                    Canvas.SetLeft(lineY, 1);
                    canvas.Children.Add(ellipse);
                    Canvas.SetTop(ellipse, lineY.Y2);
                    Canvas.SetLeft(ellipse, lineY.X2);
                    ellipseCount++;
                    var drawText = _valuesCount < 20;
                    if (!drawText)
                    {
                        drawText =  _valuesCount<30  && ellipseCount % 2 == 0;
                    }
                    if (!drawText)
                    {
                        drawText = _valuesCount < 40 && ellipseCount % 3 == 0;
                    }
                    if (!drawText)
                    {
                        drawText = ellipseCount % 4 == 0;
                    }

                    if (!drawText || !_showValueText)
                    {
                        continue;
                    }
                    var aValueText = Math.Round(aValue, 1, MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture);
                    var textBlock = new TextBlock
                    {
                        Text = aValue <= 9
                            ? aValueText
                            : $">{aValueText}",
                        Foreground = new SolidColorBrush(_colors[colorIndex])
                    };
                    Canvas.SetLeft(textBlock, lineY.X2);
                    Canvas.SetTop(textBlock, lineY.Y2);
                    canvas.Children.Add(textBlock);
                }
            }
        }
    }
}
