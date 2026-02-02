using Clifton.WebServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Serialization;
using www.SoLaNoSoft.com.BearChess.BearChessCommunication;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChess.Engine;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChessServerLib
{
    public class BearChessController : IBearChessController
    {
        private readonly string _uciPath;
        private readonly string _binPath;
        private readonly Dictionary<string, UciInfo> _installedEngines = new Dictionary<string, UciInfo>();
        private readonly List<BearChessClientInformation> _connectionList = new List<BearChessClientInformation>();

        private IBearChessComServer _bearChessServer;
        private Server _webServer;
        private readonly ILogging _logging;
        private readonly Dictionary<int, Dictionary<string, string>> _awaitedFens = new Dictionary<int, Dictionary<string, string>>();
        private readonly Dictionary<int, List<IElectronicChessBoard>> _allBoards = new Dictionary<int, List<IElectronicChessBoard>>();

        private readonly Dictionary<string, List<string>> _board2Token = new Dictionary<string, List<string>>();
        private GamePublisher _gamePublisher;
        private string _publishPath;

        public event EventHandler<IMove> ChessMoveMade;
        public event EventHandler<CurrentGame> NewGame;
        public event EventHandler BCServerStarted;
        public event EventHandler BCServerStopped;
        public event EventHandler WebServerStarted;
        public event EventHandler WebServerStopped;
        public event EventHandler<string> ClientConnected;
        public event EventHandler<string> ClientDisconnected;
        public event EventHandler<string> ControllerMessage;
        public event EventHandler<BearChessServerMessage> ClientMessage;
        public bool BCServerIsOpen => _bearChessServer != null && _bearChessServer.IsRunning;
        public bool WebServerIsOpen => _webServer != null && _webServer.IsRunning;
        public bool PublishingActive => _gamePublisher != null && _gamePublisher.PublishActive;

        public List<UciInfo> InstalledEngines() => _installedEngines.Values.ToList();
        public UciInfo SelectedEngine { get; set; }
        public string UciPath => _uciPath;
        public BearChessClientInformation[] GetCurrentConnectionList() => _connectionList.ToArray();

        public BearChessController(ILogging logging)
        {
            _logging = logging;
            _awaitedFens[Fields.COLOR_WHITE] = new Dictionary<string, string>();
            _awaitedFens[Fields.COLOR_BLACK] = new Dictionary<string, string>();
            _awaitedFens[Fields.COLOR_EMPTY] = new Dictionary<string, string>();
            _allBoards[Fields.COLOR_WHITE] = new List<IElectronicChessBoard>();
            _allBoards[Fields.COLOR_BLACK] = new List<IElectronicChessBoard>();
            _uciPath = Path.Combine(Configuration.Instance.FolderPath, "uci");
            _binPath = Configuration.Instance.BinPath;
            if (!Directory.Exists(_uciPath))
            {
                Directory.CreateDirectory(_uciPath);
            }
            ReadInstalledEngines();
        }

        public void AssignToken(string boardId, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }
            if (!_board2Token.ContainsKey(boardId))
            {
                _board2Token[boardId] = new List<string>();
            }
            _logging?.LogDebug($"BCC: Add token {token} for board {boardId}");
            _board2Token[boardId].Add(token);
        }

        public bool TokenAssigned(string boardId, string token) {
            if (!_board2Token.ContainsKey(boardId))
            {
                return false;
            }

            return _board2Token[boardId].Contains(token);
        }

        public void SendToClient(string clientAddr, BearChessServerMessage message)
        {
            _bearChessServer.SendToClient(clientAddr, message);
        }


        public void MoveMade(string identification, string fromField, string toField, string awaitedFen, string pgnGame, int color)
        {
            _logging?.LogDebug($"BCC: Move {fromField} {toField} Awaited FEN: {awaitedFen} Color: {color}");
            var eBoard = _allBoards[color].FirstOrDefault(b => b.Identification.Equals(identification));
            if (eBoard != null)
            {
                eBoard.SetAllLEDsOff(true);
                eBoard.SetLEDsFor(new SetLEDsParameter()
                    { FieldNames = new string[] { fromField, toField }, IsMove = true });
                var dict = _awaitedFens[color];
                dict[identification] = awaitedFen.Split(" ".ToCharArray())[0];
            }

            if (_webServer!=null && _webServer.IsRunning)
            {
                _gamePublisher?.PublishFen(identification, awaitedFen);
            }

            if (!string.IsNullOrWhiteSpace(pgnGame))
            {
                _gamePublisher?.Publish(identification, pgnGame);
            }

            SendToClient(identification, new BearChessServerMessage()
            {
                Ack = "ACK",
                Address = identification,
                ActionCode = "MOVE",
                Message = $"{fromField}{toField}".ToLower()
            });
        }

        public bool PublishIsConfigured()
        {
            bool isConfigured = !string.IsNullOrWhiteSpace(Configuration.Instance.GetConfigValue("publishUserName", string.Empty));
            isConfigured = isConfigured && !string.IsNullOrWhiteSpace(Configuration.Instance.GetSecureConfigValue("publishPassword", string.Empty));
            isConfigured = isConfigured && !string.IsNullOrWhiteSpace(Configuration.Instance.GetConfigValue("publishServer", string.Empty));
            return isConfigured;
        }

        public void StartStopBCServer()
        {
            if (_bearChessServer == null)
            {
                int portNumber = Configuration.Instance.GetIntValue("BCServerPortnumber", 8888);
                _bearChessServer = new BearChessComServer(portNumber, _logging);
                _bearChessServer.ClientConnected += _bearChessServer_ClientConnected;
                _bearChessServer.ClientDisconnected += _bearChessServer_ClientDisconnected;
                _bearChessServer.ClientMessage += _bearChessServer_ClientMessage;
                _bearChessServer.ServerStarted += _bearChessServer_ServerStarted;
                _bearChessServer.ServerStopped += _bearChessServer_ServerStopped;
            }
            if (_gamePublisher == null && PublishIsConfigured())
            {
                _publishPath = Path.Combine(Configuration.Instance.FolderPath, "publish");
                try
                {
                    if (!Directory.Exists(_publishPath))
                    {
                        Directory.CreateDirectory(_publishPath);
                    }

                    var path = Configuration.Instance.GetConfigValue("publishPath", ".");
                    var fileName = Configuration.Instance.GetConfigValue("publishFileName", "games.pgn");
                    _gamePublisher = new GamePublisher(FtpClientFactory.GetFtpClient(_logging), _publishPath,
                        GetGamesHtmlFilename(), path, fileName, _logging);
                    _logging?.LogDebug("Publisher created");
                   // ControllerMessage?.Invoke(this,"Publisher created");
                }
                catch (Exception ex)
                {
                    ControllerMessage?.Invoke(this,$"Error on create publisher {ex.Message}");
                    _gamePublisher = null;
                    _logging?.LogError(ex);
                }

            }

            if (_bearChessServer.IsRunning)
            {
                _logging?.LogDebug("BCC: Stop BC-Server");
                _bearChessServer.StopServer();
                _gamePublisher?.StopPublishing();
            }
            else
            {
                _logging?.LogDebug("BCC: Start BC-Server");
                _bearChessServer.RunServer();
            }
        }

        public void StartStopPublishing()
        {
            if (_gamePublisher != null)
            {
                if (_gamePublisher.PublishActive)
                {
                    _gamePublisher?.StopPublishing();
                }
                else
                {
                    var path = Configuration.Instance.GetConfigValue("publishPath", ".");
                    var fileName = Configuration.Instance.GetConfigValue("publishFileName", "games.pgn");
                    _gamePublisher.SetFTP(FtpClientFactory.GetFtpClient(_logging), path, fileName);
                    _gamePublisher?.StartPublishing();
                }
            }
            else
            {
                if ( PublishIsConfigured())
                {
                    _publishPath = Path.Combine(Configuration.Instance.FolderPath, "publish");
                    try
                    {
                        if (!Directory.Exists(_publishPath))
                        {
                            Directory.CreateDirectory(_publishPath);
                        }

                        var path = Configuration.Instance.GetConfigValue("publishPath", ".");
                        var fileName = Configuration.Instance.GetConfigValue("publishFileName", "games.pgn");
                        _gamePublisher = new GamePublisher(FtpClientFactory.GetFtpClient(_logging), _publishPath,
                            GetGamesHtmlFilename(), path, fileName, _logging);
                        _logging?.LogDebug("Publisher created");
                       // ControllerMessage?.Invoke(this, "Publisher created");
                    }
                    catch (Exception ex)
                    {
                        ControllerMessage?.Invoke(this, $"Error on create publisher {ex.Message}");
                        _gamePublisher = null;
                        _logging?.LogError(ex);
                    }
                }
            }
        }

        private void ProvideWebServerFiles()
        {
            try
            {
                var targetPath = Path.Combine(Configuration.Instance.FolderPath, "Website");
                _logging?.LogDebug($"BCC: Copy files to {targetPath}");
                var websitePath = Assembly.GetExecutingAssembly().Location;
                var fileInfo = new FileInfo(websitePath);
                var sourcePath = Path.Combine(fileInfo.DirectoryName, "Website");
                _logging?.LogDebug($"BCC: Copy files from {sourcePath}");
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories).ToList().ForEach(f =>
                {
                    var relativePath = f.Substring(sourcePath.Length + 1);
                    var targetFile = Path.Combine(targetPath, relativePath);
                    var targetDir = new FileInfo(targetFile).DirectoryName;
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    if (!File.Exists(targetFile))
                    {
                        File.Copy(f, targetFile);
                    }
                });
            }
            catch (Exception ex)
            {
                _logging?.LogError("BCC: Cannot provide web server files", ex);
            }
        }

        public void StartStopWebServer()
        {
            if (_webServer == null)
            {
                ProvideWebServerFiles();
                _webServer = new Server
                {
                    OnError = ErrorHandler,
                    OnRequest = (session, context) =>
                    {
                        session.Authenticated = true;
                        session.UpdateLastConnectionTime();
                    }
                };
                _webServer.AddRoute(new Route() { Verb = Router.GET, Path = "/demo/chess", Handler = new AnonymousRouteHandler(_webServer, ChessResponder) });

            }

            if (_webServer.IsRunning)
            {
                _logging?.LogDebug("BCC: Stop Webserver");
                _webServer.Stop();
                WebServerStopped?.Invoke(this, null);
            }
            else
            {
                int portNumber = Configuration.Instance.GetIntValue("WebServerPortnumber", 8080);
                _logging?.LogDebug($"BCC: Start Webserver port {portNumber} ");
                _webServer.Start(GetWebsitePath(), portNumber);
                WebServerStarted?.Invoke(this, null);
            }
        }

        public void Dispose()
        {
            if (_bearChessServer != null)
            {
                if (_bearChessServer.IsRunning)
                {
                    _bearChessServer.StopServer();
                }
            }

            if (_webServer != null)
            {
                if (_webServer.IsRunning)
                {
                    _webServer.Stop();
                }
            }
        }

        public void AddWhiteEBoard(IElectronicChessBoard eBoard)
        {
            if (eBoard == null)
            {
                return;
            }

            _logging?.LogInfo(
                $"BCC: Add e-Board for white: {eBoard.Information} on {eBoard.GetCurrentComPort()} as {eBoard.Identification}");
            _allBoards[Fields.COLOR_WHITE].Add(eBoard);
            var dict = _awaitedFens[Fields.COLOR_WHITE];
            dict[eBoard.Identification] = string.Empty;

            eBoard.FenEvent += WhiteEBoard_FenEvent;
            eBoard.MoveEvent += WhiteEBoard_MoveEvent;
        }

        public void AddBlackEBoard(IElectronicChessBoard eBoard)
        {
            if (eBoard == null)
            {
                return;
            }

            _logging?.LogInfo(
                $"BCC: Add e-Board for black: {eBoard.Information} on {eBoard.GetCurrentComPort()} as {eBoard.Identification}");
            _allBoards[Fields.COLOR_BLACK].Add(eBoard);
            var dict = _awaitedFens[Fields.COLOR_BLACK];
            dict[eBoard.Identification] = string.Empty;
            eBoard.FenEvent += BlackEBoard_FenEvent;
            eBoard.MoveEvent += BlackEBoard_MoveEvent;
        }

        public void RemoveEBoard(IElectronicChessBoard eBoard)
        {
            if (eBoard == null)
            {
                return;
            }

            _logging?.LogDebug(
                $"BCC: Remove e-Board {eBoard.Information} on {eBoard.GetCurrentComPort()} as {eBoard.Identification} ");
            _allBoards[Fields.COLOR_WHITE].Remove(eBoard);
            _allBoards[Fields.COLOR_BLACK].Remove(eBoard);
        }
        
