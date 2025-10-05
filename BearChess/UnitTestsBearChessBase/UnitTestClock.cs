using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestsBearChessBase
{
    [TestClass]
    public class UnitTestClock
    {

        bool IsBitSet(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }
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
        }

        [TestMethod]
        public void GetClockFromBoard()
        {
            string fromBoard = "00FFCA00043600058000000000000000000000000000000000CCCCCCCCA98BB79A";
            var clock = new ClockFromBoard();
            //for (var i = 6; i < fromBoard.Length; i += 2)
            {
                clock.LeftHours = Convert.ToInt32($"{fromBoard[6]}{fromBoard[7]}", 16) & 63;
                clock.LeftMinutes = Convert.ToInt32($"{fromBoard[8]}{fromBoard[9]}", 16) & 63;
                clock.LeftSeconds = Convert.ToInt32($"{fromBoard[10]}{fromBoard[11]}", 16) & 63;
                clock.RightHours = Convert.ToInt32($"{fromBoard[12]}{fromBoard[13]}", 16) & 63;
                clock.RightMinutes = Convert.ToInt32($"{fromBoard[14]}{fromBoard[15]}", 16) & 63;
                clock.RightSeconds = Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 63;
                int zahl = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 128);
                clock.IsRunning = IsBitSet(Convert.ToByte(zahl), 7);
                zahl = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 64); 
                clock.LeftIsRunning = !IsBitSet(Convert.ToByte(zahl), 6);
                clock.RightIsRunning = IsBitSet(Convert.ToByte(zahl), 6);

            }
            Assert.AreEqual(0,clock.LeftHours);
            Assert.AreEqual(4,clock.LeftMinutes);
            Assert.AreEqual(54,clock.LeftSeconds);
            Assert.AreEqual(0, clock.RightHours);
            Assert.AreEqual(5, clock.RightMinutes);
            Assert.AreEqual(0, clock.RightSeconds);
            Assert.IsTrue(clock.IsRunning);
            Assert.IsTrue(clock.LeftIsRunning);
            Assert.IsFalse(clock.RightIsRunning);

            fromBoard = "00FFCA00041E00041E00000000000000000000000000000000CCCCCCCCA98BB79A";

            //for (var i = 6; i < fromBoard.Length; i += 2)
            {
                clock.LeftHours = Convert.ToInt32($"{fromBoard[6]}{fromBoard[7]}", 16) & 63;
                clock.LeftMinutes = Convert.ToInt32($"{fromBoard[8]}{fromBoard[9]}", 16) & 63;
                clock.LeftSeconds = Convert.ToInt32($"{fromBoard[10]}{fromBoard[11]}", 16) & 63;
                clock.RightHours = Convert.ToInt32($"{fromBoard[12]}{fromBoard[13]}", 16) & 63;
                clock.RightMinutes = Convert.ToInt32($"{fromBoard[14]}{fromBoard[15]}", 16) & 63;
                clock.RightSeconds = Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 63;
                int zahl = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 128);
                clock.IsRunning = IsBitSet(Convert.ToByte(zahl), 7);
                zahl = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 64);
                clock.LeftIsRunning = !IsBitSet(Convert.ToByte(zahl), 6);
                clock.RightIsRunning = IsBitSet(Convert.ToByte(zahl), 6);

            }
            Assert.AreEqual(0, clock.LeftHours);
            Assert.AreEqual(4, clock.LeftMinutes);
            Assert.AreEqual(30, clock.LeftSeconds);
            Assert.AreEqual(0, clock.RightHours);
            Assert.AreEqual(4, clock.RightMinutes);
            Assert.AreEqual(30, clock.RightSeconds);
            Assert.IsFalse(clock.IsRunning);
            Assert.IsTrue(clock.LeftIsRunning);
            Assert.IsFalse(clock.RightIsRunning);

            fromBoard = "00FFCA0004270004AA000000000040060000000000000A0000CCCCC0CC098BB79A";

            //for (var i = 6; i < fromBoard.Length; i += 2)
            {
                clock.LeftHours = Convert.ToInt32($"{fromBoard[6]}{fromBoard[7]}", 16) & 63;
                clock.LeftMinutes = Convert.ToInt32($"{fromBoard[8]}{fromBoard[9]}", 16) & 63;
                clock.LeftSeconds = Convert.ToInt32($"{fromBoard[10]}{fromBoard[11]}", 16) & 63;
                clock.RightHours = Convert.ToInt32($"{fromBoard[12]}{fromBoard[13]}", 16) & 63;
                clock.RightMinutes = Convert.ToInt32($"{fromBoard[14]}{fromBoard[15]}", 16) & 63;
                clock.RightSeconds = Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 63;
                int zahl = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 128);
                clock.IsRunning = IsBitSet(Convert.ToByte(zahl), 7);
                zahl = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 64);
                var zahlString = Convert.ToString(zahl, 2);
                clock.LeftIsRunning = !IsBitSet(Convert.ToByte(zahl), 6);
                clock.RightIsRunning = IsBitSet(Convert.ToByte(zahl), 6);

            }
            Assert.AreEqual(0, clock.LeftHours);
            Assert.AreEqual(4, clock.LeftMinutes);
            Assert.AreEqual(39, clock.LeftSeconds);
            Assert.AreEqual(0, clock.RightHours);
            Assert.AreEqual(4, clock.RightMinutes);
            Assert.AreEqual(42, clock.RightSeconds);
            Assert.IsTrue(clock.IsRunning);
            Assert.IsTrue(clock.LeftIsRunning);
            Assert.IsFalse(clock.RightIsRunning);

            fromBoard = "00FFCA0004230004E60000000000A006000000000000000000CCCCC0CC098BB79A";

            //for (var i = 6; i < fromBoard.Length; i += 2)
            {
                clock.LeftHours = Convert.ToInt32($"{fromBoard[6]}{fromBoard[7]}", 16) & 63;
                clock.LeftMinutes = Convert.ToInt32($"{fromBoard[8]}{fromBoard[9]}", 16) & 63;
                clock.LeftSeconds = Convert.ToInt32($"{fromBoard[10]}{fromBoard[11]}", 16) & 63;
                clock.RightHours = Convert.ToInt32($"{fromBoard[12]}{fromBoard[13]}", 16) & 63;
                clock.RightMinutes = Convert.ToInt32($"{fromBoard[14]}{fromBoard[15]}", 16) & 63;
                clock.RightSeconds = Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 63;
                int zahl = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 128);
                clock.IsRunning = IsBitSet(Convert.ToByte(zahl), 7);
                zahl = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 64);
                var zahlString = Convert.ToString(zahl, 2);
                clock.LeftIsRunning = !IsBitSet(Convert.ToByte(zahl), 6);
                clock.RightIsRunning = IsBitSet(Convert.ToByte(zahl), 6);

            }
            Assert.AreEqual(0, clock.LeftHours);
            Assert.AreEqual(4, clock.LeftMinutes);
            Assert.AreEqual(35, clock.LeftSeconds);
            Assert.AreEqual(0, clock.RightHours);
            Assert.AreEqual(4, clock.RightMinutes);
            Assert.AreEqual(38, clock.RightSeconds);
            Assert.IsTrue(clock.IsRunning);
            Assert.IsFalse(clock.LeftIsRunning);
            Assert.IsTrue(clock.RightIsRunning);
        }
    }
}