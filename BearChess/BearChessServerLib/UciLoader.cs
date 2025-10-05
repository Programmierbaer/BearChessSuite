using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;


namespace www.SoLaNoSoft.com.BearChessServerLib
{
    public sealed class UciLoader
    {
        public class EngineEventArgs : EventArgs
        {
            public string Name { get; }
            public string FromEngine { get; }
            public bool ProbingEngine { get; }

            public int Color { get; }

            public EngineEventArgs(string name, string fromEngine, bool probingEngine, int color)
            {
                Name = name;
                FromEngine = fromEngine;
                ProbingEngine = probingEngine;
                Color = color;
            }
        }

        private readonly Process _engineProcess;
        private readonly UciInfo _uciInfo;
        private readonly ILogging _logger;
        private readonly Configuration _configuration;
        private readonly ConcurrentQueue<string> _waitForFromEngine = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _sendToUciEngine = new ConcurrentQueue<string>();
        private volatile bool _waitFor = false;
        private readonly List<string> _allMoves = new List<string>();
        private volatile bool _quit;
        private readonly object _locker = new object();

        private string _initFen;

        private int _currentColor = Fields.COLOR_EMPTY;

        public bool IsLoaded { get; private set; }

        public event EventHandler<EngineEventArgs> EngineReadingEvent;


        public UciLoader(UciInfo uciInfo, ILogging logger, Configuration configuration)
        {
            IsLoaded = false;
            _uciInfo = uciInfo;
            _logger = logger;
            _configuration = configuration;

            _logger?.LogInfo($"Load engine {uciInfo.Name} with id {uciInfo.Id}");

            var fileName = _uciInfo.FileName;
            if (fileName.ToLower().Contains("wasp") || !_uciInfo.CanMultiPV())
            {
                _uciInfo.SupportChangeMultiPV = false;
            }


            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }

            for (var i = 0; i < 4; i++)
            {
                _engineProcess = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        FileName = fileName,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(fileName),
                        Arguments = _uciInfo.AdjustStrength
                            ? _uciInfo.FileName.Replace(" ", "$")
                            : uciInfo.CommandParameter
                    }
                };
                _engineProcess.Start();
                var thread = new Thread(InitEngine) { IsBackground = true };
                thread.Start();


