using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Navigation;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessDatabase;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;


namespace www.SoLaNoSoft.com.BearChessWin
{
    /// <summary>
    /// Interaktionslogik für PuzzleWindow.xaml
    /// </summary>
    public partial class PuzzleWindow : Window
    {

        public event EventHandler<PuzzleSource> NextPuzzle;
        public event EventHandler<PuzzleSource> NextPuzzleRandom;
        public event EventHandler<PuzzleSource> ResetDatabase;
        public event EventHandler<PuzzleSource> PuzzleOfToday;
        public event EventHandler<PuzzleSource> SolutionRequested;
        public event EventHandler<PuzzleSource> HintRequested;

        private readonly Configuration _configuration;
        private readonly string _dbPath;
        private int _tries = 0;
        private int _totalCount = 0;
        private int _solvedCount = 0;
        private PuzzleSource _puzzleSource;
        private ResourceManager _rm;
        private readonly ILogging _logger;
        private Timer _timer = null;
        private bool _lastRequestWasRandom = false;
    
        public PuzzleWindow(Configuration configuration, string dbPath, ILogging logger)
        {
            InitializeComponent();
            _rm = SpeechTranslator.ResourceManager;
            _configuration = configuration;
            _dbPath = dbPath;
            _logger = logger;
            _puzzleSource = (PuzzleSource) _configuration.GetIntValue("puzzleSource", (int)PuzzleSource.ChessCom);
            Top = _configuration.GetWinDoubleValue("PuzzleWindowTop", Configuration.WinScreenInfo.Top,
                SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
            Left = _configuration.GetWinDoubleValue("PuzzleWindowLeft", Configuration.WinScreenInfo.Left,
                SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
            Width =  _configuration.GetDoubleValue("PuzzleWindowWidth", 450);
            Height = _configuration.GetDoubleValue("PuzzleWindowHeight", 320);
            buttonHint.IsEnabled = false;
            buttonResign.IsEnabled = false;
            _timer = new Timer(1000);
            _timer.AutoReset = true;
            _timer.Elapsed += TimerOnElapsed;
            UpdateSourceSelection();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher?.Invoke(() =>
            {
                var value = progressBarSeconds.Value;
                if (value < progressBarSeconds.Maximum)
                {
                    progressBarSeconds.Value = value + 1;
                }
                else
                {
                    _timer.Enabled = false;
                    progressBarSeconds.Value = 0;
                    if (_lastRequestWasRandom)
                    {

                        NextPuzzleRandom?.Invoke(this, _puzzleSource);
                    }
                    else
                    {
                        NextPuzzle?.Invoke(this, _puzzleSource);
                    }
                }
            });

        }

        public void IncTries()
        {
            _tries++;
            textBlocktTries.Text = _tries.ToString();
        }

        public void ResetSolvedCount()
        {
            _solvedCount = 0;
            _tries = 0;
            UpdateSourceSelection();
        }

        public void PuzzleSolved()
        {
            buttonHint.IsEnabled = false;
            buttonResign.IsEnabled = false;
            imageSolved.Visibility = Visibility.Visible;
            if (checkBoxCountdown.IsChecked.HasValue && checkBoxCountdown.IsChecked.Value)
            {
                progressBarSeconds.Value = 0;
                _timer.Enabled = true;
            }
        }

        public void SetPuzzle(PgnGame pgnGame, int moveCount, DateTime dateTime, string url, int totalCount, int solvedCount)
        {
            _timer.Enabled = false;
            progressBarSeconds.Value = 0;
            _tries = 0;
            _totalCount = totalCount;
            _solvedCount = solvedCount;
            textBlockTitle.Text = pgnGame.GameEvent;
            runtextBlockUrl.Text = string.Empty;
            hyperlinkUrl.NavigateUri = null;
            hyperlinkUrl.IsEnabled = !string.IsNullOrEmpty(url);
            if (!string.IsNullOrEmpty(url))
            {
                runtextBlockUrl.Text = url;
                hyperlinkUrl.NavigateUri = new Uri(url);
            }

            textBlockFen.Text = pgnGame.FENLine;
            textBlockPublished.Text =  dateTime==DateTime.MinValue ? string.Empty : dateTime.ToString("g");
            textBlocktTries.Text = _tries.ToString();
            textBlockMoveCount.Text = moveCount > 0 ? moveCount.ToString() : "?";
            buttonHint.IsEnabled = true;
            buttonResign.IsEnabled = true;
            imageSolved.Visibility = Visibility.Hidden;
            UpdateSourceSelection();
        }

        public void ShowMissingPuzzles()
        {
            Dispatcher?.Invoke(() =>
            {
                BearChessMessageBox.Show(_rm.GetString("PuzzleDatabaseEmpty"), _rm.GetString("MissingInformation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        private void PuzzleWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _configuration.SetDoubleValue("PuzzleWindowTop", Top);
            _configuration.SetDoubleValue("PuzzleWindowLeft", Left);
            _configuration.SetDoubleValue("PuzzleWindowWidth", Width);
            _configuration.SetDoubleValue("PuzzleWindowHeight", Height);
            _timer.Enabled = false;
            _timer.Close();
        }

        private void ButtonNextPuzzle_OnClick(object sender, RoutedEventArgs e)
        {
            if (_puzzleSource == PuzzleSource.None)
            {
                BearChessMessageBox.Show(_rm.GetString("PuzzelSourceMissing"), _rm.GetString("MissingInformation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _lastRequestWasRandom = false;
            NextPuzzle?.Invoke(this, _puzzleSource);
        }

        private void ButtonHint_OnClick(object sender, RoutedEventArgs e)
        {
            if (_puzzleSource == PuzzleSource.None)
            {
                return;
            }
            HintRequested?.Invoke(this, _puzzleSource);
        }

        private void ButtonResign_OnClick(object sender, RoutedEventArgs e)
        {
            if (_puzzleSource == PuzzleSource.None)
            {
                return;
            }
            SolutionRequested?.Invoke(this, _puzzleSource);
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonToday_OnClick(object sender, RoutedEventArgs e)
        {
            if (_puzzleSource == PuzzleSource.None)
            {
                return;
            }
            _lastRequestWasRandom = false;
            PuzzleOfToday?.Invoke(this, _puzzleSource);
        }

        private void MenuItemChessCom_OnClick(object sender, RoutedEventArgs e)
        {
            _puzzleSource = PuzzleSource.ChessCom;
            _solvedCount = 0;
            _totalCount = 0;
            _lastRequestWasRandom = true;
            ClearInfos();
            UpdateSourceSelection();
        }

        private void MenuItemLichess_OnClick(object sender, RoutedEventArgs e)
        {
            var currentFile = Configuration.Instance.GetConfigValue("lichessPuzzleFile", string.Empty);
            if (string.IsNullOrEmpty(currentFile) || !File.Exists(currentFile))
            {
                SelectLichessFile();
                currentFile = Configuration.Instance.GetConfigValue("lichessPuzzleFile", string.Empty);
                if (string.IsNullOrEmpty(currentFile) || !File.Exists(currentFile))
                {
                    return;
                }
            }
            _solvedCount = 0;
            _totalCount = 0;
            _puzzleSource = PuzzleSource.Lichess;
            _lastRequestWasRandom = true;
            ClearInfos();
            UpdateSourceSelection();
        }

        private void MenuItemBearChess_OnClick(object sender, RoutedEventArgs e)
        {
            _puzzleSource = PuzzleSource.BearChess;
            _solvedCount = 0;
            _totalCount = 0;
            ClearInfos();
            UpdateSourceSelection();
        }

        private void MenuItemDatabase_OnClick(object sender, RoutedEventArgs e)
        {
            _puzzleSource = PuzzleSource.Database;
            _solvedCount = 0;
            _totalCount = 0;
            ClearInfos();
            UpdateSourceSelection();
        }

        private void ClearInfos()
        {
            textBlockTitle.Text = string.Empty;
            runtextBlockUrl.Text = string.Empty;
            hyperlinkUrl.NavigateUri = null;
            hyperlinkUrl.IsEnabled = false;
            textBlockFen.Text = string.Empty;
            textBlockPublished.Text = string.Empty;
            textBlocktTries.Text = string.Empty;
            textBlockMoveCount.Text = string.Empty;
        }

        private void UpdateSourceSelection()
        {
            
            if (_puzzleSource == PuzzleSource.Lichess)
            {
                var currentFile = Configuration.Instance.GetConfigValue("lichessPuzzleFile", string.Empty);
                if (string.IsNullOrEmpty(currentFile) || !File.Exists(currentFile))
                {
                    _puzzleSource = PuzzleSource.None;
                }
            }
            buttonNextPuzzle.IsEnabled = _puzzleSource == PuzzleSource.BearChess || _puzzleSource == PuzzleSource.Database;
            buttonNextPuzzle.Visibility = _puzzleSource == PuzzleSource.BearChess || _puzzleSource == PuzzleSource.Database ? Visibility.Visible : Visibility.Hidden;
            buttonDatabaseReset.IsEnabled = _puzzleSource == PuzzleSource.BearChess || _puzzleSource == PuzzleSource.Database;
            buttonDatabaseReset.Visibility = _puzzleSource == PuzzleSource.BearChess || _puzzleSource == PuzzleSource.Database ? Visibility.Visible : Visibility.Hidden;
            buttonToday.IsEnabled = _puzzleSource == PuzzleSource.ChessCom;
            buttonToday.Visibility = _puzzleSource == PuzzleSource.ChessCom ? Visibility.Visible : Visibility.Hidden;
            imageBearChess.Visibility = _puzzleSource == PuzzleSource.BearChess ? Visibility.Visible : Visibility.Hidden;
            imageChessCom.Visibility = _puzzleSource == PuzzleSource.ChessCom ? Visibility.Visible : Visibility.Hidden;
            imageLichess.Visibility = _puzzleSource == PuzzleSource.Lichess ? Visibility.Visible : Visibility.Hidden;
            imageDatabase.Visibility = _puzzleSource == PuzzleSource.Database ? Visibility.Visible : Visibility.Hidden;
            switch (_puzzleSource)
            {
                case PuzzleSource.None:
                    textBlocktSources.Text = "----";
                    break;
                case PuzzleSource.ChessCom:
                    textBlocktSources.Text = "Chess.com";
                    break;
                case PuzzleSource.Lichess:
                    textBlocktSources.Text = "lichess.org";
                    break;
                case PuzzleSource.BearChess:
                    textBlocktSources.Text = _totalCount > 0 ? $"{_rm.GetString("MateInByBearChess")} ({_rm.GetString("Solved")} {_solvedCount} {_rm.GetString("From")} {_totalCount})" : _rm.GetString("MateInByBearChess");
                    break;
                case PuzzleSource.Database:
                    textBlocktSources.Text = _totalCount > 0 ? $"{_rm.GetString("OwnPuzzles")} ({_rm.GetString("Solved")} {_solvedCount} {_rm.GetString("From")} {_totalCount})" : _rm.GetString("OwnPuzzles");
                    break;
                default:
                    textBlocktSources.Text = "?";
                    break;
            }
        }


        private void SelectLichessFile()
        {
            var currentFile = Configuration.Instance.GetConfigValue("lichessPuzzleFile", string.Empty);

            var openFileDialog = new OpenFileDialog { Filter = "lichess.org puzzles|*.csv|All files|*.*" };
            if (!string.IsNullOrEmpty(currentFile))
            {
                try
                {
                    if (File.Exists(currentFile))
                    {
                        openFileDialog.InitialDirectory = new FileInfo(currentFile).DirectoryName;
                    }
                }
                catch
                {
                    // ignored
                }
            }
            var showDialog = openFileDialog.ShowDialog(this);
            if (showDialog.Value && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                Configuration.Instance.SetConfigValue("lichessPuzzleFile", openFileDialog.FileName);
            }
        }

        private void MenuItemLichessSelectFile_OnClick(object sender, RoutedEventArgs e)
        {
            SelectLichessFile();
        }

        private void HyperlinkUrl_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void MenuItemHelpLichess_OnClick(object sender, RoutedEventArgs e)
        {
            var helpWindow = new LichessPuzzleInfoWindow
            {
                Owner = this
            };
            helpWindow.ShowDialog();
        }

        private async Task ImportFile(string fileName)
        {
            _logger?.LogDebug($"Import file started.");
            var count = 0;
            var fi = new FileInfo(fileName);

            ProgressWindow infoWindow = null;
            Dispatcher.Invoke(() =>
            {
                infoWindow = new ProgressWindow
                {
                    Owner = this
                };

                infoWindow.IsIndeterminate(true);
                infoWindow.SetTitle($"{_rm.GetString("Import")} {fi.Name}");
                infoWindow.SetWait(_rm.GetString("PleaseWait"));
                infoWindow.Show();
            });
      

            await Task.Factory.StartNew(() =>
            {
                var startTime = DateTime.Now;
                _logger?.LogDebug("Start import...");
                var database = new PuzzleDatabase(_logger, Path.Combine(_dbPath, PuzzleDatabase.ImportDbFileName));
                var pgnLoader = new PgnLoader();
                foreach (var pgnGame in pgnLoader.Load(fileName))
                {
                    int moveCount = pgnGame.MoveCount;
                    var plyCount = pgnGame.GetValue("PlyCount");
                    if (!string.IsNullOrEmpty(plyCount) && int.TryParse(plyCount, out var plyCountValue))
                    {
                        moveCount = plyCountValue;
                    }
                    database.SavePuzzle("", pgnGame.GameEvent, pgnGame.GetGame(), moveCount);
                    count++;
                    if (count % 100 == 0)
                    {
                        infoWindow.SetInfo($"{count} {_rm.GetString("Puzzle")}...");
                    }
                }
                database.Close();
               
                var diff = DateTime.Now - startTime;
                _logger?.LogDebug($"... {count} puzzles imported in {diff.TotalSeconds} sec.");

            });
            infoWindow.Close();
            _logger?.LogDebug("Import file finished.");
            BearChessMessageBox.Show($"{count} {_rm.GetString("PuzzlesImported")} ", _rm.GetString("Information"), MessageBoxButton.OK,
                MessageBoxImage.Information);
            
        }

        private async void MenuItemImportPuzzle_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "Puzzle|*.pgn;" };
            var showDialog = openFileDialog.ShowDialog(this);
            if (!showDialog.Value || string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                return;
            }

            try
            {
                await ImportFile(openFileDialog.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                BearChessMessageBox.Show(ex.Message,
                    _rm.GetString("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckBoxCountdown_OnUnchecked(object sender, RoutedEventArgs e)
        {
            _timer.Enabled = false;
            progressBarSeconds.Value = 0;
            progressBarSeconds.Maximum = numericUpDownUserControlDuration.Value;
        }

        private void NumericUpDownUserControlDuration_OnValueChanged(object sender, int e)
        { 
            progressBarSeconds.Value = 0;
            progressBarSeconds.Maximum = numericUpDownUserControlDuration.Value;
        }

        private void CheckBoxCountdown_OnChecked(object sender, RoutedEventArgs e)
        {
            if (imageSolved.Visibility == Visibility.Visible)
            {
                progressBarSeconds.Value = 0;
                progressBarSeconds.Maximum = numericUpDownUserControlDuration.Value;
                _timer.Enabled = true;
            }
        }

        private void ButtonNextPuzzleRandom_OnClick(object sender, RoutedEventArgs e)
        {
            if (_puzzleSource == PuzzleSource.None)
            {
                BearChessMessageBox.Show(_rm.GetString("PuzzelSourceMissing"), _rm.GetString("MissingInformation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _lastRequestWasRandom = true;
            NextPuzzleRandom?.Invoke(this, _puzzleSource);
        }

        private void ButtonDatabaseReset_OnClick(object sender, RoutedEventArgs e)
        {
            var messageBoxResult = BearChessMessageBox.Show(_rm.GetString("ResetDatabase")+"?", _rm.GetString("Warning"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                ResetDatabase?.Invoke(this, _puzzleSource);
            }
        }
    }
}
