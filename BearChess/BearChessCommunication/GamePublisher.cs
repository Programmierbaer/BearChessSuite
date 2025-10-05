using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using FluentFTP;
using FluentFTP.Helpers;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication
{
    public class GamePublisher
    {
        private  IBearChessFtpClient _ftpClient;
        private readonly string _localPath;
        private readonly string _webPath;
        private string _remotePath;
        private string _remoteFileName;
        private readonly ILogging _logger = null;
        private static object _lock = new object();
        private volatile bool _stopPublish;
        
        private readonly ConcurrentQueue<string> _games = new ConcurrentQueue<string>();
        public bool PublishActive => !_stopPublish;



        public GamePublisher(IBearChessFtpClient ftpClient, string localPath, string webPath, string remotePath, string remoteFileName, ILogging logger)
        {
            _ftpClient = ftpClient;
            _localPath = localPath;
            _webPath = webPath;
            _remotePath = remotePath;
            _remoteFileName = remoteFileName;
            _logger = logger;
            CleanupFiles();
            _stopPublish = true;
            var threadPublishFtp = new Thread(PublishThread) { IsBackground = true };
            var threadGameJoiner = new Thread(GameJoinerThread) { IsBackground = true };
            var threadFenJoiner = new Thread(FenJoinerThread) { IsBackground = true };
            threadPublishFtp.Start();
            threadGameJoiner.Start();
            threadFenJoiner.Start();
        }

        private void FenJoinerThread()
        {
            string header = @"
<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01//EN"" ""http://www.w3.org/TR/html4/strict.dtd"">
<head>
    <title>BearChess Tournament Viewer</title>
   <meta http-equiv=""refresh"" content=""5"">
    <link rel=""stylesheet"" type=""text/css"" media=""screen"" href=""/Css/chess-fen20.css""/>
    <script type=""text/javascript"" src=""/Scripts/jquery-1.11.2.min.js""></script>
    <script type=""text/javascript"" src=""/Scripts/ChessFen20.js""></script>
    <script type=""text/javascript"" src=""/Scripts/ChessFenAutoload.js""></script>
    <style type=""text/css"">
        /** Demo css */
        body, html {
            margin: 0;
            padding: 0;
            text-align: center;
            font-family: arial;
        }

        .centered-container {
            margin: 0 auto;
            text-align: left;
            width: 1200px;
        }

    </style>
</head>

<body>

<div class=""centered-container"">
    <p/>Games<p/>
            ";
            string footer = @"
    
</div>
</body>
";
            string prevSend = string.Empty;
            string divPrev = "<div class=\"chess-fen-container\" style=\"width:250px;float:left;margin:10px\" data-fen=\"";
            string divPost = "\" data-labels=\"1\"></div>";
            string basePos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            while (true)
            {
                Thread.Sleep(200);
                var sb = new StringBuilder(100);
                lock (_lock)
                {
                    var fileNames = Directory.GetFiles(_localPath, "bc_*.fen");
                    var filesCount = fileNames.Length;
                    foreach (var fileName in fileNames)
                    {
                        var fenLine = File.ReadAllText(fileName);
                        sb.AppendLine($"{divPrev}{fenLine}{divPost}{Environment.NewLine}");
                    }

                    var newSend = sb.ToString();
                    if (string.IsNullOrWhiteSpace(_webPath))
                    {
                        prevSend = newSend;
                        continue;
                    }
                    if (filesCount > 0 && !newSend.Equals(prevSend))
                    {
                        File.WriteAllText(_webPath, $"{header}{newSend}{footer}");
                        prevSend = newSend;
                    }
                    else if (filesCount == 0 && !prevSend.Equals(basePos))
                    {
                        File.WriteAllText(_webPath, $"{header}{divPrev}{basePos}{divPost}{Environment.NewLine}{footer}");
                        prevSend = basePos;
                    }
                }
            }
        }

        private void GameJoinerThread()
        {
            var prevSend = string.Empty;
            while (true)
            {
                Thread.Sleep(100);
                if (_stopPublish)
                {
                    continue;
                }
                var sb = new StringBuilder(100);
                lock (_lock)
                {
                    var fileNames = Directory.GetFiles(_localPath, "bc_*.pgn");
                    var filesCount = fileNames.Length;
                    foreach (var fileName in fileNames)
                    {
                        sb.AppendLine(File.ReadAllText(fileName)+Environment.NewLine);
                    }

                    var newSend = sb.ToString();
                    if (filesCount > 0 && !newSend.Equals(prevSend))
                    {
                        Publish(newSend);
                        prevSend = newSend;
                    }
                }
            }
        }

        private void PublishThread()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (_ftpClient == null)
                {
                    continue;
                }
                try
                {
                    if (_games.TryDequeue(out var game))
                    {
                        if (!_ftpClient.IsConnected)
                        {
                            _ftpClient.Connect();
                            _ftpClient.ChangePath(_remotePath);
                        }

                        if (!_ftpClient.IsConnected)
                        {
                            _logger?.LogError($"PUB: FTP client is not connected ");
                            continue;
                        }

                        var fName = Path.Combine(_localPath, $"game_{Guid.NewGuid():N}");
                        try
                        {
                            File.WriteAllText(fName, game);
                            _logger?.LogDebug($"PUB: Upload {fName} ....");
                            _ftpClient.UploadFile(fName, _remoteFileName);
                            _logger?.LogDebug($"PUB: Uploaded {fName} ");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"PUB: Upload failed", ex);
                        }
                        finally
                        {
                            try
                            {
                                File.Delete(fName);
                            }
                            catch
                            {
                                //
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"PUB: ", ex);
                }
            }
        } 

        public void Publish(string game)
        {
            _games.Enqueue(game);
        }


        public void PublishFen(string address, string fenLine)
        {
            _logger?.LogDebug($"PUB: publish FEN for {address}");
            lock (_lock)
            {
                File.WriteAllText(Path.Combine(_localPath, $"bc_{address}.fen"), fenLine);
            }
        }

        public void Publish(string address, string game)
        {
            _logger?.LogDebug($"PUB: publish game for {address}");
            lock (_lock)
            {
                File.WriteAllText(Path.Combine(_localPath, $"bc_{address}.pgn"), game);
            }
            _logger?.LogDebug($"PUB: published for {address}");
        }

        public void StopPublishingFor(string address)
        {
            return;
            lock (_lock)
            {
                var fileNamePgn = Path.Combine(_localPath, $"bc_{address}.pgn");
                var fileNameFen = Path.Combine(_localPath, $"bc_{address}.fen");
                {
                    try
                    {
                        if (File.Exists(fileNamePgn))
                        {
                            File.Delete(fileNamePgn);
                        }
                    }
                    catch
                    {
                        //
                    }
                    try
                    {
                        if (File.Exists(fileNameFen))
                        {
                            File.Delete(fileNameFen);
                        }
                    }
                    catch
                    {
                        //
                    }
                }
            }
        }

        public void StopPublishing()
        {
            lock (_lock)
            {
                _stopPublish = true;
            }
        }

        public void StartPublishing()
        {
            lock (_lock)
            {
                _stopPublish = false;
            }
        }

        public void SetFTP(IBearChessFtpClient ftpClient, string remotePath, string remoteFileName)
        {
            if (_ftpClient != null)
            {
                try
                {
                    _ftpClient.Disconnect();
                }
                catch
                {
                    //
                }
                finally
                {
                    _ftpClient = null;
                }
            }
            _remotePath = remotePath;
            _remoteFileName = remoteFileName;
            _ftpClient = ftpClient;
        }

        private void CleanupFiles()
        {
            lock (_lock)
            {
                var fileNames = Directory.GetFiles(_localPath, "bc_*.pgn");
                foreach (var file in fileNames)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        //
                    }
                }
                fileNames = Directory.GetFiles(_localPath, "bc_*.fen");
                foreach (var file in fileNames)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        //
                    }
                }
            }
        }
    }
}