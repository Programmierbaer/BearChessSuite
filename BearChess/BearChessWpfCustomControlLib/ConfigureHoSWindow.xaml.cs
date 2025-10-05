
using System;
using System.IO;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChess.HoSLoader;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessTools;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für ConfigureHoSWindow.xaml
    /// </summary>
    public partial class ConfigureHoSWindow : Window
    {
        private readonly string _fileName;
        private readonly EChessBoardConfiguration _eChessBoardConfiguration;
        private const int _defaultScanTime = 2500;
        private const int _defaultScanIncr = 50;
        private readonly ResourceManager _rm;

        public string SelectedPortName => comboBoxComPorts.SelectedIndex == 0 ? "BTLE" : "HID";

        public ConfigureHoSWindow(Configuration configuration) : this (configuration, configuration.FolderPath)
        {

        }

        public ConfigureHoSWindow(Configuration configuration, string configPath)
        {
            InitializeComponent();
            _rm = SpeechTranslator.ResourceManager;
            _fileName = Path.Combine(configPath, HoSLoader.EBoardName, $"{HoSLoader.EBoardName}Cfg.xml");
            var fileInfo = new FileInfo(_fileName);
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
                Directory.CreateDirectory(Path.Combine(fileInfo.DirectoryName, "log"));
            }
            _eChessBoardConfiguration = EChessBoardConfiguration.Load(_fileName);            
            checkBoxDefault.IsChecked = true;
            checkBoxOwnMoves.IsChecked = _eChessBoardConfiguration.ShowOwnMoves;
            checkBoxPossibleMoves.IsChecked = _eChessBoardConfiguration.ShowPossibleMoves;
            checkBoxBestMove.IsChecked = _eChessBoardConfiguration.ShowPossibleMovesEval;
            sliderScanTime.Value = _eChessBoardConfiguration.Debounce >= 1000 ? _eChessBoardConfiguration.Debounce : 2500;
            comboBoxComPorts.Items.Add("BTLE");
            comboBoxComPorts.Items.Add("USB");
            comboBoxComPorts.SelectedIndex = _eChessBoardConfiguration.UseBluetooth ? 0 : 1;
            borderDelay.IsEnabled = _eChessBoardConfiguration.UseBluetooth;
            CheckBoxUseClock.IsChecked = _eChessBoardConfiguration.UseClock;
            RadioButtonUseExtern.IsChecked = _eChessBoardConfiguration.ClockUseExtern;
            RadioButtonUseIntern.IsChecked = _eChessBoardConfiguration.ClockUseIntern;
            RadioButtonUseExtern.IsEnabled = _eChessBoardConfiguration.UseClock;
            RadioButtonUseIntern.IsEnabled = _eChessBoardConfiguration.UseClock;
            checkBoxSwitchSide.IsChecked = !_eChessBoardConfiguration.ClockSwitchSide;
            checkBoxSwitchSide.IsEnabled = _eChessBoardConfiguration.UseClock;
            SetScanText();
        }
        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            _eChessBoardConfiguration.ShowOwnMoves = checkBoxOwnMoves.IsChecked.HasValue && checkBoxOwnMoves.IsChecked.Value;
            _eChessBoardConfiguration.ShowPossibleMoves = checkBoxPossibleMoves.IsChecked.HasValue && checkBoxPossibleMoves.IsChecked.Value;
            _eChessBoardConfiguration.ShowPossibleMovesEval = checkBoxBestMove.IsChecked.HasValue && checkBoxBestMove.IsChecked.Value;
            _eChessBoardConfiguration.UseBluetooth = comboBoxComPorts.SelectedIndex == 0;
            _eChessBoardConfiguration.Debounce = (int)sliderScanTime.Value;
            _eChessBoardConfiguration.UseClock = CheckBoxUseClock.IsChecked.HasValue && CheckBoxUseClock.IsChecked.Value;
            _eChessBoardConfiguration.ClockUseExtern = RadioButtonUseExtern.IsChecked.HasValue && RadioButtonUseExtern.IsChecked.Value;
            _eChessBoardConfiguration.ClockUseIntern = RadioButtonUseIntern.IsChecked.HasValue && RadioButtonUseIntern.IsChecked.Value;
            _eChessBoardConfiguration.ClockSwitchSide = !(checkBoxSwitchSide.IsChecked.HasValue && checkBoxSwitchSide.IsChecked.Value);
            EChessBoardConfiguration.Save(_eChessBoardConfiguration, _fileName);
            DialogResult = true;
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void ButtonTimeDelete_OnClick(object sender, RoutedEventArgs e)
        {
            if (sliderScanTime.Value >= sliderScanTime.Minimum + _defaultScanIncr)
            {
                sliderScanTime.Value -= _defaultScanIncr;
            }
            else
            {
                sliderScanTime.Value = sliderScanTime.Minimum;
            }
        }

        private void ButtonTimeAdd_OnClick(object sender, RoutedEventArgs e)
        {
            if (sliderScanTime.Value <= sliderScanTime.Maximum - _defaultScanIncr)
            {
                sliderScanTime.Value += _defaultScanIncr;
            }
            else
            {
                sliderScanTime.Value = sliderScanTime.Maximum;
            }
        }

        private void SetScanText()
        {
            if (textBlockScansPerSec != null)
            {
                textBlockScansPerSec.Text = $"{_rm.GetString("Every")} {sliderScanTime.Value.ToString("####")} ms.";
            }
        }

        private void SliderScan_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetScanText();
        }

        private void ButtonResetScan_OnClick(object sender, RoutedEventArgs e)
        {
            sliderScanTime.Value = _defaultScanTime;
        }

        private void ComboBoxComPorts_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            borderDelay.IsEnabled = comboBoxComPorts.SelectedIndex == 0;
        }

        private void CheckBoxUseClock_OnChecked(object sender, RoutedEventArgs e)
        {
            RadioButtonUseExtern.IsEnabled = true;
            RadioButtonUseIntern.IsEnabled = true;
            checkBoxSwitchSide.IsEnabled = true;
        }

        private void CheckBoxUseClock_OnUnchecked(object sender, RoutedEventArgs e)
        {
            RadioButtonUseExtern.IsEnabled = false;
            RadioButtonUseIntern.IsEnabled = false;
            checkBoxSwitchSide.IsEnabled = false;
        }
    }
}
