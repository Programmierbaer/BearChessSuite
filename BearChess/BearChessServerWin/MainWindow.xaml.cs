using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessServerLib;
using www.SoLaNoSoft.com.BearChessServerWin.UserControls;
using www.SoLaNoSoft.com.BearChessServerWin.Windows;
using www.SoLaNoSoft.com.BearChessTools;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;

namespace www.SoLaNoSoft.com.BearChessServerWin
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {



        private readonly IBearChessController _bearChessController;
        private readonly List<BearChessClientInformation> _tokenList = new List<BearChessClientInformation>();
        private readonly List<TournamentWindow> _tournamentWindowsList = new List<TournamentWindow>();
        private readonly ResourceManager _rm;
        private readonly ILogging _logging;

        public MainWindow()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Configuration.BearChessProgramAssemblyName = assembly.FullName;
            var configValueLanguage = Configuration.Instance.GetConfigValue("Language", "default");
            if (!configValueLanguage.Equals("default"))
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(configValueLanguage);
            }
            _rm = Properties.Resources.ResourceManager;
            _rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, false, true);
            InitializeComponent();
            MenuItemTournaments.Visibility = Visibility.Collapsed;
            MenuItemEngine.Visibility = Visibility.Collapsed;
            SpeechTranslator.ResourceManager = _rm;
            var logPath = Path.Combine(Configuration.Instance.FolderPath, "log");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            _logging = new FileLogger(Path.Combine(logPath, $"{Constants.BearChessServer}.log"), 10, 10)
            {
                Active = true
            };
            var assemblyName = assembly.GetName();
            var fileInfo = new FileInfo(assembly.Location);
            var productVersion = FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location).ProductVersion;
            _logging.LogInfo(
                $"Start {Constants.BearChessServer} v{assemblyName.Version} {fileInfo.LastWriteTimeUtc:G} {productVersion}");
            _bearChessController = new BearChessController(_logging);
            _bearChessController.ClientConnected += _bearChessController_ClientConnected;
            _bearChessController.ClientDisconnected += _bearChessController_ClientDisconnected;
            _bearChessController.ClientMessage += _bearChessController_ClientMessage;
            _bearChessController.BCServerStarted += _bearChessController_ServerStarted;
            _bearChessController.BCServerStopped += _bearChessController_ServerStopped;
            _bearChessController.WebServerStarted += _bearChessController_WebServerStarted;
            _bearChessController.WebServerStopped += _bearChessController_WebServerStopped;
            _bearChessController.ControllerMessage += _bearChessController_ControllerMessage;
            Top = Configuration.Instance.GetWinDoubleValue("MainWinTopMDI", Configuration.WinScreenInfo.Top,
                SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
            Left = Configuration.Instance.GetWinDoubleValue("MainWinLeftMDI", Configuration.WinScreenInfo.Left,
                SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth);
            Width = Configuration.Instance.GetDoubleValue("MainWinWidthMDI", (800).ToString());
            Height = Configuration.Instance.GetDoubleValue("MainWinHeightMDI", (300).ToString());
            Title = $"{Title} v{assemblyName.Version} - {fileInfo.LastWriteTimeUtc:yyyy-MM-dd  HH:mm:ss} - {productVersion}";
        }

        private void _bearChessController_ControllerMessage(object sender, string e)
        {
            Dispatcher?.Invoke(() =>
            {
                listBoxLog.Items.Add($"{DateTime.Now:G}: {e}");
                listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
            });
        }

        private void _bearChessController_WebServerStopped(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() => { textBlockWebServer.Text = _rm.GetString("Closed"); });
        }

        private void _bearChessController_WebServerStarted(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() => { textBlockWebServer.Text = _rm.GetString("IsRunning"); });
        }

        private void _bearChessController_ServerStopped(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() => { textBlockServer.Text = _rm.GetString("Closed"); });
        }

        private void _bearChessController_ServerStarted(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() => { textBlockServer.Text = _rm.GetString("IsRunning"); });
        }

        private void _bearChessController_ClientMessage(object sender, BearChessServerMessage e)
        {
            if (e.ActionCode.Equals("CONNECT"))
            {
                _logging?.LogDebug($"Main: Connect: {e.Message}: {e.Address} ");
                _tokenList.Add(new BearChessClientInformation() { Address = e.Address, Name = e.Message });
                Dispatcher?.Invoke(() =>
                {
                    listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("Connected")} {e.Message} / {e.Address}");
                    listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                });

                return;
            }

            if (e.ActionCode.Equals("DISCONNECT"))
            {
                _logging?.LogDebug($"Main: Disconnect: {e.Message}: {e.Address} ");
                var clientInfo = _tokenList.FirstOrDefault(t => t.Address.Equals(e.Address));
                if (clientInfo != null)
                {
                    _tokenList.Remove(clientInfo);
                }
                Dispatcher?.Invoke(() =>
                {
                    listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("Disconnected")} {e.Message} / {e.Address}");
                    listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                });

                return;
            }

            if (e.ActionCode.Equals("NEWGAME"))
            {
                Dispatcher?.Invoke(() =>
                {
                    listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("NewGame")}  {e.Address}");
                    listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                });
            }
            if (e.ActionCode.Equals("WHITEPLAYER"))
            {
                Dispatcher?.Invoke(() =>
                {
                    listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("White")}: {e.Message}  {e.Address}");
                    listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                });
            }
            if (e.ActionCode.Equals("BLACKPLAYER"))
            {
                Dispatcher?.Invoke(() =>
                {
                    listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("Black")}: {e.Message}  {e.Address}");
                    listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                });
            }
        }

        private void _bearChessController_ClientDisconnected(object sender, string e)
        {
            var clientInfo = _tokenList.FirstOrDefault(t => t.Address.Equals(e));
            if (clientInfo != null)
            {
                _tokenList.Remove(clientInfo);
            }
        }

        private void _bearChessController_ClientConnected(object sender, string e)
        {

            //  chessboardView.AddRemoteClientToken(e);
        }

        private void TWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                var aWindow = (TournamentWindow)sender;
                if (aWindow == null)
                {
                    return;
                }

                _logging?.LogDebug($"Main: Remove tournament {aWindow.TournamentName}");
                _tournamentWindowsList.Remove(aWindow);
            }
            catch
            {
                //
            }
        }

        private void MenuItemOpenBCServer_Click(object sender, RoutedEventArgs e)
        {
            if (_bearChessController.BCServerIsOpen)
            {
                if (MessageBox.Show(_rm.GetString("StopServer"), _rm.GetString("Stop"), MessageBoxButton.YesNo,
                        MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
                {
                    return;
                }

                listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("ServerStopped")}");
                listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                MenuItemOpenWebServer.IsEnabled = false;
                MenuItemStartPublish.IsEnabled = false;
                if (_bearChessController.WebServerIsOpen)
                {
                    listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("WebServerStopped")}");
                    _bearChessController?.StartStopWebServer();
                }
            }
            else
            {

                listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("ServerStarted")}");
                listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                MenuItemOpenWebServer.IsEnabled = true;
                MenuItemStartPublish.IsEnabled = true;
            }

            _bearChessController?.StartStopBCServer();
        }

        private void MenuItemOpenWebServer_Click(object sender, RoutedEventArgs e)
        {
            if (_bearChessController.WebServerIsOpen)
            {
                if (MessageBox.Show(_rm.GetString("StopWebServer"), _rm.GetString("Stop"), MessageBoxButton.YesNo,
                        MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
                {
                    return;
                }
                listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("WebServerStopped")}");
                listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);

            }
            else
            {
                listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("WebServerStarted")}");
                listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
            }

            _bearChessController?.StartStopWebServer();
        }


        private void MenuItemNewTournament_Click(object sender, RoutedEventArgs e)
        {
            var queryWindows = new QueryTournamentWindow() { Owner = this };
            var result = queryWindows.ShowDialog();
            if (result.HasValue && result.Value)
            {
                _logging?.LogDebug($"Main: New tournament named: {queryWindows.TournamentName}");
                var tWindow = new TournamentWindow(queryWindows.TournamentName, queryWindows.BoardsCount,
                    queryWindows.PublishTournament, _bearChessController, _logging)
                {
                    Owner = this
                };
                tWindow.Show();
                tWindow.Closed += TWindow_Closed;
                tWindow.Tag = Guid.NewGuid();
                _tournamentWindowsList.Add(tWindow);
            }
        }

        private void MenuItemConfigure_Click(object sender, RoutedEventArgs e)
        {
            var configValueLanguage = Configuration.Instance.GetConfigValue("Language", "default");
            var configWindow = new ConfigureServerWindow() { Owner = this };
            var result = configWindow.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Configuration.Instance.Save();
                var configValueLanguageNew = Configuration.Instance.GetConfigValue("Language", "default");
                if (!configValueLanguage.Equals(configValueLanguageNew) )
                {
                    
                        MessageBox.Show(_rm.GetString("RestartBearChessServer"), _rm.GetString("Information"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    
                }
            }
        }

        private void MenuItemSelectEngine_Click(object sender, RoutedEventArgs e)
        {
            var selectInstalledEngineWindow = new SelectInstalledEngineWindow(_bearChessController);
            selectInstalledEngineWindow.ShowDialog();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Configuration.Instance.SetDoubleValue("MainWinLeftMDI", Left);
            Configuration.Instance.SetDoubleValue("MainWinTopMDI", Top);
            Configuration.Instance.SetDoubleValue("MainWinWidthMDI", Width);
            Configuration.Instance.SetDoubleValue("MainWinHeightMDI", Height);
            Configuration.Instance.Save();
        }

        private void MenuItemConfigurePublish_Click(object sender, RoutedEventArgs e)
        {
            var configureWindow = new PublishConfigureWindow(Configuration.Instance) { Owner = this };
            var showDialog = configureWindow.ShowDialog();
            if (showDialog.HasValue && showDialog.Value)
            {
                Configuration.Instance.Save();
            }
        }

        private void MenuItemPublishGame_Click(object sender, RoutedEventArgs e)
        {
            if (!_bearChessController.PublishIsConfigured())
            {
                return;
            }
            if (_bearChessController.PublishingActive)
            {
                if (MessageBox.Show(_rm.GetString("StopPublishing"), _rm.GetString("Stop"), MessageBoxButton.YesNo,
                        MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
                {
                    return;
                }
                listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("PublishingStopped")}");
                listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                textBlockPublish.Text = _rm.GetString("Closed");

            }
            else
            {
                listBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("PublishingStarted")}");
                listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                textBlockPublish.Text = _rm.GetString("IsRunning");
            }

            _bearChessController?.StartStopPublishing();
        }
    }
}
