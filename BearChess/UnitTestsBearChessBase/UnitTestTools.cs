using Microsoft.VisualStudio.TestTools.UnitTesting;
using www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessTools;
using www.SoLaNoSoft.com.BearChessWin;

namespace UnitTestsBearChessBase
{
    [TestClass]
    public class UnitTestTools
    {
        [TestMethod]
        public void StringExtensions()
        {
            string manySpaces = "  asa    ödl  s      ww ws a ";
            string noSpaces = "asa ödl s ww ws a";
            Assert.AreEqual(noSpaces, manySpaces.RemoveSpaces());
            
        }

        [TestMethod]
        public void EloSplit()
        {
            int minValue = 1350;
            int maxValue = 2850;
            var splits = EloValueSplitter.GetSplitArray(minValue, maxValue, 20);
            Assert.AreEqual(21, splits.Length);
            Assert.AreEqual(minValue, splits[0]);
            Assert.AreEqual(maxValue, splits[20]);
            Assert.AreEqual(0, EloValueSplitter.GetSplitValue(splits, 1350));
            Assert.AreEqual(10, EloValueSplitter.GetSplitValue(splits, splits[10]));
        }

        [TestMethod]
        public void LichesPuzzleRead()
        {
           var line = LichessPuzzleReader.GetPuzzle();

        }

    }
}