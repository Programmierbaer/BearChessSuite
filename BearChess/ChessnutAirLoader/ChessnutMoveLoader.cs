using System.IO;
using www.SoLaNoSoft.com.BearChess.ChessnutEBoardWrapper;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase.Definitions;

namespace www.SoLaNoSoft.com.BearChess.ChessnutAirLoader
{
    public class ChessnutMoveLoader : AbstractLoader
    {

        public static readonly string EBoardName = Constants.ChessnutMove;
        
        public ChessnutMoveLoader() : base(EBoardName)
        {

        }
        public ChessnutMoveLoader(bool check, string name) : base(check, name)
        {
        }

        public ChessnutMoveLoader(string folderPath, string name) : base(folderPath, name)
        {
        }

        public ChessnutMoveLoader(string folderPath) : base(folderPath, EBoardName)
        {
        }

        protected override IEBoardWrapper GetEBoardImpl(string basePath, EChessBoardConfiguration configuration)
        {
            if (Check)
            {
                return new ChessnutMoveImpl(Name, basePath);
            }
            configuration.UseBluetooth = true;
            configuration.PortName = "BTLE";
            var eBoardWrapper = new ChessnutMoveImpl(Name, basePath, configuration);
            eBoardWrapper.SetDebounce(configuration.Debounce);
            return eBoardWrapper;
        }

        public static EChessBoardConfiguration Load(string basePath)
        {
            var fileName = Path.Combine(basePath, Constants.ChessnutMove,
                $"{Constants.ChessnutMove}Cfg.xml");
            var eChessBoardConfiguration = EChessBoardConfiguration.Load(fileName);
            eChessBoardConfiguration.UseBluetooth = true;
            eChessBoardConfiguration.PortName = "BTLE";           
            return eChessBoardConfiguration;

        }

        public static void Save(string basePath, bool useBluetooth, bool showMoveLine, bool showOwnMove)
        {
            var fileName = Path.Combine(basePath, Constants.ChessnutMove,
                $"{Constants.ChessnutMove}Cfg.xml");
            var eChessBoardConfiguration = EChessBoardConfiguration.Load(fileName);
            eChessBoardConfiguration.UseBluetooth = true;
            eChessBoardConfiguration.ShowMoveLine = showMoveLine;
            eChessBoardConfiguration.ShowOwnMoves = showOwnMove;
            eChessBoardConfiguration.PortName = "BTLE";
            EChessBoardConfiguration.Save(eChessBoardConfiguration, fileName);
        }

        public static void Save(string basePath, EChessBoardConfiguration eChessBoardConfiguration)
        {
            var fileName = Path.Combine(basePath, Constants.ChessnutMove,
                $"{Constants.ChessnutMove}Cfg.xml");
            EChessBoardConfiguration.Save(eChessBoardConfiguration, fileName);
        }
    }
}