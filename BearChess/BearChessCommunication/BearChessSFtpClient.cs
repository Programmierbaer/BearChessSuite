using System;
using System.IO;
using Renci.SshNet;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication
{
    public class BearChessSFtpClient : IBearChessFtpClient
    {
        private readonly string _hostname;
        private readonly string _username;
        private readonly string _password;
        private readonly int _port;
        private readonly SftpClient _ftpClient;
        private readonly ILogging _logging;

        public bool IsConnected => _ftpClient != null && _ftpClient.IsConnected;

     

        public string ErrorMessage
        {
            get; private set;
        }

        public BearChessSFtpClient(string hostname, string username, string password, string port, ILogging logging)
        {
            _hostname = hostname;
            _username = username;
            _password = password;
            _logging = logging;
            if (!int.TryParse(port, out _port))
            {
                _port = 22;
            }

            if (_port == 0)
            {
                _port = 22;
            }
            ErrorMessage = string.Empty;
            try
            {
                _logging?.LogDebug($"SFTP: Host; {hostname}  User: {username}");
                _ftpClient = new SftpClient(hostname, _port, username, password);
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _ftpClient = null;
                ErrorMessage = ex.Message;
                _logging?.LogError(ex);
            }
        }

        public bool Connect()
        {
            try
            {
                _logging?.LogDebug("SFTP: Connect");
                _ftpClient?.Connect();
                return _ftpClient?.IsConnected != null;
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                ErrorMessage = ex.Message;
            }
            return false;
        }

        public void Disconnect()
        {
            if (_ftpClient == null || !IsConnected)
            {
                return;
            }

            try
            {
                _logging?.LogDebug("SFTP: Disconnect");
                _ftpClient?.Disconnect();
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                ErrorMessage = ex.Message;
            }
        }

        public bool ChangePath(string pathName)
        {
            if (_ftpClient == null || !IsConnected)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(pathName))
            {
                return true;
            }
            try
            {
                _ftpClient?.ChangeDirectory(pathName);
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                return false;
            }
            return true;
        }
        public bool UploadFile(string localFileName, string remoteFileName)
        {
            if (_ftpClient == null || !IsConnected)
            {
                return false;
            }

            try
            {
                if (File.Exists(localFileName))
                {
                    _logging?.LogDebug($"SFTP: Upload {localFileName} as {remoteFileName}");

                    using (FileStream fs = File.OpenRead(localFileName))
                    {
                        _ftpClient.UploadFile(fs, remoteFileName);
                    }


                    ErrorMessage = "";
                    return _ftpClient.Exists(remoteFileName);
                }
                ErrorMessage = $"File {localFileName} not exists";
                _logging?.LogError($"SFTP: {ErrorMessage}");
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                ErrorMessage = ex.Message;
            }

            return false;
        }
    }
}