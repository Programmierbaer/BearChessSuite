using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChessWin
{
    public class UciInstaller
    {
        private Process _engineProcess;
        private UciInfo _uciInfo;
        
        private ILogging _logger;

        public UciInstaller(ILogging logger)
        {
            _logger = logger;
        }

        public UciInfo Install(string fileName, string parameters, string newIndicator)
        {
            _logger?.LogDebug($"Install new engine {fileName} {parameters}");
            if (!string.IsNullOrWhiteSpace(newIndicator))
            {
                fileName = fileName.Replace(@"\MessChess\MessChess.exe", @"\MessNew\MessNew.exe");
                _logger?.LogDebug($"Change file name to {fileName}");
            }
            
            _uciInfo = new UciInfo(fileName)
            {
                CommandParameter = parameters
            };
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
                    Arguments = parameters
                }

            };
            _logger?.LogDebug($"Process start");
            _engineProcess.Start();
            Thread thread = new Thread(ReadFromEngine) { IsBackground = true };
            thread.Start();
            _uciInfo.Valid = thread.Join(10000);
            try
            {
                _engineProcess.Kill();
                _engineProcess.Dispose();
            }
            catch (Exception ex) 
            {
                _logger?.LogError(ex);
            }

            if (fileName.ToLower().Contains("wasp") || !_uciInfo.CanMultiPV())
            {
                _uciInfo.SupportChangeMultiPV = false;
            }
            return _uciInfo;
        }

        private void ReadFromEngine()
        {
            _logger?.LogDebug($"Start read from engine");
            try
            {
                string waitingFor = "uciok";
                _logger?.LogDebug($"Send uci");
                _engineProcess.StandardInput.Write("uci");
                _engineProcess.StandardInput.Write("\n");
                while (true)
                {
                    var readToEnd = _engineProcess.StandardOutput.ReadLine();
                    _logger?.LogDebug($"Read from engine: {readToEnd}");
                  
                    if (!string.IsNullOrWhiteSpace(readToEnd) && readToEnd.Equals(waitingFor))
                    {
                        break;
                    }
                    if (!string.IsNullOrWhiteSpace(readToEnd))
                    {
                        if (readToEnd.StartsWith("option"))
                        {
                            _uciInfo.AddOption(readToEnd);
                        }

                        if (readToEnd.StartsWith("id name"))
                        {
                            _uciInfo.OriginName = readToEnd.Substring("id name".Length).Trim();
                            _uciInfo.Name = _uciInfo.OriginName;
                        }
                        if (readToEnd.StartsWith("id author"))
                        {
                            _uciInfo.Author = readToEnd.Substring("id author".Length).Trim();
                        }

                    }

                }
                _logger.LogDebug($"Send quit");
                _engineProcess.StandardInput.Write("quit");
                _engineProcess.StandardInput.Write("\n");
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex);
            }
        }
    }
}
