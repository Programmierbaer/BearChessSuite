using www.SoLaNoSoft.com.BearChessBase.Definitions;

namespace www.SoLaNoSoft.com.BearChess.EChessBoard
{

    public class ClockFromBoard
    {
         public int LeftHours { get; set; } = 0;
         public int LeftMinutes { get; set; } = 0;
         public int LeftSeconds { get; set; } = 0;
         public int RightHours { get; set; } = 0;
         public int RightMinutes { get; set; } = 0;
         public int RightSeconds { get; set; } = 0;
         public bool IsRunning { get; set; } = false;
         public bool LeftIsRunning { get; set; } = false;
         public bool RightIsRunning { get; set; } = false;

         public override string ToString()
         {
             var left = LeftIsRunning ? "*" : " ";
             var right = RightIsRunning ? "*" : " ";
             var active = IsRunning ? " " : "#";

             return $"{left}{LeftHours}:{LeftMinutes}:{LeftSeconds} {active} {right}{RightHours}:{RightMinutes}:{RightSeconds}";
         }
    }

   

 

    public class DataFromBoard
    {
        public string FromBoard { get; set; }
        public ulong Repeated { get; }
        public bool PlayWithWhite { get; }
        public bool BasePosition { get; set; }
        public bool NewGamePosition { get; set; }
        public bool Invalid { get; set; }
        public bool IsFieldDump { get; set; }
        public bool WhiteWinsByKingPosition { get; set; }
        public bool BlackWinsByKingPosition { get; set; }
        public bool DrawByKingPosition { get; set; }
        public ClockFromBoard ClockInformation { get; set; }

        public DataFromBoard(string fromBoard, ulong repeated=0)
        {
            FromBoard = fromBoard;
            Repeated = repeated;
            BasePosition = fromBoard.StartsWith(FenCodes.BlackBoardBasePosition) ||
                           fromBoard.StartsWith(FenCodes.WhiteBoardBasePosition);
            PlayWithWhite = fromBoard.StartsWith(FenCodes.WhiteBoardBasePosition);
            Invalid = false;
            IsFieldDump = false;
            NewGamePosition = false;
            WhiteWinsByKingPosition = false;
            BlackWinsByKingPosition = false;
            DrawByKingPosition = false;
            ClockInformation = null;
        }
        public DataFromBoard(ClockFromBoard clockFromBoard)
        {
            FromBoard = string.Empty;
            Repeated = 3;
            BasePosition = false;
            PlayWithWhite = true;
            Invalid = false;
            IsFieldDump = false;
            NewGamePosition = false;
            WhiteWinsByKingPosition = false;
            BlackWinsByKingPosition = false;
            DrawByKingPosition = false;
            ClockInformation = clockFromBoard;
        }
    }
}