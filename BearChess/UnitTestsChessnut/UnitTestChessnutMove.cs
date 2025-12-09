using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;

namespace UnitTestsChessnut
{
    [TestClass]
    public class UnitTestChessnutMove
    {

        private readonly byte[] _basePositionsBytes =
       {
            0x42, 0x21,
            0x58, 0x23, 0x31, 0x85, 0x44, 0x44, 0x44, 0x44,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x77,0x77,0x77,0x77,0xA6,0xC9,0x9B,0x6A,
            0x00

        };

        private readonly int[] _fieldOrder =
     {
            Fields.FG8, Fields.FH8, Fields.FE8, Fields.FF8, Fields.FC8, Fields.FD8, Fields.FA8, Fields.FB8,
            Fields.FG7, Fields.FH7, Fields.FE7, Fields.FF7, Fields.FC7, Fields.FD7, Fields.FA7, Fields.FB7,
            Fields.FG6, Fields.FH6, Fields.FE6, Fields.FF6, Fields.FC6, Fields.FD6, Fields.FA6, Fields.FB6,
            Fields.FG5, Fields.FH5, Fields.FE5, Fields.FF5, Fields.FC5, Fields.FD5, Fields.FA5, Fields.FB5,
            Fields.FG4, Fields.FH4, Fields.FE4, Fields.FF4, Fields.FC4, Fields.FD4, Fields.FA4, Fields.FB4,
            Fields.FG3, Fields.FH3, Fields.FE3, Fields.FF3, Fields.FC3, Fields.FD3, Fields.FA3, Fields.FB3,
            Fields.FG2, Fields.FH2, Fields.FE2, Fields.FF2, Fields.FC2, Fields.FD2, Fields.FA2, Fields.FB2,
            Fields.FG1, Fields.FH1, Fields.FE1, Fields.FF1, Fields.FC1, Fields.FD1, Fields.FA1, Fields.FB1
        };

        private readonly byte[] _commandPrefix = { 0x42, 0x21 };

        private readonly Dictionary<string, string> _fenToCode = new Dictionary<string, string>()
        {
            { "", "0" },
            { "q", "1" },
            { "k", "2" },
            { "b", "3" },
            { "p", "4" },
            { "n", "5" },
            { "R", "6" },
            { "P", "7" },
            { "r", "8" },
            { "B", "9" },
            { "N", "A" },
            { "Q", "B" },
            { "K", "C" }
        };

        private byte[] SendFenToBoard(string fenPosition)
        {
            var fastChessBoard = new FastChessBoard();
            fastChessBoard.Init(fenPosition, Array.Empty<string>());
            var allCodes = new List<byte>();
            allCodes.Add(_commandPrefix[0]);
            allCodes.Add(_commandPrefix[1]);
            string byteCode = string.Empty;
            foreach (var i in _fieldOrder)
            {
                var code = _fenToCode[fastChessBoard.GetFigureOnField(i)];
                byteCode += code;

            }

            var codes = StringToByteArray(byteCode);
            allCodes.AddRange(codes);
            allCodes.Add(0);
            return allCodes.ToArray();
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        [TestMethod]
        public void TesBasePositionByteCodes()
        {
            byte[] result =  SendFenToBoard(FenCodes.WhiteBoardBasePosition);
            Assert.AreEqual(result.Length, _basePositionsBytes.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.AreEqual(result[i], _basePositionsBytes[i]);
            }
            
        }
    }

}