#region private
        private static string ErrorHandler(Server.ServerError error)
        {
            string ret = null;

            switch (error)
            {
                case Server.ServerError.ExpiredSession:
                    ret = "/ErrorPages/expiredSession.html";
                    break;
                case Server.ServerError.FileNotFound:
                    ret = "/ErrorPages/fileNotFound.html";
                    break;
                case Server.ServerError.NotAuthorized:
                    ret = "/ErrorPages/notAuthorized.html";
                    break;
                case Server.ServerError.PageNotFound:
                    ret = "/ErrorPages/pageNotFound.html";
                    break;
                case Server.ServerError.ServerError:
                    ret = "/ErrorPages/serverError.html";
                    break;
                case Server.ServerError.UnknownType:
                    ret = "/ErrorPages/unknownType.html";
                    break;
                case Server.ServerError.ValidationError:
                    ret = "/ErrorPages/validationError.html";
                    break;
            }

            return ret;
        }
        private ResponsePacket ChessResponder(Session session, Dictionary<string, object> parms)
        {
            return _webServer.Redirect("/chess/demo");
        }
        private static string GetWebsitePath()
        {
            return Path.Combine(Configuration.Instance.FolderPath, "Website");
        }

        private  string GetGamesHtmlFilename()
        {
            var websitePath = Configuration.Instance.FolderPath;
            //var fileInfo = new FileInfo(websitePath);
            var path = Path.Combine(websitePath, "Website","Pages","Chess");
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    _logging?.LogError($"BCC: Cannot create {path}", ex);
                }
            }
            return Path.Combine(path,"BearChess.html");
        }

        private void _bearChessServer_ServerStopped(object sender, EventArgs e) => BCServerStopped?.Invoke(this, null);
        private void _bearChessServer_ServerStarted(object sender, EventArgs e) => BCServerStarted?.Invoke(this, null);

        private void _bearChessServer_ClientMessage(object sender, BearChessServerMessage e)
        {
            _logging?.LogDebug($"BCC: received: {e.ActionCode}");
            if (e.ActionCode.Equals("CONNECT"))
            {
                _logging?.LogDebug($"BCC: Connect: {e.Message}: {e.Address} ");
                _connectionList.Add(new BearChessClientInformation() { Address = e.Address, Name = e.Message });
            }
            if (e.ActionCode.Equals("DISCONNECT"))
            {
                _logging?.LogDebug($"BCC: Disconnect: {e.Message}: {e.Address} ");
                var clientInfo = _connectionList.FirstOrDefault(t => t.Address.Equals(e.Address));
                if (clientInfo != null)
                {
                    _connectionList.Remove(clientInfo);
                }
                _gamePublisher?.StopPublishingFor(e.Address);
            }
            if (e.ActionCode.Equals("PUBLISH"))
            {
                _gamePublisher?.Publish(e.Address, e.Message);
                
            }
            if (e.ActionCode.Equals("PUBLISHFEN"))
            {
                _gamePublisher?.PublishFen(e.Address, e.Message);
                
            }
            ClientMessage?.Invoke(this, e);
        }

        private void _bearChessServer_ClientDisconnected(object sender, string e)
        {
            _logging?.LogDebug($"BCC: client disconnected: {e}");
            ClientDisconnected?.Invoke(this, e);
        }

        private void _bearChessServer_ClientConnected(object sender, string e)
        {
            _logging?.LogDebug($"BCC: client connected: {e}");
            ClientConnected?.Invoke(this, e);
        }

        private void WhiteEBoard_FenEvent(object sender, string fen)
        {
            var eBoard = (IElectronicChessBoard)sender;
            _logging?.LogDebug($"BCC: White board: {eBoard.Information} as {eBoard.Identification}  FEN: {fen}");
            var dict = _awaitedFens[Fields.COLOR_WHITE];
            var awaited = dict[eBoard.Identification];
            if (!string.IsNullOrEmpty(awaited) && fen.StartsWith(awaited))
            {
                dict[eBoard.Identification] = string.Empty;
                eBoard.SetAllLEDsOff(true);
            }

            ClientMessage?.Invoke(this,
                new BearChessServerMessage()
                    { Address = eBoard.Identification, ActionCode = "FEN", Message = fen, Color = "w" });
        }

        private void WhiteEBoard_MoveEvent(object sender, string move)
        {
            var eBoard = (IElectronicChessBoard)sender;
            _logging?.LogDebug($"BCC: White board: {eBoard.Information} as {eBoard.Identification}  MOVE: {move}");
            ClientMessage?.Invoke(this,
                new BearChessServerMessage()
                    { Address = eBoard.Identification, ActionCode = "MOVE", Message = move, Color = "w" });
        }

        private void BlackEBoard_FenEvent(object sender, string fen)
        {
            var eBoard = (IElectronicChessBoard)sender;
            _logging?.LogDebug($"BCC: Black board: {eBoard.Information} as {eBoard.Identification}  FEN: {fen}");

            var dict = _awaitedFens[Fields.COLOR_BLACK];
            var awaited = dict[eBoard.Identification];
            if (!string.IsNullOrEmpty(awaited) && fen.StartsWith(awaited))
            {
                dict[eBoard.Identification] = string.Empty;
                eBoard.SetAllLEDsOff(true);
            }

            ClientMessage?.Invoke(this,
                new BearChessServerMessage()
                    { Address = eBoard.Identification, ActionCode = "FEN", Message = fen, Color = "b" });
        }

        private void BlackEBoard_MoveEvent(object sender, string move)
        {
            var eBoard = (IElectronicChessBoard)sender;
            _logging?.LogDebug($"BCC: Black board: {eBoard.Information} as {eBoard.Identification}  MOVE: {move}");
            ClientMessage?.Invoke(this,
                new BearChessServerMessage()
                    { Address = eBoard.Identification, ActionCode = "MOVE", Message = move, Color = "b" });
        }

        private void ReadInstalledEngines()
        {
            try
            {
                BearChessEngine.InstallBearChessEngines(_binPath, _uciPath);
                _logging?.LogInfo($"BCC: Reading installed engines from {_uciPath} ");
                var fileNames = Directory.GetFiles(_uciPath, "*.uci", SearchOption.AllDirectories);
                int invalidEngines = 0;
                foreach (var fileName in fileNames)
                {
                  
                    _logging?.LogInfo($"BCC:  File: {fileName} ");
                    try
                    {
                        var serializer = new XmlSerializer(typeof(UciInfo));
                        TextReader textReader = new StreamReader(fileName);
                        var savedConfig = (UciInfo)serializer.Deserialize(textReader);
                        if (!File.Exists(savedConfig.FileName))
                        {
                            _logging?.LogWarning($"BCC:  Engine file {savedConfig.FileName} not found");
                            invalidEngines++;
                            continue;
                        }

                        if (_installedEngines.ContainsKey(savedConfig.Name))
                        {
                            _logging?.LogWarning($"BCC:  Engine {savedConfig.Name} already installed");
                            invalidEngines++;
                            continue;
                        }

                        _logging?.LogInfo($"BCC:    Engine: {savedConfig.Name} ");
                        _installedEngines.Add(savedConfig.Name, savedConfig);
                      

                    }
                    catch (Exception ex)
                    {
                        _logging?.LogError("BCC: Add installed engine", ex);
                    }
                }


              
                _logging?.LogInfo($"BCC: {_installedEngines.Count} installed engines read");
                if (invalidEngines > 0)
                {
                    _logging?.LogWarning($"BCC: {invalidEngines} engines could not read");
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError("BCC: Read installed engines", ex);
            }
        }
#endregion
    }
}
