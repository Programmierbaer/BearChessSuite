using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Resources;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessServerLib;
using www.SoLaNoSoft.com.BearChessServerWin.UserControls;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;
using System.Threading;
using www.SoLaNoSoft.com.BearChessTools;
using www.SoLaNoSoft.com.BearChessServerWin.Windows;
using System.IO;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using System.Collections.Concurrent;
using System.Reflection;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using System.Diagnostics;
using System.Xml.Linq;

namespace www.SoLaNoSoft.com.BearChessServerWin
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

       

        private readonly IBearChessController _bearChessController;
        private readonly List<BearChessClientInformation> _tokenList = new List<BearChessClientInformation>();        
        private readonly List<Window> _tournamentWindowsList = new List<Window>();
        private readonly ResourceManager _rm;
        private readonly ILogging _logging;
     
        public MainWindow()
        {
            InitializeComponent();
            var assembly = Assembly.GetExecutingAssembly();
            Configuration.BearChessProgramAssemblyName = assembly.FullName;
            _rm = Properties.Resources.ResourceManager;
            _rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, false, true);
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
            _logging.LogInfo($"Start {Constants.BearChessServer} v{assemblyName.Version} {fileInfo.LastWriteTimeUtc:G} {productVersion}");
            _bearChessController = new BearChessController(_logging);
            _bearChessController.ClientConnected += _bearChessController_ClientConnected;
            _bearChessController.ClientDisconnected += _bearChessController_ClientDisconnected;
            _bearChessController.ClientMessage += _bearChessController_ClientMessage;
            _bearChessController.ServerStarted += _bearChessController_ServerStarted;
            _bearChessController.ServerStopped += _bearChessController_ServerStopped;
        }
        private void _bearChessController_ServerStopped(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() =>
            {
                textBlockServer.Text = _rm.GetString("Closed");
                MenuItemOpen.Header = _rm.GetString("Start");
            });
        }
        private void _bearChessController_ServerStarted(object sender, EventArgs e)
        {
            Dispatcher?.Invoke(() =>
            {
                textBlockServer.Text = _rm.GetString("IsRunning"); ;
                MenuItemOpen.Header = _rm.GetString("Stop");
            });
        }

        private void _bearChessController_ClientMessage(object sender, BearChessServerMessage e)
        {
            if (e.ActionCode.Equals("CONNECT"))
            {
                _logging?.LogDebug($"Main: Connect: {e.Message}: {e.Address} ");
                _tokenList.Add(new BearChessClientInformation() { Address = e.Address, Name = e.Message });               
                return;
            }
            if (e.ActionCode.Equals("DISCONNECT"))
            {
                _logging?.LogDebug($"Main: Disconnect: {e.Message}: {e.Address} ");
                var clientInfo =_tokenList.FirstOrDefault(t => t.Address.Equals(e.Address));
                if (clientInfo != null)
                {
                    _tokenList.Remove(clientInfo);
                }
                return;
            }
        }

        private void _bearChessController_ClientDisconnected(object sender, string e)
        {
              var clientInfo =_tokenList.FirstOrDefault(t => t.Address.Equals(e));
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
                var aWindow = (Window)sender;
                if (aWindow != null)
                {
                    _tournamentWindowsList.Remove(aWindow);
                }
            }
            catch
            {
                //
            }
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            if (_bearChessController.ServerIsOpen)
            {
                if (MessageBox.Show(_rm.GetString("StopServer"),_rm.GetString("Stop"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            _bearChessController?.StartStopServer();
        }

      

        private void MenItemNewTournament_Click(object sender, RoutedEventArgs e)
        {
            var queryWindows = new QueryTournamentWindow() { Owner = this };
            var result = queryWindows.ShowDialog();
            if (result.HasValue && result.Value)
            {
                var tWindow = new TournamentWindow(queryWindows.TournamentName, queryWindows.BoardsCount, _bearChessController, _logging)
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
            var configWindow = new ConfigureServerWindow() { Owner = this };
            var result = configWindow.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Configuration.Instance.SetIntValue("BCServerPortnumber", configWindow.PortNumber);
                Configuration.Instance.SetConfigValue("BCServerName", configWindow.ServerName);
            }
        }
    }
}
