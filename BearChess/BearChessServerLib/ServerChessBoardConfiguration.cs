using System;

namespace www.SoLaNoSoft.com.BearChessServerLib
{
    [Serializable]
    public class ServerChessBoardConfiguration
    {

        public string ServerBoardId { get; set; } = string.Empty;
        public bool SameBoardForWhiteAndBlack { get; set; } = false;
        public string BearChessClientNameWhite { get; set; } = string.Empty;
        public string BearChessClientNameBlack { get; set; } = string.Empty;
        public string EBoardNameWhite { get; set; } = string.Empty;
        public string EBoardNameBlack { get; set; } = string.Empty;
        public string ComPortWhite { get; set; } = string.Empty;
        public string ComPortBlack { get; set; } = string.Empty;
    }
}
