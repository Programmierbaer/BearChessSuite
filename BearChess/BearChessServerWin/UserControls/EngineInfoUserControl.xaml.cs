using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows.Controls;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;

namespace www.SoLaNoSoft.com.BearChessServerWin.UserControls
{
    /// <summary>
    /// Interaktionslogik für EngineInfoUserControl.xaml
    /// </summary>
    public partial class EngineInfoUserControl : UserControl
    {
        private readonly ConcurrentQueue<string> _infoLine = new ConcurrentQueue<string>();

        private readonly List<EngineInfoLineUserControl> _engineInfoLineUserControls =
            new List<EngineInfoLineUserControl>();

        private bool _stopInfo;
        private bool _showForWhite;
        private int _currentColor;
        private readonly ResourceManager _rm;
        private bool _moveAdded;
        private readonly List<string> _allMoves = new List<string>();
        private DisplayFigureType _figureType = DisplayFigureType.Symbol;
        private DisplayMoveType _moveType = DisplayMoveType.FromToField;
        private DisplayCountryType _countryType = DisplayCountryType.DE;
        private string _fenPosition = string.Empty;


        public int Color { get; set; }

        public EngineInfoUserControl()
        {
            InitializeComponent();
            _rm = SpeechTranslator.ResourceManager;
            var thread = new Thread(ShowInfoLine) { IsBackground = true };
            thread.Start();
        }

        public void SetEngineName(string engineName)
        {
            textBlockName.Text = engineName;
        }

        public void ShowInfo(string infoLine, string fenPosition)
        {
            _fenPosition = fenPosition;
            _infoLine.Enqueue(infoLine);
        }

        public void ClearQueue()
        {
            while (_infoLine.TryDequeue(out string _)) ;
        }

        private void ShowInfoLine()
        {
            var fastChessBoard = new FastChessBoard();
            while (true)
            {
                if (!_stopInfo && _infoLine.TryDequeue(out var infoLine))
                {
                    if (string.IsNullOrWhiteSpace(infoLine))
                    {
                        continue;
                    }

                    var scoreString = string.Empty;
                    var moveLine = string.Empty;
                    var readingMoveLine = false;
                    var currentMove = string.Empty;

                    var currentMultiPv = 1;
                    var infoLineParts = infoLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    for (var i = 0; i < infoLineParts.Length; i++)
                    {
                        if (infoLineParts[i].Equals("depth", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (infoLineParts[i].Equals("multipv", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (infoLineParts[i].Equals("seldepth", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (infoLineParts[i].Equals("score", StringComparison.OrdinalIgnoreCase))
                        {
                            var scoreType = infoLineParts[i + 1];
                            if (scoreType.Equals("cp", StringComparison.OrdinalIgnoreCase))
                            {
                                scoreString = infoLineParts[i + 2];
                                if (decimal.TryParse(scoreString, NumberStyles.Any, CultureInfo.CurrentCulture,
                                        out var score))
                                {
                                    score = score / 100;
                                    if (_showForWhite && Color == Fields.COLOR_BLACK)
                                    {
                                        score = score * -1;
                                    }

                                    if (_showForWhite && Color == Fields.COLOR_EMPTY &&
                                        _currentColor == Fields.COLOR_BLACK)
                                    {
                                        score = score * -1;
                                    }

                                    scoreString = $"{score:0.00}".Replace(",", ".");
                                }

                                continue;
                            }

                            if (scoreType.Equals("mate", StringComparison.OrdinalIgnoreCase))
                            {
                                var infoLinePart = infoLineParts[i + 2];
                                if (!infoLinePart.Equals("0"))
                                {
                                    scoreString = $"{_rm.GetString("MateIn")} {infoLinePart}";
                                }
                            }

                            continue;
                        }

                        if (infoLineParts[i].Equals("pv", StringComparison.OrdinalIgnoreCase))
                        {
                            readingMoveLine = true;
                            continue;
                        }

                        if (infoLineParts[i].Equals("currmove", StringComparison.OrdinalIgnoreCase))
                        {
                            currentMove = $"{_rm.GetString("Current")} {infoLineParts[i + 1]}";
                            readingMoveLine = false;
                            continue;
                        }

                        if (infoLineParts[i].Equals("currmovenumber", StringComparison.OrdinalIgnoreCase))
                        {
                            currentMove += $" ({infoLineParts[i + 1]})";
                            readingMoveLine = false;
                            continue;
                        }


                        if (readingMoveLine)
                        {
                            moveLine += infoLineParts[i] + " ";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(scoreString))
                    {
                        continue;
                    }
                    try
                    {
                        Dispatcher?.Invoke(() =>
                        {
                            if (currentMultiPv == 1)
                            {
                                try
                                {
                                    if (_moveAdded)
                                    {
                                        engineInfoLineUserControl1.FillLine(scoreString, string.Empty);
                                        _moveAdded = false;
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(_fenPosition))
                                        {
                                            fastChessBoard.Init(_fenPosition, _allMoves.ToArray());

                                            fastChessBoard.SetDisplayTypes(_figureType, _moveType, _countryType);

                                            var ml = string.Empty;
                                            var strings = moveLine.Split(" ".ToCharArray());
                                            try
                                            {
                                                foreach (var s in strings)
                                                {
                                                    ml = ml + " " + fastChessBoard.GetMove(s);
                                                }
                                            }
                                            catch
                                            {
                                                if (string.IsNullOrWhiteSpace(ml))
                                                {
                                                    ml = moveLine;
                                                }
                                                //
                                            }

                                            engineInfoLineUserControl1.FillLine(scoreString, ml);
                                        }
                                    }
                                }
                                catch
                                {
                                    engineInfoLineUserControl1.FillLine(scoreString, moveLine);
                                }
                            }
                            else
                            {
                                var multiPv = currentMultiPv - 2;
                                if (_engineInfoLineUserControls.Count > 0 &&
                                    _engineInfoLineUserControls.Count >= multiPv && multiPv >= 0)
                                {
                                    if (_moveAdded)
                                    {
                                        engineInfoLineUserControl1.FillLine(scoreString, string.Empty);
                                        _moveAdded = false;
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(_fenPosition))
                                        {
                                            fastChessBoard.Init(_fenPosition, _allMoves.ToArray());

                                            fastChessBoard.SetDisplayTypes(_figureType, _moveType, _countryType);
                                            try
                                            {
                                                var ml = string.Empty;
                                                var strings = moveLine.Split(" ".ToCharArray());
                                                try
                                                {
                                                    foreach (var s in strings)
                                                    {
                                                        ml = ml + " " + fastChessBoard.GetMove(s);
                                                    }
                                                }
                                                catch
                                                {
                                                    if (string.IsNullOrWhiteSpace(ml))
                                                    {
                                                        ml = moveLine;
                                                    }
                                                    //
                                                }

                                                _engineInfoLineUserControls[multiPv].FillLine(scoreString, ml);
                                            }
                                            catch
                                            {
                                                _engineInfoLineUserControls[multiPv].FillLine(scoreString, moveLine);
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    }
                    catch
                    {
                        //
                    }
                }

                Thread.Sleep(10);
            }
        }
    }
}