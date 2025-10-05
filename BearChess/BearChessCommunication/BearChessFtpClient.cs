using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication
{
    public class BearChessFtpClient : IBearChessFtpClient
    {
        private readonly string _hostname;
        private readonly string _username;
        private readonly string _password;
        private readonly int _port;
        private readonly FtpClient _ftpClient;
        private FtpProfile _autoConnect = null;
        private readonly ILogging _logging;

        public bool IsConnected => _ftpClient!=null && _ftpClient.IsConnected;
        public string ErrorMessage { get; private set; }

        public BearChessFtpClient(string hostname, string username, string password, string port,  ILogging logging)
        {
            _hostname = hostname;
            _username = username;
            _password = password;
            _logging = logging;
            if (!int.TryParse(port, out _port))
            {
                _port = 0;
            }
            ErrorMessage = string.Empty;
            try
            {
                _logging?.LogDebug($"FTP: Host; {hostname}  User: {username}");
                _ftpClient = new FtpClient(hostname, username, password, _port);
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
                _logging?.LogDebug("FTP: Connect");
                _autoConnect = _ftpClient?.AutoConnect();
                ErrorMessage = _autoConnect != null ? string.Empty : "FTP: Connect failed";
                return _autoConnect != null;
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
            if (!IsConnected)
            {
                return;
            }

            try
            {
                _logging?.LogDebug("FTP: Disconnect");
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
            try
            {
                _ftpClient?.SetWorkingDirectory(pathName);
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
            if (!IsConnected)
            {
                return false;
            }

            try
            {
                if (File.Exists(localFileName))
                {
                    _logging?.LogDebug($"FTP: Upload {localFileName} as {remoteFileName}");
                    var uploadFile = _ftpClient?.UploadFile(localFileName, remoteFileName, FtpRemoteExists.Overwrite);
                    ErrorMessage = uploadFile == FtpStatus.Success ? string.Empty : "FTP: Upload failed";
                    return uploadFile == FtpStatus.Success;
                }
                ErrorMessage = $"File {localFileName} not exists";
                _logging?.LogError($"FTP: {ErrorMessage}");
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