                if (!thread.Join(60000 + (i * 10000)))
                {
                    try
                    {
                        _engineProcess.Kill();
                        _engineProcess.Dispose();
                        _engineProcess = null;
                    }
                    catch
                    {
                        //
                    }
                }
                else
                {
                    break;
                }
            }

            if (_engineProcess == null)
            {
                return;
            }

            var threadReading = new Thread(ReadFromEngine) { IsBackground = true };
            threadReading.Start();
            var threadSending = new Thread(SendToEngine) { IsBackground = true };
            threadSending.Start();
            IsLoaded = true;
            _initFen = string.Empty;
        }


        public void StopProcess()
        {
            try
            {
                _engineProcess?.Kill();
            }
            catch
            {
                //
            }
        }

        public void NewGame()
        {
            _initFen = string.Empty;
            _allMoves.Clear();
            _allMoves.Add("position startpos moves");
            SendToEngine("ucinewgame");
            IsReady();
        }

        public void SetFen(string fen, string moves)
        {
            _logger?.LogDebug($"Send stop for probing");
            SendToEngine("stop");
            // King missing? => Ignore
            if (!fen.Contains("k") || !fen.Contains("K"))
            {
                return;
            }

            _currentColor = fen.Contains("w") ? Fields.COLOR_WHITE : Fields.COLOR_BLACK;

            _initFen = fen;
            SendToEngine(string.IsNullOrWhiteSpace(moves)
                ? $"position fen {fen}"
                : $"position fen {fen} moves {moves.ToLower()}");
            _allMoves.Clear();
            _allMoves.Add($"position fen {fen} moves");

            _logger?.LogDebug($"Go infinite for probing {fen} {moves}");
            GoInfinite();
        }


        public void GoInfinite()
        {
            SendToEngine("go infinite");
        }

        public void SetMultiPv(int multiPvValue)
        {
            SendToEngine($"setoption name MultiPV value {multiPvValue}");
        }

        public void IsReady()
        {
            SendToEngine("isready");
        }

        public void Stop()
        {
            SendToEngine("stop");
        }

        public void Quit()
        {
            SendToEngine("quit");
        }


        public void SetOption(string name, string value)
        {
            SendToEngine($"setoption name {name} value {value}");
        }

        public void SetOptions()
        {
            foreach (var uciInfoOptionValue in _uciInfo.OptionValues)
            {
                SendToEngine(uciInfoOptionValue);
            }
        }

        public void SendToEngine(string command)
        {
            if (command.Equals("clear"))
            {
                while (!_waitForFromEngine.IsEmpty)
                {
                    _waitForFromEngine.TryDequeue(out var _);
                }

                _waitFor = false;
            }

            _sendToUciEngine.Enqueue(command);
        }

        private void ReadFromEngine()
        {
            var waitingFor = string.Empty;
            try
            {
                while (!_quit)
                {
                    var readToEnd = string.Empty;
                    try
                    {
                        {
                            readToEnd = _engineProcess?.StandardOutput.ReadLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Read ", ex);
                    }

                    if (string.IsNullOrWhiteSpace(readToEnd))
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    if (!_waitForFromEngine.IsEmpty)
                    {
                        _waitForFromEngine.TryDequeue(out waitingFor);
                    }

                    if (!string.IsNullOrWhiteSpace(waitingFor) && !readToEnd.StartsWith(waitingFor))
                    {
                        //_logger?.LogDebug($"<< Ignore: {readToEnd}");
                        continue;
                    }

                    lock (_locker)
                    {
                        _waitFor = false;
                    }

                    waitingFor = string.Empty;
                    _logger?.LogDebug($"<< {readToEnd}");
                    OnEngineReadingEvent(new EngineEventArgs(_uciInfo.Name, readToEnd, _uciInfo.IsProbing,
                        _currentColor));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex);
            }
        }

        private void SendToEngine()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(10);
                    lock (_locker)
                    {
                        if (_waitFor)
                        {
                            continue;
                        }
                    }

                    if (_sendToUciEngine.TryDequeue(out var commandToEngine))
                    {
                        if (commandToEngine.Equals("isready"))
                        {
                            _logger?.LogDebug("wait for ready ok");
                            lock (_locker)
                            {
                                _waitFor = true;
                            }

                            _waitForFromEngine.Enqueue("readyok");
                        }

                        _logger?.LogDebug($">> {commandToEngine}");
                        try
                        {
                            {
                                _engineProcess?.StandardInput.Write(commandToEngine);
                                _engineProcess?.StandardInput.Write("\n");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError("Send ", ex);
                        }

                        if (commandToEngine.Equals("quit", StringComparison.OrdinalIgnoreCase))
                        {
                            _quit = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex);
            }
        }

        private void InitEngine()
        {
            try
            {
                _logger?.LogDebug(">> uci");

                {
                    _engineProcess?.StandardInput.Write("uci");
                    _engineProcess?.StandardInput.Write("\n");
                }

                var waitingFor = "uciok";
                while (true)
                {
                    var readToEnd = _engineProcess?.StandardOutput.ReadLine();
                    _logger?.LogDebug($"<< {readToEnd}");
                    if (!string.IsNullOrWhiteSpace(readToEnd) && readToEnd.Equals(waitingFor))
                    {
                        if (waitingFor.Equals("uciok"))
                        {
                            waitingFor = "readyok";
                            _logger?.LogDebug(">> isready");

                            {
                                _engineProcess?.StandardInput.Write("isready");
                                _engineProcess?.StandardInput.Write("\n");
                            }

                            continue;
                        }

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.Message);
            }
        }

        private void OnEngineReadingEvent(EngineEventArgs e)
        {
            EngineReadingEvent?.Invoke(this, e);
        }
    }
}