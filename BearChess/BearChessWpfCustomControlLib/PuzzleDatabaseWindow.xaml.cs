using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessDatabase;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib;

public partial class PuzzleDatabaseWindow : Window
{
    
    private readonly ILogging _logger;
    private readonly Configuration _configuration;
    private string _dbPath;
    private ResourceManager _rm;
    private PuzzleDatabase _database;

    public PuzzleDatabaseWindow(string dbPath, ILogging logger)
    {
        InitializeComponent();
        _configuration = Configuration.Instance;
        _rm = SpeechTranslator.ResourceManager;
        _dbPath = dbPath;
        _logger = logger;
        Top = _configuration.GetWinDoubleValue("PuzzleDatabaseWindowTop", Configuration.WinScreenInfo.Top,
            SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
        Left = _configuration.GetWinDoubleValue("PuzzleDatabaseWindowLeft", Configuration.WinScreenInfo.Left,
            SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
        var fileName = Path.Combine(dbPath,_configuration.GetConfigValue("PuzzleDatabaseFileName", PuzzleDatabase.ImportDBFileName));
        if (!File.Exists(fileName))
        {
            fileName = Path.Combine(dbPath, PuzzleDatabase.ImportDBFileName);
            _configuration.SetConfigValue("PuzzleDatabaseFileName", PuzzleDatabase.ImportDBFileName);
        }
      //  if (File.Exists(fileName))
        {
            _database = new PuzzleDatabase(logger, fileName);
            _database.LoadDb();
            SetDataSource();
            UpdateTitle();
        }
    }

    private void UpdateTitle()
    {
        Dispatcher.Invoke(() =>
        {
            Title =
                $"{dataGridGames.Items.Count} {_rm.GetString("Puzzle")}: {_database?.FileName}";
        });
    }

    private void SetDataSource()
    {
        var databasePuzzles = _database.GetPuzzles();
        dataGridGames.ItemsSource = databasePuzzles;
    }

    private void RepertoireDatabaseWindow_OnClosing(object sender, CancelEventArgs e)
    {
        _configuration.SetDoubleValue("PuzzleDatabaseWindowTop", Top);
        _configuration.SetDoubleValue("PuzzleDatabaseWindowLeft", Left);
    }

    private void ButtonNewDatabase_OnClick(object sender, RoutedEventArgs e)
    {
        var editWindow = new EditWindow()
        {
            Owner = this
        };
        editWindow.SetTitle(_rm.GetString("PuzzleDatabase"));
        editWindow.SetEditComment(_rm.GetString("NameDialog"));
        var showDialog = editWindow.ShowDialog();
        if (showDialog.HasValue && showDialog.Value)
        { 
            var dbName = editWindow.Comment;
            if (dbName.Contains(".db", StringComparison.InvariantCultureIgnoreCase))
            {
                dbName = dbName.ReplaceIgnoreCase(".db", "");
            }
            _configuration.SetConfigValue("PuzzleDatabaseFileName", $"{dbName}.db");
            var fileName = Path.Combine(_dbPath, $"{dbName}.db");
            if (File.Exists(fileName))
            {
                return;
            }                        
            _database = new PuzzleDatabase(_logger, fileName);            
            _database.LoadDb();            
            SetDataSource();
            UpdateTitle();
        }
    }

    private void ButtonOpenDatabase_OnClick(object sender, RoutedEventArgs e)
    {
        var dbFiles = Directory.GetFiles(_dbPath, "*.db");
        if (dbFiles.Length == 0) 
        {
            return;
        }

        var queryWindows = new QuerySelectionWindow
        {
            Owner = this
        };
        queryWindows.SetTitle(_rm.GetString("PuzzleDatabase"));
        queryWindows.SetComboBox(dbFiles.Select(file => Path.GetFileName(file)).ToArray());
        var result = queryWindows.ShowDialog();
        if (result.HasValue && result.Value)
        {
            var dbName = queryWindows.GetSelectedItem as string;
            _configuration.SetConfigValue("PuzzleDatabaseFileName", dbName);
            _database?.Close();
            _database = null;
            var fileName = Path.Combine(_dbPath, dbName);
            _database = new PuzzleDatabase(_logger, fileName);
            _database.LoadDb();
            SetDataSource();
            UpdateTitle();
        }
    }

    private void ButtonCompressDb_OnClick(object sender, RoutedEventArgs e)
    {
        _database?.Compress();
    }

 
    private void ButtonDeleteDb_OnClick(object sender, RoutedEventArgs e)
    {
        if (_database != null)
        {
            if (MessageBox.Show(_rm.GetString("DeleteAllGames"), _rm.GetString("DeleteDatabase"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                _database.Close();
                _database.Dispose();
                File.Delete(_database.FileName);
                _database = new PuzzleDatabase(_logger, Path.Combine(_dbPath, PuzzleDatabase.ImportDBFileName));
                _database.LoadDb();
                SetDataSource();
                UpdateTitle();
            }
        }
    }


    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_database == null)
        {
            return;
        }
        if (dataGridGames.SelectedItems.Count == 0)
        {
            return;
        }

        if (dataGridGames.SelectedItems.Count > 1)
        {
            if (MessageBox.Show(
                    $"{_rm.GetString("DeleteAllSelectedPuzzle")}  {dataGridGames.SelectedItems.Count} {_rm.GetString("Puzzle")}",
                    _rm.GetString("DeletePuzzle"), MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }
        }
        else
        {
            if (MessageBox.Show(_rm.GetString("DeleteSelectedPuzzle"), _rm.GetString("DeletePuzzle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }
        }
        foreach (var selectedItem in dataGridGames.SelectedItems)
        {
            if (selectedItem is DatabaseGameSimple pgnGame)
            {
                _database.DeletePuzzle(pgnGame.Id);
            }
        }
        SetDataSource();
        UpdateTitle();
    }

    private async void ButtonImport_OnClick(object sender, RoutedEventArgs e)
    {
        if (_database == null)
        {
            return;
        }
        var openFileDialog = new OpenFileDialog { Filter = "Puzzle|*.pgn" };
        try
        {
            openFileDialog.InitialDirectory = _dbPath;
        }
        catch
        {
            // ignored
        }
        var showDialog = openFileDialog.ShowDialog(this);
        if (showDialog.Value && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            await ImportFile(openFileDialog.FileName);
        }
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

        var startFromBasePosition = true;
        CurrentGame currentGame;
        var pgnLoader = new PgnLoader();
        var chessBoard = new ChessBoard();
        chessBoard.Init();
        chessBoard.NewGame();

        await Task.Factory.StartNew(() =>
        {
            var startTime = DateTime.Now;
            _logger?.LogDebug("Start import...");

            foreach (var pgnGame in pgnLoader.Load(fileName))
            {
                var fenValue = pgnGame.GetValue("FEN");
                if (!string.IsNullOrWhiteSpace(fenValue))
                {
                    chessBoard.SetPosition(fenValue, false);
                    startFromBasePosition = false;
                }
                for (var i = 0; i < pgnGame.MoveCount; i++)
                {
                    chessBoard.MakePgnMove(pgnGame.GetMove(i), pgnGame.GetComment(i), pgnGame.GetEMT(i), pgnGame.GetClock(i));
                }
                count++;
                if (count % 100 == 0)
                {
                    Dispatcher.Invoke(() => { infoWindow.SetInfo($"{count} {_rm.GetString("Puzzle")}..."); });
                }
                var uciInfoWhite = new UciInfo()
                {
                    IsPlayer = true,
                    Name = pgnGame.PlayerWhite
                };
                var uciInfoBlack = new UciInfo()
                {
                    IsPlayer = true,
                    Name = pgnGame.PlayerBlack
                };
                currentGame = new CurrentGame(uciInfoWhite, uciInfoBlack, string.Empty,
                    new TimeControl(), pgnGame.PlayerWhite, pgnGame.PlayerBlack,
                    startFromBasePosition, true);
                if (!startFromBasePosition)
                {
                    currentGame.StartPosition = fenValue;
                }
                
                if (_database.SavePuzzle("",pgnGame.GameEvent,pgnGame.GetGame(),pgnGame.MoveCount, false))
                {
                    chessBoard.Init();
                    chessBoard.NewGame();
                }
                else
                {
                    break;
                }
            }
            var diff = DateTime.Now - startTime;
            _database.CommitAndClose();
            _database.Compress();
            _logger?.LogDebug($"... {count} puzzle imported in {diff.TotalSeconds} sec.");

        }).ContinueWith(task =>
        {
            
            Dispatcher.Invoke(() => { infoWindow.Close(); });
            SetDataSource();
            UpdateTitle();
        }, System.Threading.CancellationToken.None, TaskContinuationOptions.None,
            TaskScheduler.FromCurrentSynchronizationContext());
        _logger?.LogDebug($"Import file finished.");
    }

    private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
    {
        if (_database == null)
        {
            return;
        }
        if (dataGridGames.Items.Count == 0)
        {
            return;
        }

        var selectedItems = dataGridGames.SelectedItems;
        if (selectedItems.Count == 0)
        {
            selectedItems = dataGridGames.Items;
        }

        ExportGames.Export(selectedItems, _database, _configuration.GetPgnConfiguration(), this);
    }

    private void DataGridGames_OnCopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
    {
        if (_database == null)
        {
            return;
        }
        e.ClipboardRowContent.Clear();
        try
        {
            if (dataGridGames.SelectedItem is DatabasePuzzle pgnGame)
            {
                var game = _database.LoadPuzzle(pgnGame.Id).PgnGame.GetGame();
                e.ClipboardRowContent.Add(new DataGridClipboardCellContent(e.Item, (sender as DataGrid).Columns[0], game));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, _rm.GetString("ErrorOnCopy"), MessageBoxButton.OK, MessageBoxImage.Error);
            MessageBox.Show(ex.StackTrace, _rm.GetString("ErrorOnCopy"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}