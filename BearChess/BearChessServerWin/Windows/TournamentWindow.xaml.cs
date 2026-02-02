using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessDatabase;
using www.SoLaNoSoft.com.BearChessServerLib;
using www.SoLaNoSoft.com.BearChessServerWin.UserControls;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;

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
        private readonly bool _publishTournament;
        private readonly ILogging _serverLogging;
        private readonly ILogging _localLogging;

        public string TournamentName { get; private set; }
        public string UniqueName { get; private set; }

        public TournamentWindow(string name, int gamesCount, bool publishTournament, IBearChessController bearChessController,
            ILogging serverLogging)
        {
            InitializeComponent();
            Title = $"{Title} {name}";
            TournamentName = name;
            _publishTournament = publishTournament;
            _serverLogging = serverLogging;
            _bearChessController = bearChessController;
            _bearChessController.ClientMessage += _bearChessController_ClientMessage;
            UniqueName = $"T_{Guid.NewGuid():N}";
            try
            {
                var logPath = Path.Combine(Configuration.Instance.FolderPath, "log", UniqueName);
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }

                _localLogging = new FileLogger(Path.Combine(logPath, "Tournament.log"), 10, 10)
                {
                    Active = true
                };
            }
            catch
            {
                // ignore
            }

            _localLogging?.LogInfo($"Tournament: {name}");

            var colsCount = gamesCount > 3 ? 4 : gamesCount;
            var rowsCount = Math.Ceiling((decimal)((decimal)gamesCount / (decimal)colsCount));
            for (var i = 0; i < colsCount; i++)
            {
                var colDef1 = new ColumnDefinition();
                GridGames.ColumnDefinitions.Add(colDef1);
            }

            for (var i = 0; i < rowsCount; i++)
            {
                var rowDef1 = new RowDefinition();
                GridGames.RowDefinitions.Add(rowDef1);
            }

            for (var r = 0; r < GridGames.RowDefinitions.Count; r++)
            {
                for (var c = 0; c < GridGames.ColumnDefinitions.Count; c++)
                {
                    if (gamesCount <= 0)
                    {
                        break;
                    }

                    var boardGrid = new Grid() { Name = $"boardGrid{UniqueName}_{gamesCount}" };
                    boardGrid.ColumnDefinitions.Add(new ColumnDefinition()
                        { Width = new GridLength(1, GridUnitType.Star) });
                    boardGrid.ColumnDefinitions.Add(new ColumnDefinition()
                        { Width = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetColumn(boardGrid, c);
                    Grid.SetRow(boardGrid, r);
                    var view1 = new SmallChessboardUserControl()
                    {
                        Margin = new Thickness(5),
                    };
                    view1.SetLogging(_localLogging);
                    view1.ConfigurationRequested += Board_ConfigurationRequested;
                    _chessBoardList.Add(view1);
                    view1.SetBearChessController(_bearChessController);
                    view1.SetPublishGame(_publishTournament);
                    view1.SetTournamentName(TournamentName);
                    Grid.SetColumn(view1, 0);
                    Grid.SetRow(view1, 0);
                    boardGrid.Children.Add(view1);
                    GridGames.Children.Add(boardGrid);
                    gamesCount--;
                }
            }

            _tokenList.AddRange(_bearChessController.GetCurrentConnectionList());
        }

        private void _bearChessController_ClientMessage(object sender, BearChessServerMessage e)
        {
            if (e.ActionCode.Equals("CONNECT"))
            {
                _serverLogging?.LogDebug($"Main: Connect: {e.Message}: {e.Address} ");
                _tokenList.Add(new BearChessClientInformation() { Address = e.Address, Name = e.Message });
                return;
            }

            if (e.ActionCode.Equals("DISCONNECT"))
            {
                _serverLogging?.LogDebug($"Main: Disconnect: {e.Message}: {e.Address} ");
                var clientInfo = _tokenList.FirstOrDefault(t => t.Address.Equals(e.Address));
                if (clientInfo != null)
                {
                    _tokenList.Remove(clientInfo);
                }
            }
        }

        private void Board_ConfigurationRequested(object sender, string boardId)
        {
            var configBoard = _chessBoardList.FirstOrDefault(f => f.BoardId.Equals(boardId));
            if (configBoard == null)
            {
                return;
            }

            var configServerBoard = new ConfigureServerChessboardWindow(boardId, _tokenList.Where(t => t.AssignedBoardId.Equals(boardId)
            || string.IsNullOrWhiteSpace(t.AssignedBoardId)).ToArray(), _serverLogging)
            {
                Owner = this
            };
            var dialogResult = configServerBoard.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                _serverLogging?.LogDebug(
                    $"Main: Configured e-Board for white: {configServerBoard.WhiteConnectionId} {configServerBoard.WhiteEBoard} ");
                _serverLogging?.LogDebug(
                    $"Main: Configured e-Board for black: {configServerBoard.BlackConnectionId} {configServerBoard.BlackEBoard} ");
                foreach (var bearChessClientInformation in _tokenList.Where(bearChessClientInformation => bearChessClientInformation.AssignedBoardId.Equals(boardId)))
                {
                    bearChessClientInformation.AssignedBoardId = string.Empty;
                }

                var token = _tokenList.FirstOrDefault(t => t.Address.Equals(configServerBoard.WhiteConnectionId));
                if (token != null)
                {
                    token.AssignedBoardId = boardId;
                }
                token = _tokenList.FirstOrDefault(t => t.Address.Equals(configServerBoard.BlackConnectionId));
                if (token != null)
                {
                    token.AssignedBoardId = boardId;
                }

                configBoard.AddRemoteClientToken(configServerBoard.WhiteConnectionId);
                configBoard.AddRemoteClientToken(configServerBoard.BlackConnectionId);
                configBoard.WhitePlayerName(configServerBoard.WhitePlayerName);
                configBoard.BlackPlayerName(configServerBoard.BlackPlayerName);
                _bearChessController.AddWhiteEBoard(configServerBoard.WhiteEBoard);
                _bearChessController.AddBlackEBoard(configServerBoard.BlackEBoard);
                _bearChessController.AssignToken(boardId, configServerBoard.WhiteConnectionId);
                _bearChessController.AssignToken(boardId, configServerBoard.BlackConnectionId);
                if (!string.IsNullOrWhiteSpace(configServerBoard.WhiteConnectionId))
                {
                    _bearChessController.SendToClient(configServerBoard.WhiteConnectionId,
                        new BearChessServerMessage()
                        {
                            ActionCode = "TOURNAMENT",
                            Message = TournamentName,
                            Address = configServerBoard.WhiteConnectionId
                        });
                    _bearChessController.SendToClient(configServerBoard.WhiteConnectionId,
                        new BearChessServerMessage()
                        {
                            ActionCode = "PLAYER_WHITE",
                            Message = configServerBoard.WhitePlayerName,
                            Color = configServerBoard.SameConnection ? string.Empty : configServerBoard.WhiteConnectionId,
                            Address = configServerBoard.WhiteConnectionId
                        });
                    _bearChessController.SendToClient(configServerBoard.WhiteConnectionId,
                        new BearChessServerMessage()
                        {
                            ActionCode = "PLAYER_BLACK",
                            Message = configServerBoard.BlackPlayerName,
                            Color = configServerBoard.SameConnection ? string.Empty : configServerBoard.BlackConnectionId,
                            Address = configServerBoard.WhiteConnectionId
                        });
                }

                if (!configServerBoard.SameConnection && !string.IsNullOrWhiteSpace(configServerBoard.BlackConnectionId))
                {
                    _bearChessController.SendToClient(configServerBoard.BlackConnectionId,
                        new BearChessServerMessage()
                        {
                            ActionCode = "TOURNAMENT",
                            Message = TournamentName,
                            Address = configServerBoard.BlackConnectionId
                        });
                    _bearChessController.SendToClient(configServerBoard.BlackConnectionId,
                        new BearChessServerMessage()
                        {
                            ActionCode = "PLAYER_WHITE",
                            Message = configServerBoard.WhitePlayerName,
                            Color = configServerBoard.WhiteConnectionId,
                            Address = configServerBoard.BlackConnectionId
                        });
                    _bearChessController.SendToClient(configServerBoard.BlackConnectionId,
                        new BearChessServerMessage()
                        {
                            ActionCode = "PLAYER_BLACK",
                            Message = configServerBoard.BlackPlayerName,
                            Color = configServerBoard.BlackConnectionId,
                            Address = configServerBoard.BlackConnectionId
                        });
                }
            }
        }

        private void TournamentWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _bearChessController.ClientMessage -= _bearChessController_ClientMessage;
        }

        private void MenuItemSave_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Instance.GetConfigValue("DatabaseFile", string.Empty)))
            {
                var saveFileDialog = new SaveFileDialog() { Filter = "Database|*.db;" };
                var saveDialog = saveFileDialog.ShowDialog(this);
                if (saveDialog.Value && !string.IsNullOrWhiteSpace(saveFileDialog.FileName))
                {
                    Configuration.Instance.SetConfigValue("DatabaseFile", saveFileDialog.FileName);
                }
                else
                {
                    return;
                }
            }

            var database = new Database(this, null, Configuration.Instance.GetConfigValue("DatabaseFile", string.Empty));
            foreach (var smallChessboard in _chessBoardList)
            {
                var playedMoveList = smallChessboard.GetPlayedMoveList();
                if (playedMoveList == null || playedMoveList.Length == 0)
                {
                    continue;
                }

                var pgnCreator = new PgnCreator(string.Empty, Configuration.Instance.GetPgnConfiguration());
                foreach (var move in playedMoveList)
                {
                    pgnCreator.AddMove(move);
                }

                var pgnGame = new PgnGame
                {
                    GameEvent = Title,
                    PlayerWhite = smallChessboard.PlayerWhite,
                    PlayerBlack = smallChessboard.PlayerBlack,
                    Result = "*",
                    GameDate = DateTime.Today.ToString("MM/dd/yyyy"),
                    Round = "1"
                };
                foreach (var move in pgnCreator.GetAllMoves())
                {
                    pgnGame.AddMove(move);
                }

                var currentGame = new CurrentGame(new UciInfo() { IsPlayer = true }, new UciInfo() { IsPlayer = true },
                    Title, new TimeControl() { TimeControlType = TimeControlEnum.NoControl },
                    new TimeControl() { TimeControlType = TimeControlEnum.NoControl }, pgnGame.PlayerWhite,
                    pgnGame.PlayerBlack, true, false, 0, false, false);

                var databaseGame = new DatabaseGame(pgnGame, playedMoveList, currentGame);

                var gameId = database.Save(databaseGame, false);
            }
        }
    }
}
