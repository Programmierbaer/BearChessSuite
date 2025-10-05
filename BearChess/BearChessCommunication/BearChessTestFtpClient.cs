using System;
using System.IO;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication
{
    public class BearChessTestFtpClient : IBearChessFtpClient
    {
        public BearChessTestFtpClient(string hostname, string username, string password, ILogging logging)
        {
            
        }

        public bool IsConnected
        {
            get;
            set;
        }

        public bool Connect()
        {
            IsConnected = true;
            return true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public bool UploadFile(string localFileName, string remoteFileName)
        {
            try
            {
                if (File.Exists(localFileName))
                {
                    File.Copy(localFileName, remoteFileName, true);
                    ErrorMessage = string.Empty;
                    return true;
                }

                ErrorMessage = $"File {localFileName} not exists";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            return false;
        }

        public bool ChangePath(string pathName)
        {
            return true;
        }

        public string ErrorMessage
        {
            get;
            private set;
        }
    }
}