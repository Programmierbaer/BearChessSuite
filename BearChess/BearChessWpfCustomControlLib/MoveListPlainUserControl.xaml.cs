﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessTools;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{

    public class SelectedMoveOfMoveList
    {

        public int MoveNumber { get; }
        public Move Move { get; }
        public SelectedMoveOfMoveList(Move move, int moveNumber)
        {
            MoveNumber = moveNumber;
            Move = move;
        }

    }
    /// <summary>
    /// Interaktionslogik für MoveListPlainUserControl.xaml
    /// </summary>
    public partial class MoveListPlainUserControl : UserControl, IMoveListPlainWindow
    {
        private Configuration _configuration;
        private PgnConfiguration _pgnConfiguration;
        private int _lastMoveNumber;
        private WrapPanel _wrapPanel;
        private DisplayFigureType _figureType;
        private DisplayMoveType _moveType;
        private DisplayCountryType _countryType;
        private List<Move> _moveList = new List<Move>();
        private bool _newPanelAdded = false;
        private MovePlainUserControl _movePlainUserControl;
        private bool _showOnlyMoves;
        private bool _showFullInfo;
        private bool _showComments;
        private bool _showForBuddy;
        private double _fontSize;
        private int _lastMarkedMoveNumber;
        private int _lastMarkedColor;
        private bool _showForWhite;
        private FontFamily _fontFamily;
        private CurrentGame _currentGame;
        private PgnGame _pgnGame;
        private string _gameStartPosition;
        private ResourceManager _rm;

        public event EventHandler<SelectedMoveOfMoveList> SelectedMoveChanged;
        public event EventHandler<SelectedMoveOfMoveList> ContentChanged;
        public event EventHandler<SelectedMoveOfMoveList> RestartEvent;

        public MoveListPlainUserControl()
        {
            InitializeComponent();
            _rm = SpeechTranslator.ResourceManager;
            _fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Chess Merida");
            _fontSize = Configuration.Instance.GetDoubleValue("MoveListPlainWindowFontSize", "18");
            textBlockBlack.FontFamily = _fontFamily;
            textBlockBlack.Text = FontConverter.ConvertFont("k", "Chess Merida");
            textBlockWhite.FontFamily = _fontFamily;
            textBlockWhite.Text = FontConverter.ConvertFont("K", "Chess Merida");
        }

        public void SetServerConfiguration(Configuration configuration)
        {
            _configuration = configuration;
            _pgnConfiguration = configuration.GetPgnConfiguration();
            _fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Chess Merida");
            _fontSize = Configuration.Instance.GetDoubleValue("MoveListPlainWindowFontSize", "12");
            _lastMoveNumber = 0;

            _figureType = (DisplayFigureType)Enum.Parse(typeof(DisplayFigureType), _configuration.GetConfigValue("DisplayFigureType", DisplayFigureType.Symbol.ToString()));
            _moveType = (DisplayMoveType)Enum.Parse(typeof(DisplayMoveType), _configuration.GetConfigValue("DisplayMoveType", DisplayMoveType.FromToField.ToString()));
            _countryType = (DisplayCountryType)Enum.Parse(typeof(DisplayCountryType), _configuration.GetConfigValue("DisplayCountryType", DisplayCountryType.GB.ToString()));
            _moveList.Clear();
            _showOnlyMoves = true;
            _showFullInfo = false;
            _showComments = false;
            _showForWhite = false;
            _showForBuddy = false;
            textBlockWhitePlayer.Text = string.Empty;
            textBlockBlackPlayer.Text = string.Empty;
            textBlockResult.Text = string.Empty;
            buttonExtend.Visibility = Visibility.Collapsed;
            buttonShowHideComments.Visibility = Visibility.Collapsed;
            textBlockContent.Visibility = Visibility.Collapsed;
            SetContentInfo();
            _currentGame = null;
            _gameStartPosition = string.Empty;
        }

        public void SetConfiguration(Configuration configuration)
        {
            _configuration = configuration;
            _pgnConfiguration = configuration.GetPgnConfiguration();

            _lastMoveNumber = 0;

            _figureType = (DisplayFigureType)Enum.Parse(typeof(DisplayFigureType), _configuration.GetConfigValue("DisplayFigureType", DisplayFigureType.Symbol.ToString()));
            _moveType = (DisplayMoveType)Enum.Parse(typeof(DisplayMoveType), _configuration.GetConfigValue("DisplayMoveType", DisplayMoveType.FromToField.ToString()));
            _countryType = (DisplayCountryType)Enum.Parse(typeof(DisplayCountryType), _configuration.GetConfigValue("DisplayCountryType", DisplayCountryType.GB.ToString()));
            _moveList.Clear();
            _showOnlyMoves = bool.Parse(_configuration.GetConfigValue("extendMoveList", "false"));
            _showFullInfo = bool.Parse(_configuration.GetConfigValue("extendFull", "false"));
            _showComments = bool.Parse(_configuration.GetConfigValue("extendComments", "false"));
            _showForWhite = bool.Parse(_configuration.GetConfigValue("showForWhite", "false"));
            _showForBuddy = bool.Parse(_configuration.GetConfigValue("showForBuddy", "false"));
            textBlockWhitePlayer.Text = string.Empty;
            textBlockBlackPlayer.Text = string.Empty;
            textBlockResult.Text = string.Empty;
            SetContentInfo();
            _currentGame = null;
            _gameStartPosition = string.Empty;

        }

        public void SetPlayerAndResult(CurrentGame currentGame, string gameStartPosition, PgnGame pgnGame)
        {
            _currentGame = currentGame;
            _pgnGame = pgnGame;
            _gameStartPosition = gameStartPosition;
            textBlockWhitePlayer.Text = currentGame.PlayerWhite;
            textBlockBlackPlayer.Text = currentGame.PlayerBlack;
            string result = pgnGame != null ? pgnGame.Result : "*";

            if (result.Contains("/"))
            {
                result = "1/2";
            }

            textBlockResult.Text = result;
        }

        public void SetPlayer(string player, int color)
        {
            if (color == Fields.COLOR_WHITE)
            {
                textBlockWhitePlayer.Text = player;
            }
            else
            {
                textBlockBlackPlayer.Text = player;
            }
        }

        public void SetStartPosition(string gameStartPosition)
        {
            _gameStartPosition = gameStartPosition;
        }

        public void SetResult(string result)
        {
            if (result.Contains("/"))
            {
                result = "1/2";
            }

            textBlockResult.Text = result;
        }
        public void AddMove(Move move)
        {
            _moveList.Add(move);
            AddInternalMove(move);
        }

        public void AddMove(Move move, bool tournamentMode)
        {
            _moveList.Add(move);
            AddInternalMove(move);
        }

        public void Clear()
        {
            stackPanelMoves.Children.Clear();
            _moveList.Clear();
            _lastMoveNumber = 0;
            textBlockWhitePlayer.Text = string.Empty;
            textBlockBlackPlayer.Text = string.Empty;
            textBlockResult.Text = string.Empty;
        }

        public void SetDisplayTypes(DisplayFigureType displayFigureType, DisplayMoveType displayMoveType, DisplayCountryType displayCountryType)
        {
            _figureType = displayFigureType;
            _moveType = displayMoveType;
            _countryType = displayCountryType;
            stackPanelMoves.Children.Clear();
            _lastMoveNumber = 0;
            foreach (var move in _moveList)
            {
                AddInternalMove(move);
            }
        }

        public void SetShowForWhite(bool showForWhite)
        {
            _showForWhite = showForWhite;
            stackPanelMoves.Children.Clear();
            _lastMoveNumber = 0;
            foreach (var move in _moveList)
            {
                AddInternalMove(move);
            }
        }

        public void ClearMark()
        {
            for (int w = 0; w < stackPanelMoves.Children.Count; w++)
            {
                if (stackPanelMoves.Children[w] is WrapPanel wrapPanel1)
                {
                    for (int c = 0; c < wrapPanel1.Children.Count; c++)
                    {
                        if (wrapPanel1.Children[c] is MovePlainUserControl movePlainUserControl)
                        {
                            movePlainUserControl.UnMark();
                        }
                    }
                }
                else
                {
                    if (stackPanelMoves.Children[w] is MovePlainUserControl movePlainUserControl)
                    {
                        movePlainUserControl.UnMark();
                    }
                }
            }
        }

        public void MarkLastMove()
        {
            MarkMove(_lastMarkedMoveNumber, _lastMarkedColor);
        }

        public void MarkMove(int number, int color)
        {
            if (number < 0 || stackPanelMoves.Children.Count < 1)
            {
                return;
            }

            _lastMarkedMoveNumber = number;
            _lastMarkedColor = color;
            for (var w = 0; w < stackPanelMoves.Children.Count; w++)
            {
                if (stackPanelMoves.Children[w] is WrapPanel wrapPanel1)
                {
                    for (var c = 0; c < wrapPanel1.Children.Count; c++)
                    {
                        if (wrapPanel1.Children[c] is MovePlainUserControl movePlainUserControl && movePlainUserControl.CurrentMoveNumber.Equals(number)
                            && movePlainUserControl.CurrentMove.FigureColor.Equals(color))
                        {
                            movePlainUserControl.Mark();
                            movePlainUserControl.BringIntoView();
                            _movePlainUserControl = movePlainUserControl;
                            return;
                        }
                    }
                }
                else
                {
                    if (stackPanelMoves.Children[w] is MovePlainUserControl movePlainUserControl && movePlainUserControl.CurrentMoveNumber.Equals(number)
                        && movePlainUserControl.CurrentMove.FigureColor.Equals(color))
                    {
                        movePlainUserControl.Mark();
                        movePlainUserControl.BringIntoView();
                        _movePlainUserControl = movePlainUserControl;
                        return;
                    }
                }
            }
        }


        public void RemainingMovesFor50MovesDraw(int remainingMoves)
        {
          //
        }

        public void Show()
        {
        }


        public void Close()
        {
        }

        public event EventHandler Closed;
        public double Top
        {
            get;
            set;
        }

        public double Left
        {
            get;
            set;
        }

        private void SetSizes(double top, double left, double width, double height)
        {

            Height = _configuration.GetWinDoubleValue("MoveListPlainWindowHeight", Configuration.WinScreenInfo.Height, SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth,
                                                      (height / 2).ToString(CultureInfo.InvariantCulture));
            Width = _configuration.GetWinDoubleValue("MoveListPlainWindowWidth", Configuration.WinScreenInfo.Width, SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth,
                                                     (width / 2).ToString(CultureInfo.InvariantCulture));
            _fontSize = _configuration.GetDoubleValue("MoveListPlainWindowFontSize", "18");

        }

        private void AddInternalMove(Move move)
        {
            if (_lastMoveNumber == 0)
            {
                _wrapPanel = new WrapPanel();
                stackPanelMoves.Children.Add(_wrapPanel);
            }

            var movePlainUserControl = new MovePlainUserControl(null);
            {
                FontSize = _fontSize;
            }
            ;
            movePlainUserControl.SelectedChanged += MovePlainUserControl_SelectedChanged;
            movePlainUserControl.ContentChanged += MovePlainUserControl_ContentChanged;
            movePlainUserControl.RestartEvent += MovePlainUserControl_RestartEvent;
            movePlainUserControl.SetDisplayTypes(_figureType, _moveType, _countryType);
            movePlainUserControl.SetInformationDetails(_showOnlyMoves, _showFullInfo, _showComments, _showForWhite, _showForBuddy);
            if (move.FigureColor == Fields.COLOR_WHITE || (_lastMoveNumber == 0))
            {
                if (_lastMoveNumber == 0)
                {
                    _newPanelAdded = true;
                }
                _lastMoveNumber++;
            }
            if (!_showOnlyMoves)
            {
                if (!string.IsNullOrWhiteSpace(move.BestLine) || (_showComments && !string.IsNullOrWhiteSpace(move.Comment)))
                {
                    _wrapPanel = new WrapPanel();
                    stackPanelMoves.Children.Add(_wrapPanel);
                    _newPanelAdded = true;
                }
            }

            movePlainUserControl.SetMove(move, _lastMoveNumber, _newPanelAdded, _moveList);
            _wrapPanel.Children.Add(movePlainUserControl);
            _newPanelAdded = false;
            if (!_showOnlyMoves)
            {
                if (!string.IsNullOrWhiteSpace(move.BestLine) || (_showComments && !string.IsNullOrWhiteSpace(move.Comment)))
                {
                    _wrapPanel = new WrapPanel();
                    stackPanelMoves.Children.Add(_wrapPanel);
                    _newPanelAdded = true;
                }
            }
            movePlainUserControl.BringIntoView();
        }


        private void MovePlainUserControl_RestartEvent(object sender, EventArgs e)
        {
            _movePlainUserControl = sender as MovePlainUserControl;
            if (_movePlainUserControl != null)
            {
                RestartEvent?.Invoke(this,
                                       new SelectedMoveOfMoveList(_movePlainUserControl.CurrentMove,
                                                                  _movePlainUserControl.CurrentMoveNumber
                                       ));
            }
        }
        private void MovePlainUserControl_ContentChanged(object sender, EventArgs e)
        {
            _movePlainUserControl = sender as MovePlainUserControl;
            if (_movePlainUserControl != null)
            {
                ContentChanged?.Invoke(this,
                                       new SelectedMoveOfMoveList(_movePlainUserControl.CurrentMove,
                                                                  _movePlainUserControl.CurrentMoveNumber
                                       ));
            }
        }

        private void MovePlainUserControl_SelectedChanged(object sender, EventArgs e)
        {
            _movePlainUserControl = sender as MovePlainUserControl;
            if (_movePlainUserControl != null)
            {
                SelectedMoveChanged?.Invoke(
                    this,
                    new SelectedMoveOfMoveList(_movePlainUserControl.CurrentMove,
                                               _movePlainUserControl.CurrentMoveNumber
                                               ));
            }
        }

        private void StackPanelMoves_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (_movePlainUserControl == null)
            {
                return;
            }
            int wpIndex = -1;
            int ucIndex = -1;
            for (int w = 0; w < stackPanelMoves.Children.Count; w++)
            {
                if (stackPanelMoves.Children[w] is WrapPanel wrapPanel1)
                {
                    for (int c = 0; c < wrapPanel1.Children.Count; c++)
                    {
                        if (wrapPanel1.Children[c] is MovePlainUserControl movePlainUserControl && movePlainUserControl.CurrentMove.Equals(_movePlainUserControl.CurrentMove))
                        {
                            if (c < wrapPanel1.Children.Count)
                            {
                                wpIndex = w;
                                ucIndex = c;
                                break;
                            }
                        }
                    }
                }

                if (ucIndex > -1)
                {
                    break;
                }
            }
            if (e.Key == Key.Right)
            {
                ucIndex++;
            }
            if (e.Key == Key.Left)
            {
                ucIndex--;
            }
            if (e.Key == Key.Up)
            {
                if (_showOnlyMoves)
                {
                    ucIndex -= 4;
                }
                else
                {
                    wpIndex -= 8;
                    ucIndex = 0;
                }
            }
            if (e.Key == Key.Down)
            {
                if (_showOnlyMoves)
                {
                    ucIndex += 4;
                }
                else
                {
                    wpIndex += 8;
                    ucIndex = 0;
                }
            }
            if (e.Key == Key.PageDown)
            {
                if (_showOnlyMoves)
                {
                    ucIndex += 8;
                }
                else
                {
                    wpIndex += 16;
                    ucIndex = 0;
                }
            }
            if (e.Key == Key.PageUp)
            {
                if (_showOnlyMoves)
                {
                    ucIndex -= 8;
                }
                else
                {
                    wpIndex -= 8;
                    ucIndex = 0;
                }
            }
            if (e.Key == Key.Home)
            {
                ucIndex = 0;
                wpIndex = 0;
            }
            if (e.Key == Key.End)
            {
                ucIndex = 0;
                wpIndex = stackPanelMoves.Children.Count - 1;
                if (stackPanelMoves.Children[wpIndex] is WrapPanel wrapPanel2)
                {
                    ucIndex = wrapPanel2.Children.Count - 1;
                }
            }

            if (wpIndex > stackPanelMoves.Children.Count - 1)
            {
                wpIndex = stackPanelMoves.Children.Count - 1;
            }

            if (wpIndex < 0)
            {
                wpIndex = 0;
            }

            if (stackPanelMoves.Children[wpIndex] is WrapPanel wrapPanel3)
            {
                if (ucIndex > wrapPanel3.Children.Count - 1)
                {
                    if (wpIndex < stackPanelMoves.Children.Count - 1)
                    {
                        wpIndex++;
                        if (stackPanelMoves.Children[wpIndex] is WrapPanel wrapPanel4)
                        {
                            if (wpIndex < stackPanelMoves.Children.Count - 1 && wrapPanel4.Children.Count == 0)
                            {
                                wpIndex++;
                            }
                        }
                        ucIndex = 0;
                    }
                    else
                    {
                        ucIndex = wrapPanel3.Children.Count - 1;
                    }
                }
                if (ucIndex < 0)
                {
                    if (wpIndex > 0)
                    {
                        wpIndex--;
                        if (stackPanelMoves.Children[wpIndex] is WrapPanel wrapPanel2)
                        {
                            if (wpIndex > 0 && wrapPanel2.Children.Count == 0)
                            {
                                wpIndex--;
                                if (stackPanelMoves.Children[wpIndex] is WrapPanel wrapPanel4)
                                {
                                    ucIndex = wrapPanel4.Children.Count - 1;
                                }
                            }
                            else
                            {
                                ucIndex = wrapPanel2.Children.Count - 1;
                            }
                        }
                    }
                    else
                    {
                        ucIndex = 0;
                    }
                }
            }

            if (ucIndex < 0)
            {
                ucIndex = 0;
            }

            if (stackPanelMoves.Children[wpIndex] is WrapPanel wrapPanel)
            {
                if (ucIndex < wrapPanel.Children.Count)
                {
                    if (wrapPanel.Children[ucIndex] is MovePlainUserControl movePlainUserControl)
                    {
                        movePlainUserControl.SetFocus();
                    }
                }
            }

        }

        private void ButtonCopy_OnClick(object sender, RoutedEventArgs e)
        {
            var pgnCreator = new PgnCreator(_gameStartPosition, _pgnConfiguration);
            for (int w = 0; w < stackPanelMoves.Children.Count; w++)
            {
                if (stackPanelMoves.Children[w] is WrapPanel wrapPanel1)
                {
                    for (int c = 0; c < wrapPanel1.Children.Count; c++)
                    {
                        if (wrapPanel1.Children[c] is MovePlainUserControl movePlainUserControl)
                        {
                            pgnCreator.AddMove(movePlainUserControl.CurrentMove);
                        }
                    }
                }
            }
            var pgnGame = new PgnGame
            {
                GameEvent = _currentGame?.GameEvent,
                GameSite = _pgnGame?.GameSite,
                Round = _pgnGame?.Round,
                PlayerWhite = textBlockWhitePlayer.Text,
                PlayerBlack = textBlockBlackPlayer.Text,
                Result = textBlockResult.Text,
                GameDate = _pgnGame!=null ? _pgnGame.GameDate : DateTime.Now.ToString("yyyy.MM.dd")
            };
            if (!string.IsNullOrWhiteSpace(_gameStartPosition))
            {
                pgnGame.AddValue("FEN", _gameStartPosition);
                pgnGame.AddValue("SetUp", "1");
            }
            foreach (var move in pgnCreator.GetAllMoves())
            {
                pgnGame.AddMove(move);
            }
            ClipboardHelper.SetText(pgnGame.GetGame());
        }

        private void ButtonExtend_OnClick(object sender, RoutedEventArgs e)
        {
            if (_showOnlyMoves)
            {
                _showOnlyMoves = false;
                _configuration.SetBoolValue("extendMoveList", _showOnlyMoves);
                Refresh();
                return;
            }
            if (!_showFullInfo)
            {
                _showFullInfo = true;
                _configuration.SetBoolValue("extendFull", _showFullInfo);
                Refresh();
                return;
            }
            _showOnlyMoves = true;
            _showFullInfo = false;
            _configuration.SetBoolValue("extendMoveList", _showOnlyMoves);
            _configuration.SetBoolValue("extendFull", _showFullInfo);
            Refresh();
            MarkLastMove();
        }

        private void Refresh()
        {
            for (int w = 0; w < stackPanelMoves.Children.Count; w++)
            {
                if (stackPanelMoves.Children[w] is WrapPanel wrapPanel1)
                {
                    for (int c = 0; c < wrapPanel1.Children.Count; c++)
                    {
                        if (wrapPanel1.Children[c] is MovePlainUserControl movePlainUserControl)
                        {
                            movePlainUserControl.SelectedChanged -= MovePlainUserControl_SelectedChanged;
                            movePlainUserControl.ContentChanged -= MovePlainUserControl_ContentChanged;

                        }
                    }
                }
            }

            stackPanelMoves.Children.Clear();
            _lastMoveNumber = 0;
            foreach (var move in _moveList)
            {
                AddInternalMove(move);
            }
            SetContentInfo();
        }

        private void SetContentInfo()
        {
            textBlockContent.Text = string.Empty;
            string comments = _rm.GetString("WithoutComments");
            if (_showComments)
            {
                comments = _rm.GetString("WithComments");
            }
            if (_showOnlyMoves)
            {
                textBlockContent.Text = $"{_rm.GetString("ContentOnlyMoves")} {comments}";
                return;
            }

            if (_showForBuddy)
            {
                textBlockContent.Text = $"{_rm.GetString("ContentMoves")} {comments} {_rm.GetString("AndWithBestLineBuddy")} ";
                if (_showFullInfo)
                {
                    textBlockContent.Text += $"{_rm.GetString("AndWithBestLine")}";
                }

                return;
            }
            if (_showFullInfo)
            {
                textBlockContent.Text = $"{_rm.GetString("ContentMoves")} {comments} {_rm.GetString("AndWithBestLine")}";
                return;
            }

            textBlockContent.Text += $"{_rm.GetString("ContentMoves")} {comments} {_rm.GetString("AndWithFirstBestMove")}";
        }
        private void ButtonFontInc_OnClick(object sender, RoutedEventArgs e)
        {
            _fontSize += 2;
            for (int w = 0; w < stackPanelMoves.Children.Count; w++)
            {
                if (stackPanelMoves.Children[w] is WrapPanel wrapPanel1)
                {
                    for (int c = 0; c < wrapPanel1.Children.Count; c++)
                    {
                        if (wrapPanel1.Children[c] is MovePlainUserControl movePlainUserControl)
                        {

                            movePlainUserControl.FontSize = _fontSize;
                        }
                    }
                }
            }

        }

        private void ButtonFontDec_OnClick(object sender, RoutedEventArgs e)
        {
            if (_fontSize > 8)
            {
                _fontSize -= 2;

                for (int w = 0; w < stackPanelMoves.Children.Count; w++)
                {
                    if (stackPanelMoves.Children[w] is WrapPanel wrapPanel1)
                    {
                        for (int c = 0; c < wrapPanel1.Children.Count; c++)
                        {
                            if (wrapPanel1.Children[c] is MovePlainUserControl movePlainUserControl)
                            {
                                movePlainUserControl.FontSize = _fontSize;
                            }
                        }
                    }
                }
            }
        }

        private void ButtonShowHideComments_OnClick(object sender, RoutedEventArgs e)
        {
            _showComments = !_showComments;
            _configuration.SetBoolValue("extendComments", _showComments);
            Refresh();
        }

        private void CommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void MenuItemCopy_OnClick(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonCopy_OnClick(sender, e);
        }

        private void ButtonShowHideBuddy_OnClick(object sender, RoutedEventArgs e)
        {
            _showForBuddy = !_showForBuddy;
            _configuration.SetBoolValue("showForBuddy", _showForBuddy);
            Refresh();
        }
    }
}
