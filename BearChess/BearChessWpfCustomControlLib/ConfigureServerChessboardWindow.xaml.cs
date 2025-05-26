using System;
using System.Windows;
using System.IO;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChess.CertaboLoader;
using www.SoLaNoSoft.com.BearChess.IChessOneLoader;
using www.SoLaNoSoft.com.BearChess.TabuTronic.Tactum.Loader;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChess.DGTLoader;
using www.SoLaNoSoft.com.BearChess.ChessnutAirLoader;
using www.SoLaNoSoft.com.BearChess.HoSLoader;
using www.SoLaNoSoft.com.BearChess.MChessLinkLoader;
using www.SoLaNoSoft.com.BearChess.Tabutronic.Cerno.Loader;
using www.SoLaNoSoft.com.BearChess.Tabutronic.Sentio.Loader;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{

    /// <summary>
    /// Interaktionslogik für ConfigureServerChessboardWindow.xaml
    /// </summary>
    public partial class ConfigureServerChessboardWindow : Window
    {


        public class BoardSelection
        {
            public string BoardName
            {
                get;
                set;
            }

            public bool BluetoothLE
            {
                get;
                set;
            }

            public bool BluetoothClassic
            {
                get;
                set;
            }

            public BoardSelection(string boardName, bool bluetoothClassic, bool bluetoothLE)
            {
                BoardName = boardName;
                BluetoothLE = bluetoothLE;
                BluetoothClassic = bluetoothClassic;
            }

            public override string ToString()
            {
                var bluetooth = BluetoothLE ? "BTLE" : BluetoothClassic ? "BT" : string.Empty;
                return $"{BoardName} {bluetooth}".Trim();
            }
        }

        private readonly string _configPathWhite;
        private readonly string _configPathBlack;
        private bool _isInitialized = false;
        private readonly ILogging _logging;

        public string WhiteConnectionId { get; private set; }
        public string BlackConnectionId { get; private set; }

        public IElectronicChessBoard WhiteEBoard { get; private set; }
        public IElectronicChessBoard BlackEBoard { get; private set; }

        public ConfigureServerChessboardWindow(string serverBoardId, BearChessClientInformation[] bcClientToken, ILogging logging)
        {
            InitializeComponent();
            var serverPath = Path.Combine(Configuration.Instance.FolderPath,"server");
            var configPath = Path.Combine(serverPath, "boards", serverBoardId);
            _logging = logging;
            _configPathWhite = Path.Combine(configPath,"white");
            _configPathBlack = Path.Combine(configPath, "black");
            comboboxWhiteEBoardNames.Items.Clear();
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.Certabo, false, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.Certabo, false, true));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.ChessnutAir, false, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.ChessnutAir, false, true));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.ChessnutEvo,false,false));            
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.DGT, false, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.DGT, false, true));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.IChessOne, false, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.IChessOne, false, true));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.MChessLink, false, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.MChessLink, true, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.MChessLink, false, true));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicCerno, false, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicCerno, false, true));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicCernoSpectrum, false, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicCernoSpectrum, false, true));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicTactum, false, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicTactum, false, true));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.Zmartfun, false, false));
            comboboxWhiteEBoardNames.Items.Add(new BoardSelection(Constants.Zmartfun, false, true));
            comboboxBlackEBoardNames.Items.Clear();
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.Certabo, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.Certabo, false, true));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.ChessnutAir, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.ChessnutAir, false, true));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.ChessnutEvo, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.DGT, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.DGT, false, true));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.IChessOne, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.IChessOne, false, true));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.MChessLink, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.MChessLink, true, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.MChessLink, false, true));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicCerno, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicCerno, false, true));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicCernoSpectrum, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicCernoSpectrum, false, true));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicTactum, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.TabutronicTactum, false, true));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.Zmartfun, false, false));
            comboboxBlackEBoardNames.Items.Add(new BoardSelection(Constants.Zmartfun, false, true));
            for (int i=0; i< bcClientToken.Length; i++)
            {
                comboboxWhiteBCNames.Items.Add(bcClientToken[i]);
                comboboxBlackBCNames.Items.Add(bcClientToken[i]);
            }
            WhiteEBoard = null;
            BlackEBoard = null;
            WhiteConnectionId = string.Empty;
            BlackConnectionId = string.Empty;
            comboboxWhiteBCNames.IsEnabled = false;
            comboboxBlackEBoardNames.IsEnabled = true;
            ButtonConfigureWhiteConnection.IsEnabled = true;
            CheckBoxSameConnection.IsChecked = true;
            radioButtonWhiteDirectConnected.IsChecked = true;
            BorderBlack.IsEnabled = false;
            _isInitialized = true;
        }


        private bool ConfigureBoardConnection(BoardSelection selectedBoard, string configPath, bool forWhite)
        {
            _logging.LogDebug($"Configure {selectedBoard}");
            if (selectedBoard.BoardName.Equals(Constants.IChessOne))
            { 
                var configIChessOne = new ConfigureIChessOneWindow(Configuration.Instance, selectedBoard.BluetoothLE, configPath) { Owner = this};
                var dialogResult = configIChessOne.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        WhiteEBoard = new IChessOneLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new IChessOneLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }
                    
                    return true;
                }

                return false;
            }

            if (selectedBoard.BoardName.Equals(Constants.Certabo))
            {
                var configCertabo = new ConfigureCertaboWindow(Configuration.Instance, selectedBoard.BluetoothClassic, selectedBoard.BluetoothLE, false, configPath) { Owner = this };
                var dialogResult = configCertabo.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        WhiteEBoard = new CertaboLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new CertaboLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }

                    return true;
                }

                return false;
            }

            if (selectedBoard.BoardName.Equals(Constants.DGT))
            {
                var configDGT = new ConfigureDGTWindow(Configuration.Instance, selectedBoard.BluetoothLE , configPath) { Owner = this };
                var dialogResult = configDGT.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        
                        WhiteEBoard = new DGTLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new DGTLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }

                    return true;

                }

                return false;
            }

            if (selectedBoard.BoardName.Equals(Constants.ChessnutAir))
            {
                var configChessnut =
                    new ConfigureChessnutWindow(Constants.ChessnutAir, Configuration.Instance, selectedBoard.BluetoothLE, configPath) { Owner = this };
                var dialogResult = configChessnut.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        WhiteEBoard = new ChessnutAirLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new ChessnutAirLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }

                    return true;

                }

                return false;
            }

            if (selectedBoard.BoardName.Equals(Constants.ChessnutEvo))
            {
                var configEvo = new ConfigureChessnutEvoWindow(Configuration.Instance, configPath) { Owner = this };
                var dialogResult = configEvo.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        WhiteEBoard = new ChessnutEvoLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new ChessnutEvoLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }

                    return true;
                }

                return false;
            }

            if (selectedBoard.BoardName.Equals(Constants.MChessLink))
            {
                var configWindow =
                    new ConfigureMChessLinkWindow(Configuration.Instance, selectedBoard.BluetoothClassic, selectedBoard.BluetoothLE, false, false, configPath) { Owner = this };
                var dialogResult = configWindow.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        WhiteEBoard = new MChessLinkLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new MChessLinkLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }

                    return true;
                }

                return false;
            }

            if (selectedBoard.BoardName.Equals(Constants.TabutronicCerno))
            {
                var configWindow = new ConfigureCernoWindow(Configuration.Instance, selectedBoard.BluetoothClassic, selectedBoard.BluetoothLE, configPath) { Owner = this };
                var dialogResult = configWindow.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        WhiteEBoard = new TabutronicCernoLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new TabutronicCernoLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }

                    return true;
                }

                return false;
            }

            if (selectedBoard.BoardName.Equals(Constants.TabutronicCernoSpectrum))
            {
                var configWindow = new ConfigureCernoSpectrumWindow(Configuration.Instance, selectedBoard.BluetoothClassic, selectedBoard.BluetoothLE, configPath) { Owner = this };
                var dialogResult = configWindow.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        WhiteEBoard = new TabutronicCernoSpectrumLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new TabutronicCernoSpectrumLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }
                    return true;
                }
                return false;
            }

            if (selectedBoard.BoardName.Equals(Constants.TabutronicTactum))
            {
                var configWindow = new ConfigureTactumWindow(Configuration.Instance, selectedBoard.BluetoothLE, configPath) { Owner = this };
                var dialogResult = configWindow.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        WhiteEBoard = new TabuTronicTactumLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new TabuTronicTactumLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }

                    return true;
                }

                return false;
            }

            if (selectedBoard.BoardName.Equals(Constants.Zmartfun))
            {
                var configWindow = new ConfigureHoSWindow(Configuration.Instance, configPath) { Owner = this };
                var dialogResult = configWindow.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    if (forWhite)
                    {
                        WhiteEBoard = new HoSLoader(configPath);
                        WhiteEBoard.Identification = Guid.NewGuid().ToString("N");
                        WhiteConnectionId = WhiteEBoard.Identification;
                    }
                    else
                    {
                        BlackEBoard = new HoSLoader(configPath);
                        BlackEBoard.Identification = Guid.NewGuid().ToString("N");
                        BlackConnectionId = BlackEBoard.Identification;
                    }

                    return true;
                }

                return false;
            }

            return false;
        }

        private void ButtonConfigureWhiteConnection_Click(object sender, RoutedEventArgs e)
        {
            WhiteConnectionId = string.Empty;
            if (comboboxWhiteEBoardNames.SelectedItem==null)
            {
                return;
            }
            var selectedBoard = comboboxWhiteEBoardNames.SelectedItem as BoardSelection;
            if (ConfigureBoardConnection(selectedBoard, _configPathWhite, true))
            {
                TextBlockPortNameWhite.Text = WhiteConnectionId;
            }

        }

        private void ButtonConfigureBlackConnection_Click(object sender, RoutedEventArgs e)
        {
            BlackConnectionId = string.Empty;
            if (comboboxBlackEBoardNames.SelectedItem == null)
            {
                return;
            }
            var selectedBoard = comboboxBlackEBoardNames.SelectedItem as BoardSelection;
            if (ConfigureBoardConnection(selectedBoard, _configPathBlack, false))
            {
                TextBlockPortNameBlack.Text = BlackConnectionId;
            }
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            var sameConnection = CheckBoxSameConnection.IsChecked.HasValue && CheckBoxSameConnection.IsChecked.Value;
            if (radioButtonWhiteConnectedViaBC.IsChecked.HasValue && radioButtonWhiteConnectedViaBC.IsChecked.Value)
            {
                if (comboboxWhiteBCNames?.SelectedItem != null)
                {
                    WhiteConnectionId = (comboboxWhiteBCNames.SelectedItem as BearChessClientInformation).Address;
                }
            }
            if (sameConnection)
            {
                BlackConnectionId = WhiteConnectionId;
                if (WhiteEBoard != null)
                {
                    BlackEBoard = WhiteEBoard;
                }
            }
            else
            {
                if (radioButtonBlackConnectedViaBC.IsChecked.HasValue && radioButtonBlackConnectedViaBC.IsChecked.Value)
                {
                    if (comboboxBlackBCNames?.SelectedItem != null)
                    {
                        BlackConnectionId = (comboboxBlackBCNames.SelectedItem as BearChessClientInformation).Address;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(WhiteConnectionId) || string.IsNullOrWhiteSpace(BlackConnectionId))
            {
                return;
            }
            DialogResult = true;
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult= false;
        }

        private void radioButtonWhiteDirectConnected_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
            {
                comboboxWhiteBCNames.IsEnabled = false;
                comboboxWhiteEBoardNames.IsEnabled = true;
                ButtonConfigureWhiteConnection.IsEnabled = true;
            }
        }

        private void radioButtonWhiteConnectedViaBC_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
            {
                comboboxWhiteBCNames.IsEnabled = true;
                comboboxWhiteEBoardNames.IsEnabled = false;
                ButtonConfigureWhiteConnection.IsEnabled = false;
            }
        }

        private void CheckBoxSameConnection_OnChecked(object sender, RoutedEventArgs e)
        {
            BorderBlack.IsEnabled = false;
        }

        private void CheckBoxSameConnection_OnUnchecked(object sender, RoutedEventArgs e)
        {
            BorderBlack.IsEnabled = true;
        }
    }
}
