namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication
{
    public interface IBearChessFtpClient
    {
        bool IsConnected { get; }
        bool Connect();
        void Disconnect();
        bool UploadFile(string localFileName, string remoteFileName);
        bool ChangePath(string pathName);
        string ErrorMessage { get; }
    }
}