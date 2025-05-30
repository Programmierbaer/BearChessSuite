﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Resources;
using www.SoLaNoSoft.com.BearChessTools;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using System.Threading;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChess.CertaboLoader;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für ConfigureCertaboWindow.xaml
    /// </summary>
    public partial class ConfigureCertaboWindow : Window
    {
        private readonly ResourceManager _rm;
        private readonly Configuration _configuration;
        private readonly string _fileName;
        private readonly string _calibrateFileName;
        private readonly EChessBoardConfiguration _eChessBoardConfiguration;
        private List<string> _portNames;
        private List<string> _allPortNames;
        private readonly ILogging _fileLogger;

        public string SelectedPortName => (string)comboBoxComPorts.SelectedItem;

        public ConfigureCertaboWindow(Configuration configuration, bool useBluetooth, bool useBluetoothLE, bool useChesstimation) : this(configuration,useBluetooth,useBluetoothLE,useChesstimation, configuration.FolderPath) 
        {
        }
        public ConfigureCertaboWindow(Configuration configuration, bool useBluetooth, bool useBluetoothLE, bool useChesstimation, string configPath)
        {
            InitializeComponent();
            _configuration = configuration;
            _rm = Properties.Resources.ResourceManager;
            _rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, false, true);
            SpeechTranslator.ResourceManager = _rm;
            _rm = SpeechTranslator.ResourceManager;
            _fileName = Path.Combine(configPath, CertaboLoader.EBoardName,
                                   $"{CertaboLoader.EBoardName}Cfg.xml");
            var fileInfo = new FileInfo(_fileName);
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
                Directory.CreateDirectory(Path.Combine(fileInfo.DirectoryName, "log"));
            }

            try
            {
                _fileLogger = new FileLogger(Path.Combine(fileInfo.DirectoryName, "log", "CertaboCfg.log"), 10, 10);
                _fileLogger.Active = bool.Parse(configuration.GetConfigValue("writeLogFiles", "true"));
            }
            catch
            {
                _fileLogger = null;
            }
            _eChessBoardConfiguration = EChessBoardConfiguration.Load(_fileName);
            _eChessBoardConfiguration.UseChesstimation = useChesstimation;
            checkBoxMoveLine.IsChecked = _eChessBoardConfiguration.ShowMoveLine;
            checkBoxOwnMoves.IsChecked = _eChessBoardConfiguration.ShowOwnMoves;
            checkBoxPossibleMoves.IsChecked = _eChessBoardConfiguration.ShowPossibleMoves;
            checkBoxBestMove.IsChecked = _eChessBoardConfiguration.ShowPossibleMovesEval;
            _allPortNames = new List<string> { "<auto>" };
            if (useBluetooth || useBluetoothLE)
            {
                var comPortSearchWindow = new COMPortSearchWindow();
                comPortSearchWindow.Show();
                _portNames = SerialCommunicationTools
                             .GetBTComPort(CertaboLoader.EBoardName, configuration, _fileLogger, useBluetooth, useBluetoothLE, useChesstimation).ToList();
                comPortSearchWindow.Close();

            }
            else
            {
                if (useChesstimation)
                {
                    _portNames = SerialCommunicationTools.GetPortNames("CH340").ToList();
                }
                else
                {
                    _portNames = SerialCommunicationTools.GetPortNames("Silicon Labs").ToList();
                }
            }

            _portNames.ForEach(f => _allPortNames.Add(f));
            comboBoxComPorts.ItemsSource = _allPortNames;
            comboBoxComPorts.SelectedIndex = 0;

            _calibrateFileName = Path.Combine(configPath, CertaboLoader.EBoardName, "calibrate.xml");

            _eChessBoardConfiguration.UseBluetooth = useBluetooth || useBluetoothLE;
            textBlockCalibrate.Text = useChesstimation || File.Exists(_calibrateFileName) ? _rm.GetString("IsCalibrated") : _rm.GetString("IsNotCalibrated");
            textBlockCurrentPort.Text = _eChessBoardConfiguration.PortName;
            if (_portNames.Count == 0)
            {
                textBlockInformation.Visibility = Visibility.Visible;
            }
            else
            {
                comboBoxComPorts.SelectedIndex = _allPortNames.IndexOf(_eChessBoardConfiguration.PortName);
            }
            borderChesstimation.Visibility = useChesstimation ? Visibility.Visible : Visibility.Collapsed;
            buttonCalibrate.IsEnabled = !useChesstimation;
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            _eChessBoardConfiguration.PortName = comboBoxComPorts.SelectionBoxItem.ToString();
            _eChessBoardConfiguration.ShowMoveLine = checkBoxMoveLine.IsChecked.HasValue && checkBoxMoveLine.IsChecked.Value;
            _eChessBoardConfiguration.ShowOwnMoves = checkBoxOwnMoves.IsChecked.HasValue && checkBoxOwnMoves.IsChecked.Value;
            _eChessBoardConfiguration.ShowPossibleMoves = checkBoxPossibleMoves.IsChecked.HasValue && checkBoxPossibleMoves.IsChecked.Value;
            _eChessBoardConfiguration.ShowPossibleMovesEval = checkBoxBestMove.IsChecked.HasValue && checkBoxBestMove.IsChecked.Value;
            EChessBoardConfiguration.Save(_eChessBoardConfiguration, _fileName);
            DialogResult = true;
        }


        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonCalibrate_OnClick(object sender, RoutedEventArgs e)
        {
            var calibrateBaseWindow = new CalibrateBaseWindow { Owner = this };
            var showDialog = calibrateBaseWindow.ShowDialog();
            if (!showDialog.HasValue || !showDialog.Value)
            {
                return;
            }

            var infoWindow = new InfoWindow
            {
                Owner = this
            };
            infoWindow.Show();
            _eChessBoardConfiguration.PortName = comboBoxComPorts.SelectionBoxItem.ToString();
            EChessBoardConfiguration.Save(_eChessBoardConfiguration, _fileName);
            try
            {
                var certaboLoader = new CertaboLoader();
                certaboLoader.Calibrate();
                certaboLoader.SetAllLedsOn();
                certaboLoader.SetAllLedsOff(false);
                certaboLoader.SetAllLedsOff(false);
                infoWindow.Close();
                certaboLoader.Stop();
                certaboLoader.SetAllLedsOff(false);
                MessageBox.Show(this, $"{_rm.GetString("CalibrationMsg")} {_rm.GetString("Finished")}", _rm.GetString("CalibrationMsg"), MessageBoxButton.OK,
                    MessageBoxImage.Information);
                certaboLoader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"{ex.Message}", _rm.GetString("CalibrationMsg"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            textBlockCalibrate.Text = File.Exists(_calibrateFileName) ? _rm.GetString("IsCalibrated") : _rm.GetString("IsNotCalibrated");

        }

        private void ButtonCheck_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var infoWindow = new ProgressWindow()
                {
                    Owner = this
                };
                var certaboLoader = new CertaboLoader(true, CertaboLoader.EBoardName);
                var portName = comboBoxComPorts.SelectionBoxItem.ToString();
                if (portName.Contains("auto"))
                {
                    infoWindow.SetMaxValue(_portNames.Count);
                    infoWindow.Show();
                    var i = 0;
                    foreach (var name in _portNames)
                    {
                        infoWindow.SetCurrentValue(i, name);
                        infoWindow.SetCurrentValue(i);
                        i++;
                        if (certaboLoader.CheckComPort(name))
                        {
                            infoWindow.Close();
                            MessageBox.Show($"{_rm.GetString("CheckConnectionSuccess")} {name}", _rm.GetString("Check"), MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            comboBoxComPorts.SelectedIndex = _allPortNames.IndexOf(name);
                            return;
                        }
                    }

                    infoWindow.Close();
                    MessageBox.Show(_rm.GetString("CheckConnectionFailedForAll"), _rm.GetString("Check"), MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;

                }

                infoWindow.SetMaxValue(1);
                infoWindow.Show();
                infoWindow.SetCurrentValue(1, portName);
                if (certaboLoader.CheckComPort(portName))
                {
                    infoWindow.Close();
                    MessageBox.Show($"{_rm.GetString("CheckConnectionSuccess")} {portName}", _rm.GetString("Check"), MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    infoWindow.Close();
                    MessageBox.Show($"{_rm.GetString("CheckConnectionFailed")} {portName}", _rm.GetString("Check"), MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _fileLogger?.LogError(ex);
            }
        }

        private void CheckBoxOwnMoves_OnUnchecked(object sender, RoutedEventArgs e)
        {
            checkBoxBestMove.IsEnabled = true;
            checkBoxPossibleMoves.IsEnabled = true;
        }

        private void CheckBoxOwnMoves_OnChecked(object sender, RoutedEventArgs e)
        {
            checkBoxBestMove.IsEnabled = false;
            checkBoxPossibleMoves.IsEnabled = false;
        }

        private void CheckBoxPossibleMoves_OnChecked(object sender, RoutedEventArgs e)
        {
            checkBoxOwnMoves.IsEnabled = false;
            checkBoxOwnMoves.IsChecked = false;
        }

        private void CheckBoxPossibleMoves_OnUnchecked(object sender, RoutedEventArgs e)
        {
            checkBoxOwnMoves.IsEnabled = !checkBoxBestMove.IsChecked.Value;
        }

        private void CheckBoxBesteMove_OnChecked(object sender, RoutedEventArgs e)
        {
            checkBoxOwnMoves.IsEnabled = false;
            checkBoxOwnMoves.IsChecked = false;
        }

        private void CheckBoxBesteMove_OnUnchecked(object sender, RoutedEventArgs e)
        {
            checkBoxOwnMoves.IsEnabled = !checkBoxPossibleMoves.IsChecked.Value;
        }

    }
}
