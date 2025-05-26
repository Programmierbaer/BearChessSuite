using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessServerLib;
using www.SoLaNoSoft.com.BearChessServerWin.UserControls;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using System.IO;

namespace www.SoLaNoSoft.com.BearChessServerWin.Windows
{
    /// <summary>
    /// Interaktionslogik für TournamentWindow.xaml
    /// </summary>
    public partial class TournamentWindow : Window
    {
        private readonly List<SmallChessboardUserControl> _chessBoardList = new List<SmallChessboardUserControl>();
        private readonly List<BearChessClientInformation> _tokenList = new List<BearChessClientInformation>();
        private readonly IBearChessController _bearChessController;
        private readonly ILogging _logging;

        public TournamentWindow(string name, int gamesCount, IBearChessController bearChessController, ILogging serverLogging )
        {
            InitializeComponent();
            Title  = $"{Title} {name}";
            _logging = serverLogging;
            _bearChessController = bearChessController;
            _bearChessController.ClientMessage += _bearChessController_ClientMessage;
            var uniqueName = Guid.NewGuid().ToString("N");
            var logPath = Path.Combine(Configuration.Instance.FolderPath, "log", $"T_{uniqueName}");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            var logging = new FileLogger(Path.Combine(logPath, "Tournament.log"), 10, 10)
            {
                Active = true
            };
           
            logging?.LogInfo($"Tournament: {name}");

            var colsCount = gamesCount > 3 ? 4 : gamesCount;
            var rowsCount = Math.Ceiling((decimal)((decimal)gamesCount / (decimal)colsCount));
            for (var i = 0; i < colsCount; i++)
            {
                var colDef1 = new ColumnDefinition();
                gamesGrid.ColumnDefinitions.Add(colDef1);
            }
            for (var i = 0; i < rowsCount; i++)
            {
                var rowDef1 = new RowDefinition();
                gamesGrid.RowDefinitions.Add(rowDef1);
            }
            for (var r = 0; r < gamesGrid.RowDefinitions.Count; r++)
            {
                for (var c = 0; c < gamesGrid.ColumnDefinitions.Count; c++)
                {
                    if (gamesCount <= 0)
                    {
                        break;
                    }

                    var boardGrid = new Grid() { Name = $"boardGrid{uniqueName}" };
                    boardGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    boardGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetColumn(boardGrid, c);
                    Grid.SetRow(boardGrid, r);
                    var view1 = new SmallChessboardUserControl()
                    {
                        Margin = new Thickness(5),
                    };
                    view1.SetLogging(logging);
                    view1.ConfigurationRequested += Board_ConfigurationRequested;
                    _chessBoardList.Add(view1);
                    view1.SetBearChessController(_bearChessController);
                    Grid.SetColumn(view1, 0);
                    Grid.SetRow(view1, 0);
                    boardGrid.Children.Add(view1);
                    gamesGrid.Children.Add(boardGrid);
                    gamesCount--;
                }
            }
            // Select new tab item
           
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
                var clientInfo = _tokenList.FirstOrDefault(t => t.Address.Equals(e.Address));
                if (clientInfo != null)
                {
                    _tokenList.Remove(clientInfo);
                }
                return;
            }
        }

        private void Board_ConfigurationRequested(object sender, string boardId)
        {
            var configBoard = _chessBoardList.FirstOrDefault(f => f.BoardId.Equals(boardId));
            if (configBoard == null)
            {
                return;
            }

            var configServerBoard = new ConfigureServerChessboardWindow(boardId, _tokenList.ToArray(), _logging)
            {
                Owner = this
            };
            var dialogResult = configServerBoard.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                _logging?.LogDebug($"Main: Configured e-Board for white: {configServerBoard.WhiteConnectionId} {configServerBoard.WhiteEBoard} ");
                _logging?.LogDebug($"Main: Configured e-Board for black: {configServerBoard.BlackConnectionId} {configServerBoard.BlackEBoard} ");
                configBoard.AddRemoteClientToken(configServerBoard.WhiteConnectionId);
                configBoard.AddRemoteClientToken(configServerBoard.BlackConnectionId);
                _bearChessController.AddWhiteEBoard(configServerBoard.WhiteEBoard);
                _bearChessController.AddBlackEBoard(configServerBoard.BlackEBoard);
            }
        }

        private void TournamentWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _bearChessController.ClientMessage -= _bearChessController_ClientMessage;
        }
    }

   
    }
