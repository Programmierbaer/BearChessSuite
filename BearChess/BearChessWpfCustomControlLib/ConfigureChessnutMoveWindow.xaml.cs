using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using www.SoLaNoSoft.com.BearChess.ChessnutAirLoader;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Implementations;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für ConfigureChessnutMoveWindow.xaml
    /// </summary>
    public partial class ConfigureChessnutMoveWindow : Window
    {
        private ChessnutMoveLoader _loader;
        private readonly string _fileName;
        private readonly EChessBoardConfiguration _eChessBoardConfiguration;
        private int _currentIndex;
        private bool _loaded = false;        
        private readonly FileLogger _fileLogger;
        private readonly ResourceManager _rm;

        public string SelectedPortName => "BTLE";

        public ConfigureChessnutMoveWindow(Configuration configuration) : this(configuration, configuration.FolderPath) { }

        public ConfigureChessnutMoveWindow(Configuration configuration, string configPath)
        {
            InitializeComponent();
            _rm = SpeechTranslator.ResourceManager;

            _fileName = Path.Combine(configPath, ChessnutMoveLoader.EBoardName, $"{ChessnutMoveLoader.EBoardName}Cfg.xml");

            var fileInfo = new FileInfo(_fileName);
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
                Directory.CreateDirectory(Path.Combine(fileInfo.DirectoryName, "log"));
            }
            try
            {
                _fileLogger = new FileLogger(Path.Combine(fileInfo.DirectoryName, "log", "ChessnutMoveCfg.log"), 10, 10);
                _fileLogger.Active = bool.Parse(configuration.GetConfigValue("writeLogFiles", "true"));
            }
            catch
            {
                _fileLogger = null;
            }

            _eChessBoardConfiguration = EChessBoardConfiguration.Load(_fileName);
            if (string.IsNullOrWhiteSpace(_eChessBoardConfiguration.FileName))
            {
                _eChessBoardConfiguration.PortName = "BTLE";
                _eChessBoardConfiguration.UseBluetooth = true;           
            }
           
            if (_eChessBoardConfiguration.ExtendedConfig == null || _eChessBoardConfiguration.ExtendedConfig.Length == 0)
            {
                _eChessBoardConfiguration.ExtendedConfig = new ExtendedEChessBoardConfiguration[] { new ExtendedEChessBoardConfiguration(false,true)
                                                               {
                                                                   Name = "BearChess",
                                                                   IsCurrent = true,
                                                                   ShowCurrentColor = true,
                                                               }};
            }
         
            _currentIndex = 0;
            for (int i = 0; i < _eChessBoardConfiguration.ExtendedConfig.Length; i++)
            {
                comboBoxSettings.Items.Add(_eChessBoardConfiguration.ExtendedConfig[i]);
                if (_eChessBoardConfiguration.ExtendedConfig[i].IsCurrent)
                {
                    _currentIndex = i;
                }
            }
            comboBoxSettings.SelectedIndex = _currentIndex;
            ShowCurrentConfig();
            _loaded = true;
            buttonShowHint.Visibility = Visibility.Hidden;
            buttonShowBook.Visibility = Visibility.Hidden;
            buttonShowInvalid.Visibility = Visibility.Hidden;
            buttonShowMoveFrom.Visibility = Visibility.Hidden;
            buttonShowTakeBack.Visibility = Visibility.Hidden;
            buttonShowCurrentGoodMoveEvaluation.Visibility = Visibility.Hidden;
            buttonShowCurrentPossibleMoves.Visibility = Visibility.Hidden;
            sliderDebounce.Value = _eChessBoardConfiguration.Debounce;
        }

        private void ShowCurrentConfig()
        {
            var current = comboBoxSettings.Items[_currentIndex] as ExtendedEChessBoardConfiguration;
            if (current == null)
            {
                return;
            }
            checkBoxUseLEDs.IsChecked = current.SendLedCommands;
            tabControlLEDs.IsEnabled = checkBoxUseLEDs.IsChecked.Value;

            checkBoxMoveFigures.IsChecked = current.AutoMoveFigures;
            checkBoxMoveLine.IsChecked = current.ShowMoveLine;

            radioButtonMoveFromRed.IsChecked = current.RGBMoveFrom[0] != '0';
            radioButtonMoveFromGreen.IsChecked = current.RGBMoveFrom[1] != '0';
            radioButtonMoveFromBlue.IsChecked = current.RGBMoveFrom[2] != '0';

            radioButtonMoveToRed.IsChecked = current.RGBMoveTo[0] != '0';
            radioButtonMoveToGreen.IsChecked = current.RGBMoveTo[1] != '0';
            radioButtonMoveToBlue.IsChecked = current.RGBMoveTo[2] != '0';

            radioButtonTakeBackRed.IsChecked = current.RGBTakeBack[0] != '0';
            radioButtonTakeBackGreen.IsChecked = current.RGBTakeBack[1] != '0';
            radioButtonTakeBackBlue.IsChecked = current.RGBTakeBack[2] != '0';

            radioButtonHintRed.IsChecked = current.RGBHelp[0] != '0';
            radioButtonHintGreen.IsChecked = current.RGBHelp[1] != '0';
            radioButtonHintBlue.IsChecked = current.RGBHelp[2] != '0';

            radioButtonBookRed.IsChecked = current.RGBBookMove[0] != '0';
            radioButtonBookGreen.IsChecked = current.RGBBookMove[1] != '0';
            radioButtonBookBlue.IsChecked = current.RGBBookMove[2] != '0';

            radioButtonInvalidRed.IsChecked = current.RGBInvalid[0] != '0';
            radioButtonInvalidGreen.IsChecked = current.RGBInvalid[1] != '0';
            radioButtonInvalidBlue.IsChecked = current.RGBInvalid[2] != '0';

            radioButtonPossibleMovesRed.IsChecked = current.RGBPossibleMoves[0] != '0';
            radioButtonPossibleMovesGreen.IsChecked = current.RGBPossibleMoves[1] != '0';
            radioButtonPossibleMovesBlue.IsChecked = current.RGBPossibleMoves[2] != '0';

            radioButtonGoodMoveEvaluationRed.IsChecked = current.RGBPossibleMovesGood[0] != '0';
            radioButtonGoodMoveEvaluationGreen.IsChecked = current.RGBPossibleMovesGood[1] != '0';
            radioButtonGoodMoveEvaluationBlue.IsChecked = current.RGBPossibleMovesGood[2] != '0';

            radioButtonBadMoveEvaluationRed.IsChecked = current.RGBPossibleMovesBad[0] != '0';
            radioButtonBadMoveEvaluationGreen.IsChecked = current.RGBPossibleMovesBad[1] != '0';
            radioButtonBadMoveEvaluationBlue.IsChecked = current.RGBPossibleMovesBad[2] != '0';

            radioButtonPlayableMoveEvaluationRed.IsChecked = current.RGBPossibleMovesPlayable[0] != '0';
            radioButtonPlayableMoveEvaluationGreen.IsChecked = current.RGBPossibleMovesPlayable[1] != '0';
            radioButtonPlayableMoveEvaluationBlue.IsChecked = current.RGBPossibleMovesPlayable[2] != '0';

            checkBoxPossibleMoves.IsChecked = current.ShowPossibleMoves;
            checkBoxPossibleMovesEval.IsChecked = current.ShowPossibleMovesEval;
            checkBoxOwnMoves.IsChecked = current.ShowOwnMoves;
            checkBoxPossibleMovesEval.IsEnabled = checkBoxPossibleMoves.IsChecked.Value;

            checkBoxTakeBackMove.IsChecked = current.ShowTakeBackMoves;
            checkBoxHintMoves.IsChecked = current.ShowHintMoves;
            checkBoxBookMoves.IsChecked = current.ShowBookMoves;

            checkBoxInvalidMoves.IsChecked = current.ShowInvalidMoves;
            ShowTooltip();
        }
        private void ShowTooltip()
        {
            buttonOk.ToolTip =
                $"{_rm.GetString("SelectAndSaveConfiguration")} '{((ExtendedEChessBoardConfiguration)comboBoxSettings.Items[_currentIndex]).Name}'";
        }

        private void ButtonShowDim_OnClick(object sender, RoutedEventArgs e)
        {
            if (_loader == null)
            {
                _loader = new ChessnutMoveLoader();
                buttonShowDim.Visibility = Visibility.Collapsed;
                buttonShowDim2.Visibility = Visibility.Visible;
                buttonShowHint.Visibility = Visibility.Visible;
                buttonShowInvalid.Visibility = Visibility.Visible;
                buttonShowMoveFrom.Visibility = Visibility.Visible;
                buttonShowTakeBack.Visibility = Visibility.Visible;
                buttonShowCurrentGoodMoveEvaluation.Visibility = Visibility.Visible;
                buttonShowCurrentPossibleMoves.Visibility = Visibility.Visible;
                buttonShowBook.Visibility = Visibility.Visible;
            }
            else
            {
                buttonShowDim.Visibility = Visibility.Visible;
                buttonShowDim2.Visibility = Visibility.Collapsed;
                _loader.SetAllLEDsOff(true);
                Thread.Sleep(500);
                _loader.Close();
                _loader = null;
                buttonShowHint.Visibility = Visibility.Hidden;
                buttonShowInvalid.Visibility = Visibility.Hidden;
                buttonShowMoveFrom.Visibility = Visibility.Hidden;
                buttonShowTakeBack.Visibility = Visibility.Hidden;
                buttonShowCurrentGoodMoveEvaluation.Visibility = Visibility.Hidden;
                buttonShowCurrentPossibleMoves.Visibility = Visibility.Hidden;
                buttonShowBook.Visibility = Visibility.Hidden;
            }
        }
        private void ShowExample(string buttonName)
        {
            var redFields = new StringBuilder();
            var greenFields = new StringBuilder();
            var blueFields = new StringBuilder();

            if (buttonName.Equals("buttonShowMoveFrom"))
            {
                string movesFrom = "E2";
                string movesTo = "E4";
                if (checkBoxMoveLine.IsChecked.Value)
                {
                    movesFrom = "E2E3";
                }

                if (radioButtonMoveFromRed.IsChecked.Value)
                {
                    redFields.Append(movesFrom);
                }
                if (radioButtonMoveFromGreen.IsChecked.Value)
                {
                    greenFields.Append(movesFrom);
                }
                if (radioButtonMoveFromBlue.IsChecked.Value)
                {
                    blueFields.Append(movesFrom);
                }
                if (radioButtonMoveToRed.IsChecked.Value)
                {
                    redFields.Append(movesTo);
                }
                if (radioButtonMoveToGreen.IsChecked.Value)
                {
                    greenFields.Append(movesTo);
                }
                if (radioButtonMoveToBlue.IsChecked.Value)
                {
                    blueFields.Append(movesTo);
                }
                _loader.AdditionalInformation($"R:{redFields} G:{greenFields} B:{blueFields} ");
                return;
            }
            if (buttonName.Equals("buttonShowInvalid"))
            {
                string moves = "E2E4";
                if (radioButtonInvalidRed.IsChecked.Value)
                {
                    redFields.Append(moves);
                }
                if (radioButtonInvalidGreen.IsChecked.Value)
                {
                    greenFields.Append(moves);
                }
                if (radioButtonInvalidBlue.IsChecked.Value)
                {
                    blueFields.Append(moves);
                }
                _loader.AdditionalInformation($"R:{redFields} G:{greenFields} B:{blueFields} ");
                return;
            }
            if (buttonName.Equals("buttonShowTakeBack"))
            {
                string moves = "E2E4";
                if (checkBoxMoveLine.IsChecked.Value)
                {
                    moves = "E2E3E4";
                }
                if (radioButtonTakeBackRed.IsChecked.Value)
                {
                    redFields.Append(moves);
                }
                if (radioButtonTakeBackGreen.IsChecked.Value)
                {
                    greenFields.Append(moves);
                }
                if (radioButtonTakeBackBlue.IsChecked.Value)
                {
                    blueFields.Append(moves);
                }
                _loader.AdditionalInformation($"R:{redFields} G:{greenFields} B:{blueFields} ");
                return;
            }
            if (buttonName.Equals("buttonShowHint"))
            {
                string moves = "E2E4";
                if (checkBoxMoveLine.IsChecked.Value)
                {
                    moves = "E2E3E4";
                }
              
                if (radioButtonHintRed.IsChecked.Value)
                {
                    redFields.Append(moves);
                }
                if (radioButtonHintGreen.IsChecked.Value)
                {
                    greenFields.Append(moves);
                }
                if (radioButtonHintBlue.IsChecked.Value)
                {
                    blueFields.Append(moves);
                }
                _loader.AdditionalInformation($"R:{redFields} G:{greenFields} B:{blueFields} ");
                return;
            }
            if (buttonName.Equals("buttonShowBook"))
            {
                string moves = "E2E4";
                if (checkBoxMoveLine.IsChecked.Value)
                {
                    moves = "E2E3E4";
                }
                if (radioButtonBookRed.IsChecked.Value)
                {
                    redFields.Append(moves);
                }
                if (radioButtonBookGreen.IsChecked.Value)
                {
                    greenFields.Append(moves);
                }
                if (radioButtonBookBlue.IsChecked.Value)
                {
                    blueFields.Append(moves);
                }
                _loader.AdditionalInformation($"R:{redFields} G:{greenFields} B:{blueFields} ");
                return;
            }
            if (buttonName.Equals("buttonShowCurrentPossibleMoves"))
            {
                string moves = "E2D3C4B5A6";
                if (radioButtonPossibleMovesRed.IsChecked.Value)
                {
                    redFields.Append(moves);
                }
                if (radioButtonPossibleMovesGreen.IsChecked.Value)
                {
                    greenFields.Append(moves);
                }
                if (radioButtonPossibleMovesBlue.IsChecked.Value)
                {
                    blueFields.Append(moves);
                }
                _loader.AdditionalInformation($"R:{redFields} G:{greenFields} B:{blueFields} ");
                return;
            }

            if (buttonName.Equals("buttonShowCurrentGoodMoveEvaluation"))
            {
              
                if (radioButtonGoodMoveEvaluationRed.IsChecked.Value)
                {
                    redFields.Append("C4B5");
                }
                if (radioButtonGoodMoveEvaluationGreen.IsChecked.Value)
                {
                    greenFields.Append("C4B5");
                }
                if (radioButtonGoodMoveEvaluationBlue.IsChecked.Value)
                {
                    blueFields.Append("C4B5");
                }
                if (radioButtonBadMoveEvaluationRed.IsChecked.Value)
                {
                    redFields.Append("A6");
                }
                if (radioButtonBadMoveEvaluationGreen.IsChecked.Value)
                {
                    greenFields.Append("A6");
                }
                if (radioButtonBadMoveEvaluationBlue.IsChecked.Value)
                {
                    blueFields.Append("A6");
                }
                if (radioButtonPlayableMoveEvaluationRed.IsChecked.Value)
                {
                    redFields.Append("E2D3");
                }
                if (radioButtonPlayableMoveEvaluationGreen.IsChecked.Value)
                {
                    greenFields.Append("E2D3");
                }
                if (radioButtonPlayableMoveEvaluationBlue.IsChecked.Value)
                {
                    blueFields.Append("E2D3");
                }
                _loader.AdditionalInformation($"R:{redFields} G:{greenFields} B:{blueFields} ");
                return;
            }

        }
        private void ButtonShowHideLEDs_OnClick(object sender, RoutedEventArgs e)
        {
            if (_loader == null)
            {
                return;
            }
            if (sender is Button button)
            {
                ShowExample(button.Name);

            }
        }

        private void CheckBoxPossibleMoves_OnUnchecked(object sender, RoutedEventArgs e)
        {
            checkBoxPossibleMovesEval.IsEnabled = false;
            checkBoxOwnMoves.IsEnabled = true;
        }

        private void CheckBoxPossibleMoves_OnChecked(object sender, RoutedEventArgs e)
        {
            checkBoxPossibleMovesEval.IsEnabled = true;
            checkBoxOwnMoves.IsChecked = false;
            checkBoxOwnMoves.IsEnabled = false;
        }

     

        private char ConvertToChar(bool isChecked)
        {
            return isChecked ? '1' : '0';
        }

        private void UpdateExtendedConfig(ExtendedEChessBoardConfiguration extendedConfiguration)
        {
            extendedConfiguration.ShowMoveLine = checkBoxMoveLine.IsChecked.HasValue && checkBoxMoveLine.IsChecked.Value;
            extendedConfiguration.RGBMoveFrom = $"{ConvertToChar(radioButtonMoveFromRed.IsChecked.Value)}{ConvertToChar(radioButtonMoveFromGreen.IsChecked.Value)}{ConvertToChar(radioButtonMoveFromBlue.IsChecked.Value)}";
            extendedConfiguration.RGBMoveTo = $"{ConvertToChar(radioButtonMoveToRed.IsChecked.Value)}{ConvertToChar(radioButtonMoveToGreen.IsChecked.Value)}{ConvertToChar(radioButtonMoveToBlue.IsChecked.Value)}";
            extendedConfiguration.RGBInvalid = $"{ConvertToChar(radioButtonInvalidRed.IsChecked.Value)}{ConvertToChar(radioButtonInvalidGreen.IsChecked.Value)}{ConvertToChar(radioButtonInvalidBlue.IsChecked.Value)}";
            extendedConfiguration.RGBHelp = $"{ConvertToChar(radioButtonHintRed.IsChecked.Value)}{ConvertToChar(radioButtonHintGreen.IsChecked.Value)}{ConvertToChar(radioButtonHintBlue.IsChecked.Value)}";
            extendedConfiguration.RGBBookMove = $"{ConvertToChar(radioButtonBookRed.IsChecked.Value)}{ConvertToChar(radioButtonBookGreen.IsChecked.Value)}{ConvertToChar(radioButtonBookBlue.IsChecked.Value)}";
            extendedConfiguration.RGBTakeBack = $"{ConvertToChar(radioButtonTakeBackRed.IsChecked.Value)}{ConvertToChar(radioButtonTakeBackGreen.IsChecked.Value)}{ConvertToChar(radioButtonTakeBackBlue.IsChecked.Value)}";
            extendedConfiguration.RGBPossibleMoves = $"{ConvertToChar(radioButtonPossibleMovesRed.IsChecked.Value)}{ConvertToChar(radioButtonPossibleMovesGreen.IsChecked.Value)}{ConvertToChar(radioButtonPossibleMovesBlue.IsChecked.Value)}";
            extendedConfiguration.RGBPossibleMovesGood = $"{ConvertToChar(radioButtonGoodMoveEvaluationRed.IsChecked.Value)}{ConvertToChar(radioButtonGoodMoveEvaluationGreen.IsChecked.Value)}{ConvertToChar(radioButtonGoodMoveEvaluationBlue.IsChecked.Value)}";
            extendedConfiguration.RGBPossibleMovesBad = $"{ConvertToChar(radioButtonBadMoveEvaluationRed.IsChecked.Value)}{ConvertToChar(radioButtonBadMoveEvaluationGreen.IsChecked.Value)}{ConvertToChar(radioButtonBadMoveEvaluationBlue.IsChecked.Value)}";
            extendedConfiguration.RGBPossibleMovesPlayable = $"{ConvertToChar(radioButtonPlayableMoveEvaluationRed.IsChecked.Value)}{ConvertToChar(radioButtonPlayableMoveEvaluationGreen.IsChecked.Value)}{ConvertToChar(radioButtonPlayableMoveEvaluationBlue.IsChecked.Value)}";
            extendedConfiguration.ShowPossibleMoves = checkBoxPossibleMoves.IsChecked.HasValue && checkBoxPossibleMoves.IsChecked.Value;
            extendedConfiguration.ShowPossibleMovesEval = checkBoxPossibleMovesEval.IsChecked.HasValue && checkBoxPossibleMovesEval.IsChecked.Value;
            extendedConfiguration.ShowOwnMoves = checkBoxOwnMoves.IsChecked.HasValue && checkBoxOwnMoves.IsChecked.Value;
            extendedConfiguration.ShowTakeBackMoves = checkBoxTakeBackMove.IsChecked.HasValue && checkBoxTakeBackMove.IsChecked.Value;
            extendedConfiguration.ShowHintMoves = checkBoxHintMoves.IsChecked.HasValue && checkBoxHintMoves.IsChecked.Value;
            extendedConfiguration.ShowBookMoves = checkBoxBookMoves.IsChecked.HasValue && checkBoxBookMoves.IsChecked.Value;
            extendedConfiguration.ShowInvalidMoves = checkBoxInvalidMoves.IsChecked.HasValue && checkBoxInvalidMoves.IsChecked.Value;
            extendedConfiguration.AutoMoveFigures = checkBoxMoveFigures.IsChecked.HasValue && checkBoxMoveFigures.IsChecked.Value;
            extendedConfiguration.SendLedCommands = checkBoxUseLEDs.IsChecked.HasValue && checkBoxUseLEDs.IsChecked.Value;
            _eChessBoardConfiguration.PortName = "BTLE";            
            _eChessBoardConfiguration.ShowPossibleMoves = extendedConfiguration.ShowPossibleMoves;
            _eChessBoardConfiguration.ShowPossibleMovesEval = extendedConfiguration.ShowPossibleMovesEval;
            _eChessBoardConfiguration.ShowOwnMoves = extendedConfiguration.ShowOwnMoves;
            _eChessBoardConfiguration.ShowHintMoves = extendedConfiguration.ShowHintMoves;
            _eChessBoardConfiguration.Debounce = (int)sliderDebounce.Value;
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            if (_loader != null)
            {
                _loader?.SetAllLEDsOff(true);
                Thread.Sleep(500);
                _loader?.Close();
            }            
            UpdateExtendedConfig(comboBoxSettings.Items[_currentIndex] as ExtendedEChessBoardConfiguration);
            _eChessBoardConfiguration.ExtendedConfig = new ExtendedEChessBoardConfiguration[comboBoxSettings.Items.Count];
            for (int i = 0; i < comboBoxSettings.Items.Count; i++)
            {
                _eChessBoardConfiguration.ExtendedConfig[i] = comboBoxSettings.Items[i] as ExtendedEChessBoardConfiguration;
            }
            EChessBoardConfiguration.Save(_eChessBoardConfiguration, _fileName);
            DialogResult = true;
        }

        private void ComboBoxSettings_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_loaded)
            {
                return;
            }
            UpdateExtendedConfig(comboBoxSettings.Items[_currentIndex] as ExtendedEChessBoardConfiguration);
            ((ExtendedEChessBoardConfiguration)comboBoxSettings.Items[_currentIndex]).IsCurrent = false;
            _currentIndex = comboBoxSettings.SelectedIndex;
            ((ExtendedEChessBoardConfiguration)comboBoxSettings.Items[_currentIndex]).IsCurrent = true;
            ShowCurrentConfig();
        }

        private void ButtonSaveAsNew_OnClick(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditWindow()
            {
                Owner = this
            };
            editWindow.SetTitle(_rm.GetString("GiveConfigurationName"));
            var showDialog = editWindow.ShowDialog();
            if (showDialog.HasValue && showDialog.Value)
            {
                if (string.IsNullOrWhiteSpace(editWindow.Comment))
                {
                    return;
                }
                ((ExtendedEChessBoardConfiguration)comboBoxSettings.Items[_currentIndex]).IsCurrent = false;
                var extendedEChessBoardConfiguration = new ExtendedEChessBoardConfiguration(false,true)
                {
                    Name = editWindow.Comment,
                    IsCurrent = true
                };
                _loaded = false;
                comboBoxSettings.Items.Add(extendedEChessBoardConfiguration);
                comboBoxSettings.SelectedIndex = comboBoxSettings.Items.Count - 1;
                _currentIndex = comboBoxSettings.Items.Count - 1;
                UpdateExtendedConfig(comboBoxSettings.Items[_currentIndex] as ExtendedEChessBoardConfiguration);
                ShowCurrentConfig();
                _loaded = true;
            }
        }

        private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentIndex == 0)
            {
                MessageBox.Show(_rm.GetString("CannotDeleteBearChessConfig"), _rm.GetString("NotAllowed"), MessageBoxButton.OK,
                                MessageBoxImage.Hand);
                return;
            }
            if (MessageBox.Show($"{_rm.GetString("DeleteConfiguration")} '{((ExtendedEChessBoardConfiguration)comboBoxSettings.Items[_currentIndex]).Name}' ?", _rm.GetString("Delete"), MessageBoxButton.YesNo,
                                MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                _loaded = false;
                comboBoxSettings.Items.RemoveAt(_currentIndex);
                _currentIndex = 0;
                comboBoxSettings.SelectedIndex = 0;
                ((ExtendedEChessBoardConfiguration)comboBoxSettings.Items[_currentIndex]).IsCurrent = true;
                ShowCurrentConfig();
                _loaded = true;
            }
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            if (_loader != null)
            {
                _loader?.SetAllLEDsOff(true);
                Thread.Sleep(1000);
                _loader?.Close();
            }

            DialogResult = false;
        }

        private void checkBoxUseLEDsChecked(object sender, RoutedEventArgs e)
        {
            tabControlLEDs.IsEnabled = true;
        }

        private void checkBoxUseLEDsUnChecked(object sender, RoutedEventArgs e)
        {
            tabControlLEDs.IsEnabled = false;
        }

        private void SliderDebounce_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetDebounceText();
        }

        private void SetDebounceText()
        {
            if (textBlockDebounce != null)
            {
                textBlockDebounce.Text = ((int)sliderDebounce.Value).ToString();
            }
        }

        private void ButtonDebounceAdd_OnClick(object sender, RoutedEventArgs e)
        {
            if (sliderDebounce.Value < sliderDebounce.Maximum)
            {
                sliderDebounce.Value++;
            }
        }

        private void ButtonDebounceDelete_OnClick(object sender, RoutedEventArgs e)
        {
            if (sliderDebounce.Value > sliderDebounce.Minimum)
            {
                sliderDebounce.Value--;
            }
        }

        private void ButtonResetDebounce_OnClick(object sender, RoutedEventArgs e)
        {
            sliderDebounce.Value = 1;
        }

    }
}
