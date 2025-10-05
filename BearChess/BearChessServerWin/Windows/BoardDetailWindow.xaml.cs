using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessServerLib;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;

namespace www.SoLaNoSoft.com.BearChessServerWin.Windows
{
    /// <summary>
    /// Interaktionslogik für BoardDetailWindow.xaml
    /// </summary>
    public partial class BoardDetailWindow : Window
    {
        private readonly IBearChessController _bearChessController;
        private UciLoader _uciLoader;
        private string _fenPosition;
        private int _currentColor;
        private string _currentEngineName = string.Empty;

        public BoardDetailWindow(IBearChessController bearChessController)
        {
            InitializeComponent();
            _bearChessController = bearChessController;
            chessBoardUcGraphics.SetCanvas(canvasBoard);
            moveListPlainUserControl.Clear();
        }

        public void SetPlayerNames(string whitePlayerName, string blackPlayerName)
        {
            chessBoardUcGraphics.SetPlayer(whitePlayerName, blackPlayerName);
            moveListPlainUserControl.Clear();
            moveListPlainUserControl.SetPlayer(whitePlayerName, Fields.COLOR_WHITE);
            moveListPlainUserControl.SetPlayer(blackPlayerName, Fields.COLOR_BLACK);
        }

        public void RepaintBoard(IChessBoard chessBoard)
        {
            var playedMoveList = chessBoard.GetPlayedMoveList();
            moveListPlainUserControl.AddMove(playedMoveList.LastOrDefault());
            _currentColor = chessBoard.CurrentColor;
            chessBoardUcGraphics.RepaintBoard(chessBoard);
            if (_bearChessController.SelectedEngine == null)
            {
                return;
            }

            engineInfoUserControl.ClearQueue();
            if ((_uciLoader != null) && (_currentEngineName != _bearChessController.SelectedEngine.Name))
            {
                _uciLoader.EngineReadingEvent -= _uciLoader_EngineReadingEvent;
                _uciLoader.Stop();
                _uciLoader.Quit();
                _uciLoader.StopProcess();
                _uciLoader = null;
            }
            _fenPosition = chessBoard.GetFenPosition();
            if (_uciLoader != null)
            {
                _uciLoader.SetFen(_fenPosition, string.Empty);
            }
            else
            {
                _uciLoader = new UciLoader(_bearChessController.SelectedEngine, null, Configuration.Instance);
                _uciLoader.EngineReadingEvent += _uciLoader_EngineReadingEvent;
                _uciLoader.SetFen(_fenPosition, string.Empty);
                Dispatcher?.Invoke(() =>
                {
                    engineInfoUserControl.SetEngineName(_bearChessController.SelectedEngine.Name);

                });
            }
            _currentEngineName = _bearChessController.SelectedEngine.Name;
        }

        private void _uciLoader_EngineReadingEvent(object sender, UciLoader.EngineEventArgs e)
        {
            if (e.FromEngine.StartsWith("bestmove"))
            {
                return;
            }
            
            engineInfoUserControl.Color = _currentColor;
            engineInfoUserControl.ShowInfo(e.FromEngine, _fenPosition);

            var infoLineParts = e.FromEngine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < infoLineParts.Length; i++)
            {
                if (!infoLineParts[i].Equals("score", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var scoreType = infoLineParts[i + 1];
                if (scoreType.Equals("cp", StringComparison.OrdinalIgnoreCase))
                {
                    var scoreString = infoLineParts[i + 2];
                    if (decimal.TryParse(scoreString, NumberStyles.Any, CultureInfo.CurrentCulture, out var score))
                    {
                        score /= 100;
                        var analysesScore = score;
                        if ((e.Color == Fields.COLOR_BLACK) ||
                            (e.Color == Fields.COLOR_EMPTY && _currentColor == Fields.COLOR_BLACK))
                        {
                            analysesScore *= -1;
                        }
                        Dispatcher?.Invoke(() => { chessBoardUcGraphics.DrawAnalyses((double)analysesScore); });
                        break;
                    }
                }
            }
        }

        private void BoardDetailWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_uciLoader == null)
            {
                return;
            }
            _uciLoader.EngineReadingEvent -= _uciLoader_EngineReadingEvent;
            _uciLoader.Stop();
            _uciLoader.Quit();
            _uciLoader.StopProcess();
        }
    }
}
