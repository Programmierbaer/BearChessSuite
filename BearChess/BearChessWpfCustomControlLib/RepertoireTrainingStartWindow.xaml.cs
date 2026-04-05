using System.IO;
using System.Windows;
using System.Windows.Controls;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessDatabase;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für RepertoireTrainingStartWindow.xaml
    /// </summary>
    public partial class RepertoireTrainingStartWindow : Window
    {
        
        private RepertoireDatabase _database;
        private readonly string _dbPath;
        private readonly RepertoireTrainingConfig _repertoireTrainingConfig;
        private readonly ILogging _logger;
        private bool _ignoreSelectionChanged = false;
        private string _initialDbName;

        public RepertoireTrainingStartWindow(string dbPath, RepertoireTrainingConfig repertoireTrainingConfig, ILogging logger)
        {
            
            _dbPath = dbPath;
            _repertoireTrainingConfig = repertoireTrainingConfig;
            _logger = logger;
            InitializeComponent();
            _initialDbName = repertoireTrainingConfig.DatabaseName;
            _ignoreSelectionChanged  = true;
            FillDatabaseComboBox();
            checkBoxAllowAllMoves.IsChecked = repertoireTrainingConfig.AllowAllMoves;
            checkBoxMakeMove.IsChecked = repertoireTrainingConfig.ExecuteMoveAutomatically;
            radioButtonWhite.IsChecked = repertoireTrainingConfig.ExecuteForColor == Fields.COLOR_WHITE;
            radioButtonBlack.IsChecked = repertoireTrainingConfig.ExecuteForColor == Fields.COLOR_BLACK;
            checkBoxShowNextMove.IsChecked = repertoireTrainingConfig.ShowNextMove;
            numericUpDownUserControlInterval.Value = repertoireTrainingConfig.NextMoveSeconds;
            checkBoxShowMovelist.IsChecked = repertoireTrainingConfig.ShowCurrentGame;
            checkBoxStartGame.IsChecked = repertoireTrainingConfig.ContinueAsNewGame;
            _ignoreSelectionChanged  = false;
        }

        private void FillDatabaseComboBox()
        {
            var selectionIndex = 0;
            comboBoxDatabases.Items.Clear();
            var databaseNames = Directory.GetFiles(_dbPath, "*.rep");
            if (databaseNames.Length == 0)
            {
                var fileName = Path.Combine(_dbPath, $"{Constants.RepertoireDefaultDBName}");
                var database = new RepertoireDatabase(_logger, fileName, this);
                database.LoadDb();
                database.Close();
                databaseNames = Directory.GetFiles(_dbPath, "*.rep");
                _repertoireTrainingConfig.DatabaseName = Constants.RepertoireDefaultDBName;
            }
            for (var i = 0; i < databaseNames.Length; i++)
            {
                var fi = new FileInfo(databaseNames[i]);
                comboBoxDatabases.Items.Add(fi.Name);
                if (fi.Name.Equals(_repertoireTrainingConfig.DatabaseName))
                {
                    selectionIndex = i;
                }
            }
            if (comboBoxDatabases.Items.Count > 0)
            {
                comboBoxDatabases.SelectedIndex = selectionIndex;
                var dbFileName =  $"{comboBoxDatabases.SelectedItem}";
                _repertoireTrainingConfig.DatabaseName = dbFileName;
            }
            buttonOk.IsEnabled = GetGamesCount(_repertoireTrainingConfig.DatabaseName) > 0;
            textBlockEmptyDatabase.Visibility  = buttonOk.IsEnabled ? Visibility.Hidden : Visibility.Visible;
        }

        private int GetGamesCount(string dbName)
        {
            var fileName = Path.Combine(_dbPath, dbName);
            var database = new RepertoireDatabase(_logger, fileName, this);
            database.LoadDb();
            var count = database.GetTotalGamesCount();
            database.Close();
            return count;
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            _repertoireTrainingConfig.DatabaseName = _initialDbName;
            DialogResult = false;
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            if (comboBoxDatabases.Items.Count > 0)
            {
                _repertoireTrainingConfig.AllowAllMoves =
                    checkBoxAllowAllMoves.IsChecked.HasValue && checkBoxAllowAllMoves.IsChecked.Value;
                _repertoireTrainingConfig.ExecuteMoveAutomatically =
                    checkBoxMakeMove.IsChecked.HasValue && checkBoxMakeMove.IsChecked.Value;
                _repertoireTrainingConfig.ShowNextMove =
                    checkBoxShowNextMove.IsChecked.HasValue && checkBoxShowNextMove.IsChecked.Value;
                _repertoireTrainingConfig.ShowCurrentGame =
                    checkBoxShowMovelist.IsChecked.HasValue && checkBoxShowMovelist.IsChecked.Value;
                _repertoireTrainingConfig.ContinueAsNewGame =
                    checkBoxStartGame.IsChecked.HasValue && checkBoxStartGame.IsChecked.Value;
                _repertoireTrainingConfig.NextMoveSeconds = numericUpDownUserControlInterval.Value;
                _repertoireTrainingConfig.ExecuteForColor =
                    radioButtonWhite.IsChecked.HasValue && radioButtonWhite.IsChecked.Value
                        ? Fields.COLOR_WHITE
                        : Fields.COLOR_BLACK;
                _repertoireTrainingConfig.DatabaseName = $"{comboBoxDatabases.SelectedItem}";
            }
            DialogResult = true;
        }

      

        private void ButtonDatabase_OnClick(object sender, RoutedEventArgs e)
        {
            var dbDialog = new RepertoireDatabaseWindow(_dbPath, _logger, _repertoireTrainingConfig)
                {
                    Owner = this
                };
            dbDialog.ShowDialog();
            
            _ignoreSelectionChanged = true;
            FillDatabaseComboBox();
            _ignoreSelectionChanged = false;
        }

        private void ComboBoxDatabases_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreSelectionChanged)
            {
                return;
            }

            if (comboBoxDatabases.Items.Count > 0)
            {
                _repertoireTrainingConfig.DatabaseName = $"{comboBoxDatabases.SelectedItem}";
            }
            buttonOk.IsEnabled = comboBoxDatabases.Items.Count > 0 &&
                                 GetGamesCount(_repertoireTrainingConfig.DatabaseName) > 0;
            textBlockEmptyDatabase.Visibility  = buttonOk.IsEnabled ? Visibility.Hidden : Visibility.Visible;
        }
    }
}
