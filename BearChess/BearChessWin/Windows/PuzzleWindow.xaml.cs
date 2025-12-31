using System;
using System.ComponentModel;
using System.Windows;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using static www.SoLaNoSoft.com.BearChess.BearChessCommunication.ChessCom.ChessComReader;

namespace www.SoLaNoSoft.com.BearChessWin
{
    /// <summary>
    /// Interaktionslogik für PuzzleWindow.xaml
    /// </summary>
    public partial class PuzzleWindow : Window
    {

        public event EventHandler NextPuzzle;
        public event EventHandler PuzzleOfToday;
        public event EventHandler SolutionRequested;
        public event EventHandler HintRequested;

        private readonly Configuration _configuration;
        private int _tries = 0;

        public PuzzleWindow(Configuration configuration)
        {
            InitializeComponent();
            _configuration = configuration;
            Top = _configuration.GetWinDoubleValue("PuzzleWindowTop", Configuration.WinScreenInfo.Top,
                SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
            Left = _configuration.GetWinDoubleValue("PuzzleWindowLeft", Configuration.WinScreenInfo.Left,
                SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
            Width =  _configuration.GetDoubleValue("PuzzleWindowWidth", 450);
            Height = _configuration.GetDoubleValue("PuzzleWindowHeight", 250);
            buttonHint.IsEnabled = false;
            buttonResign.IsEnabled = false;
        }

        public void IncTries()
        {
            _tries++;
            textBlocktTries.Text = _tries.ToString();
        }

        public void PuzzleSolved()
        {
            buttonHint.IsEnabled = false;
            buttonResign.IsEnabled = false;
            imageSolved.Visibility = Visibility.Visible;
        }

        public void SetPuzzle(PuzzleResponse puzzle, int moveCount)
        {
            _tries = 0;
            textBlockTitle.Text = puzzle.Title;
            textBlockUrl.Text = puzzle.Url;
            textBlockFen.Text = puzzle.Fen;
            textBlockPublished.Text = DateTimeOffset.FromUnixTimeSeconds(puzzle.PublishTime).DateTime.ToString("g");
            textBlocktTries.Text = _tries.ToString();
            textBlockMoveCount.Text = moveCount>0 ? moveCount.ToString() : "?";
            buttonHint.IsEnabled = true;
            buttonResign.IsEnabled = true;
            imageSolved.Visibility = Visibility.Hidden;
        }

        private void PuzzleWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _configuration.SetDoubleValue("PuzzleWindowTop", Top);
            _configuration.SetDoubleValue("PuzzleWindowLeft", Left);
            _configuration.SetDoubleValue("PuzzleWindowWidth", Width);
            _configuration.SetDoubleValue("PuzzleWindowHeight", Height);
        }

        private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            NextPuzzle?.Invoke(this, null);
        }

        private void ButtonHint_OnClick(object sender, RoutedEventArgs e)
        {
            HintRequested?.Invoke(this, null);
        }

        private void ButtonResign_OnClick(object sender, RoutedEventArgs e)
        {
            SolutionRequested?.Invoke(this, null);
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonToday_OnClick(object sender, RoutedEventArgs e)
        {
            PuzzleOfToday?.Invoke(this, null);
        }
    }
}
