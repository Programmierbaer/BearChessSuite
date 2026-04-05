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
        private readonly List<Tournament> _tournaments = new List<Tournament>();

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
            // MenuItemTournaments.Visibility = Visibility.Collapsed;
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
                ListBoxLog.Items.Add($"{DateTime.Now:G}: {e}");
                ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
            });
        }

        private void _bearChessController_WebServerStopped(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() => { TextBlockWebServer.Text = _rm.GetString("Closed"); });
        }

        private void _bearChessController_WebServerStarted(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() => { TextBlockWebServer.Text = _rm.GetString("IsRunning"); });
        }

        private void _bearChessController_ServerStopped(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() => { TextBlockServer.Text = _rm.GetString("Closed"); });
        }

        private void _bearChessController_ServerStarted(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() => { TextBlockServer.Text = _rm.GetString("IsRunning"); });
        }

        private void _bearChessController_ClientMessage(object sender, BearChessServerMessage e)
        {
            if (e.ActionCode.Equals(BCServerConstants.ActionConnect))
            {
                _logging?.LogDebug($"Main: Connect: {e.Message}: {e.Address} ");
                _tokenList.Add(new BearChessClientInformation() { Address = e.Address, Name = e.Message });
                Dispatcher?.Invoke(() =>
                {
                    ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("Connected")} {e.Message} / {e.Address}");
                    ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
                });

                return;
            }

            if (e.ActionCode.Equals(BCServerConstants.ActionDisConnect))
            {
                _logging?.LogDebug($"Main: Disconnect: {e.Message}: {e.Address} ");
                var clientInfo = _tokenList.FirstOrDefault(t => t.Address.Equals(e.Address));
                if (clientInfo != null)
                {
                    _tokenList.Remove(clientInfo);
                }
                Dispatcher?.Invoke(() =>
                {
                    ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("Disconnected")} {e.Message} / {e.Address}");
                    ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
                });

                return;
            }

            if (e.ActionCode.Equals(BCServerConstants.ActionNewGame))
            {
                Dispatcher?.Invoke(() =>
                {
                    ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("NewGame")}  {e.Address}");
                    ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
                });
            }
            if (e.ActionCode.Equals(BCServerConstants.ActionPlayerWhite))
            {
                Dispatcher?.Invoke(() =>
                {
                    ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("White")}: {e.Message}  {e.Address}");
                    ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
                });
            }
            if (e.ActionCode.Equals(BCServerConstants.ActionPlayerBlack))
            {
                Dispatcher?.Invoke(() =>
                {
                    ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("Black")}: {e.Message}  {e.Address}");
                    ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
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

        private void TournamentWindow_Closed(object sender, EventArgs e)
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

                ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("ServerStopped")}");
                ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
                MenuItemOpenWebServer.IsEnabled = false;
                MenuItemStartPublish.IsEnabled = false;
                if (_bearChessController.WebServerIsOpen)
                {
                    ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("WebServerStopped")}");
                    _bearChessController?.StartStopWebServer();
                }
            }
            else
            {

                ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("ServerStarted")}");
                ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
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
                ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("WebServerStopped")}");
                ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);

            }
            else
            {
                ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("WebServerStarted")}");
                ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
            }

            _bearChessController?.StartStopWebServer();
        }


        private void MenuItemNewTournament_Click(object sender, RoutedEventArgs e)
        {
            var queryWindows = new QueryTournamentWindow() { Owner = this };
            var result = queryWindows.ShowDialog();
            if (result.HasValue && result.Value)
            {
                var ident = Guid.NewGuid();
                var tournament = new Tournament(ident,queryWindows.TournamentName, queryWindows.BoardsCount,
                    queryWindows.PublishTournament);
                _tournaments.Add(tournament);
                _logging?.LogDebug($"Main: New tournament named: {queryWindows.TournamentName}");
                var tWindow = new TournamentWindow(tournament, _bearChessController, _logging)
                {
                    Owner = this
                };
                tWindow.Show();
                tWindow.Closed += TournamentWindow_Closed;
                tWindow.Tag = ident;
                tWindow.TournamentChanged += TWindowOnTournamentChanged;
                _tournamentWindowsList.Add(tWindow);
                var tTreeView = new TreeViewItem();
                tTreeView.Header = tWindow.TournamentName;
                tTreeView.Tag = tWindow.Tag;
                TreeViewItemTournaments.Items.Add(tTreeView);
            }
        }

        private void TWindowOnTournamentChanged(object sender, EventArgs e)
        {
            var aWindow = (TournamentWindow)sender;
            if (aWindow == null)
            {
                return;
            }

            var tournament = _tournaments.FirstOrDefault(t => t.Identifier.Equals(aWindow.Tag));
            if (tournament == null)
            {
                return;
            }
            
            foreach (var item in TreeViewItemTournaments.Items)
            {
                var tItem = (TreeViewItem)item; 
                if (tItem is { Tag: not null } && tItem.Tag.Equals(tournament.Identifier))
                {
                    var tournamentGames = tournament.GetGames();
                    tItem.Items.Clear();
                    foreach (var g in tournamentGames)
                    {
                        var tTreeView = new TreeViewItem();
                        tTreeView.Header = $"White: {g.PlayerWhite}";
                        tItem.Items.Add(tTreeView);
                        tTreeView = new TreeViewItem();
                        tTreeView.Header = $"Black: {g.PlayerBlack}";
                        tItem.Items.Add(tTreeView);
                    }
                }
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
                ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("PublishingStopped")}");
                ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
                TextBlockPublish.Text = _rm.GetString("Closed");

            }
            else
            {
                ListBoxLog.Items.Add($"{DateTime.Now:G}: {_rm.GetString("PublishingStarted")}");
                ListBoxLog.ScrollIntoView(ListBoxLog.Items[ListBoxLog.Items.Count - 1]);
                TextBlockPublish.Text = _rm.GetString("IsRunning");
            }

            _bearChessController?.StartStopPublishing();
        }
    }
}
