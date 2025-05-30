﻿using System.IO;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChess.SquareOffEBoardWrapper;
using www.SoLaNoSoft.com.BearChessBase.Definitions;

namespace www.SoLaNoSoft.com.BearChess.SquareOffProLoader
{
    public class SquareOffProLoader : AbstractLoader
    {

        public static readonly string EBoardName = Constants.SquareOffPro;

        public SquareOffProLoader(bool check, string name) : base(check, name)
        {

        }

        public SquareOffProLoader() : base(EBoardName)
        {
        }

        public SquareOffProLoader(string folderPath) : base(folderPath, EBoardName)
        {
        }


        public static EChessBoardConfiguration Load(string basePath)
        {
            var fileName = Path.Combine(basePath, Constants.SquareOffPro,
                $"{Constants.SquareOffPro}Cfg.xml");
            var config = EChessBoardConfiguration.Load(fileName);
            config.ShowOwnMoves = false;
            return config;

        }

        public static void Save(string basePath, EChessBoardConfiguration eChessBoardConfiguration)
        {
            var fileName = Path.Combine(basePath, Constants.SquareOffPro,
                $"{Constants.SquareOffPro}Cfg.xml");
            EChessBoardConfiguration.Save(eChessBoardConfiguration, fileName);
        }

        protected override IEBoardWrapper GetEBoardImpl(string basePath, EChessBoardConfiguration configuration)
        {
            if (Check)
            {
                return new SquareOffImpl(Name, basePath);
            }

            var eBoardWrapper = new SquareOffImpl(Name, basePath, configuration);
            eBoardWrapper.SetDebounce(configuration.Debounce);
            eBoardWrapper.SetScanTime(configuration.ScanTime);

            return eBoardWrapper;
        }
        
    }
}
