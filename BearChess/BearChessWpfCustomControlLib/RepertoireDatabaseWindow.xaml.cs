using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessDatabase;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib;

public partial class RepertoireDatabaseWindow : Window
{
    public ILogging Logger { get; }
    private RepertoireDatabase _database;
    private readonly ILogging _logger;
    private readonly Configuration _configuration;
    private string _dbPath;
    private readonly RepertoireTrainingConfig _repertoireTrainingConfig;
    private ResourceManager _rm;
    public RepertoireDatabaseWindow(string dbPath, ILogging logger, RepertoireTrainingConfig repertoireTrainingConfig)
    {
        Logger = logger;
        _dbPath = dbPath;
        _repertoireTrainingConfig = repertoireTrainingConfig;
        InitializeComponent();
        _rm = SpeechTranslator.ResourceManager;
        _configuration = Configuration.Instance;
        Top = _configuration.GetWinDoubleValue("RepertoireDatabaseWindowTop", Configuration.WinScreenInfo.Top, SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
        Left = _configuration.GetWinDoubleValue("RepertoireDatabaseWindowLeft", Configuration.WinScreenInfo.Left, SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
        if (!string.IsNullOrEmpty(repertoireTrainingConfig.DatabaseName))
        {
           var fileName = Path.Combine(_dbPath, $"{repertoireTrainingConfig.DatabaseName}");
           if (File.Exists(fileName))
           {
               _database = new RepertoireDatabase(logger, fileName, this);
               _database.LoadDb(fileName);
               SetDataSource();
               UpdateTitle();
           }
        }
        
    }

    private void SetDataSource()
    {
        var databaseGameSimples = _database.GetGames(new GamesFilter());
        dataGridGames.ItemsSource = databaseGameSimples;
    }

    private void ButtonNewDatabase_OnClick(object sender, RoutedEventArgs e)
    {
        var editWindow = new EditWindow()
        {
            Owner = this
        };
        editWindow.SetTitle("Database Name");
        var showDialog = editWindow.ShowDialog();
        if (showDialog.HasValue && showDialog.Value) 
        {
            var newName = editWindow.Comment.Replace(".db","");
            var fileName = Path.Combine(_dbPath, $"{newName}.db");
            if (File.Exists(fileName))
            {
                return;
            }            
            if (_database == null)
            {
                _database = new RepertoireDatabase(_logger, fileName, this);
            }
            _database.LoadDb(fileName);
            _repertoireTrainingConfig.DatabaseName  = $"{newName}.db";
            SetDataSource();
            UpdateTitle();
        }
    }

    private void ButtonOpenDatabase_OnClick(object sender, RoutedEventArgs e)
    {
        var dbFiles = Directory.GetFiles(_dbPath, "*.rep");
        if (dbFiles.Length == 0) 
        {
            return;
        }
        
        var queryWindows = new QuerySelectionWindow();
        queryWindows.Owner = this;
        queryWindows.SetTitle("Repertoire Databases");
        queryWindows.SetComboBox(dbFiles.Select(file => Path.GetFileName(file)).ToArray());
        var result = queryWindows.ShowDialog();
        if (result.HasValue && result.Value)
        {
            var dbName = queryWindows.GetSelectedItem as string;
            _database?.Close();
            _database = null;
            var fileName = Path.Combine(_dbPath, dbName);
            _database = new RepertoireDatabase(_logger, fileName, this);
            _database.LoadDb(fileName);
            _repertoireTrainingConfig.DatabaseName  = dbName;
            SetDataSource();
            UpdateTitle();
        }
    }

    private void ButtonCompressDb_OnClick(object sender, RoutedEventArgs e)
    {
        _database?.Compress();
    }

    private void ButtonSaveDb_OnClick(object sender, RoutedEventArgs e)
    {
        if (_database == null)
        {
            return;
        }

        var backup = _database.Backup();
        if (backup.StartsWith("Error"))
        {
            MessageBox.Show($"{_rm.GetString("UnableToSaveDatabase")}{Environment.NewLine}{backup}",
                _rm.GetString("SaveDatabase"), MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else
        {
            MessageBox.Show($"{_rm.GetString("DatabaseSavedTo")}{Environment.NewLine}{backup}",
                _rm.GetString("SaveDatabase"), MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        
        SetItemsSource();
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
                File.Delete(_database.FileName);
                _database = new RepertoireDatabase(_logger, Path.Combine(_dbPath,Constants.RepertoireDefaultDBName), this);
                _database.LoadDb();
                SetDataSource();
                UpdateTitle();
            }
        }
    }

    private void ButtonRestoreDb_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = $"{_rm.GetString("SavedDatabase")}|*.bak_*;",
            InitialDirectory = _dbPath
        };
        var showDialog = openFileDialog.ShowDialog(this);
        if (showDialog.Value && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
        {
            var info = new FileInfo(openFileDialog.FileName);
            if (MessageBox.Show(
                    $"{_rm.GetString("OverrideDatabase")}{Environment.NewLine}{_rm.GetString("From")} {info.CreationTime}?",
                    _rm.GetString("RestoreDatabase"), MessageBoxButton.YesNo,
                    MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                if (_database == null)
                {
                    _database = new RepertoireDatabase(_logger, openFileDialog.FileName, this);
                }

                var restore = _database.Restore(openFileDialog.FileName);
                if (string.IsNullOrWhiteSpace(restore))
                {
                    MessageBox.Show(_rm.GetString("DatabaseRestored"), _rm.GetString("RestoreDatabase"),
                        MessageBoxButton.OK, MessageBoxImage.Information);

                }
                else
                {
                    MessageBox.Show($"{_rm.GetString("UnableRestoreDatabase")}{Environment.NewLine}{restore}",
                        _rm.GetString("RestoreDatabase"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                SetItemsSource();
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
                    $"{_rm.GetString("DeleteAllSelected")}  {dataGridGames.SelectedItems.Count} {_rm.GetString("Games")}",
                    _rm.GetString("DeleteGames"), MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }
        }
        else
        {
            if (MessageBox.Show(_rm.GetString("DeleteSelectedGame"), _rm.GetString("DeleteGame"),
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
                _database.DeleteGame(pgnGame.Id);
            }
        }
        SetItemsSource();
        UpdateTitle();
    }

    private async void ButtonImport_OnClick(object sender, RoutedEventArgs e)
    {
        if (_database == null)
        {
            return;
        }
        var openFileDialog = new OpenFileDialog { Filter = "Repertoire|*.pgn" };
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
                //    continue;
                count++;
                if (count % 100 == 0)
                {
                    Dispatcher.Invoke(() => { infoWindow.SetInfo($"{count} {_rm.GetString("Games")}..."); });
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
                
                if (_database.Save(new DatabaseGame(pgnGame, chessBoard.GetPlayedMoveList(), currentGame) { TwicId = 0 }, false, count % 100 == 0, 0) > 0)
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
            _logger?.LogDebug($"... {count} games imported in {diff.TotalSeconds} sec.");

        }).ContinueWith(task =>
        {
            
            Dispatcher.Invoke(() => { infoWindow.Close(); });
            SetItemsSource();
            UpdateTitle();
        }, System.Threading.CancellationToken.None, TaskContinuationOptions.None,
            TaskScheduler.FromCurrentSynchronizationContext());
        _logger?.LogDebug($"Import file finished.");
    }

    private void UpdateTitle()
    {
        Dispatcher.Invoke(() =>
        {
            Title =
                $"{dataGridGames.Items.Count} {_rm.GetString("Of")} {_database?.GetTotalGamesCount()} {_rm.GetString("GamesOn")}: {_database?.FileName}";
        });
    }

    private void SetItemsSource()
    {
        if (_database == null)
        {
            dataGridGames.ItemsSource = null;
            return;
        }
        var databaseGameSimples = _database.GetGames(new GamesFilter());
        dataGridGames.ItemsSource = databaseGameSimples;
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
            if (dataGridGames.SelectedItem is DatabaseGameSimple pgnGame)
            {
                var game = _database.LoadGame(pgnGame.Id, _configuration.GetPgnConfiguration()).PgnGame.GetGame();
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

    private void RepertoireDatabaseWindow_OnClosing(object sender, CancelEventArgs e)
    {
        _configuration.SetDoubleValue("RepertoireDatabaseWindowTop", Top);
        _configuration.SetDoubleValue("RepertoireDatabaseWindowLeft", Left);
    }
}