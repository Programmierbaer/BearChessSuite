using System.Threading;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase.Definitions;

namespace www.SoLaNoSoft.com.BearChess.ChessnutEBoardWrapper
{
    public class ChessnutMoveImpl : AbstractEBoardWrapper
    {

        public ChessnutMoveImpl(string name, string basePath) : base(name, basePath)
        {

        }

        public ChessnutMoveImpl(string name, string basePath, string comPortName, bool useBluetooth) : base(
            name, basePath, "BTLE", true, false, false, false)
        {

        }

        public ChessnutMoveImpl(string name, string basePath, EChessBoardConfiguration configuration) : base(
            name, basePath, configuration)
        {

        }

        public override bool Calibrate()
        {
            _stop = true;
            SetAllLedsOn();
            Thread.Sleep(1000);
            _board.Calibrate();
            SetAllLedsOff(false);
            _stop = false;
            return true;
        }

        public override void SendInformation(string message)
        {
            // throw new System.NotImplementedException();
        }

        public override string RequestInformation(string message)
        {
            return string.Empty;
        }

        public override void DimLEDs(bool dimLEDs)
        {
            //throw new System.NotImplementedException();
        }

        public override void DimLEDs(int level)
        {
            //throw new System.NotImplementedException();
        }

        public override void SetScanTime(int scanTime)
        {
            // ignore
        }

        public override void SetDebounce(int debounce)
        {
            _board?.SetDebounce(debounce);
        }

        public override void FlashMode(EnumFlashMode flashMode)
        {
            //throw new System.NotImplementedException();
        }


        protected override IEBoard GetEBoard()
        {
            return new ChessnutChessBoard.EChessBoardMove(_fileLogger, _configuration);
        }

        protected override IEBoard GetEBoard(bool check)
        {
            return new ChessnutChessBoard.EChessBoardMove(_fileLogger);
        }
    }
}