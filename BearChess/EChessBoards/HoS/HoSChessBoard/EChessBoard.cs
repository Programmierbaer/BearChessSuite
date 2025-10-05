using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.HoSChessBoard
{
    public class EChessBoard : AbstractEBoard
    {

        private class LedArrayInformation
        {
            public byte[] LedArray { get; set; }

            public bool Repeat { get; set; }

        }

        public override event EventHandler BasePositionEvent;
        public override event EventHandler NewGamePositionEvent;
        public override event EventHandler HelpRequestedEvent;
        public override event EventHandler<string> DataEvent;
        public override event EventHandler<string> GameEndEvent;

        private bool _release = false;
        private readonly EChessBoardConfiguration _boardConfiguration;
        private readonly bool _useBluetooth;
        private string _lastData = string.Empty;
        private string _lastResult = string.Empty;
        private string _lastClock = string.Empty;
        private const string _basePositionStringWhite = "00432551346666666600000000000000000000000000000000CCCCCCCCA98BB79A";
        private const string _basePositionStringBlack = "00A97BB89ACCCCCCCC000000000000000000000000000000006666666643155234";

        private static readonly Dictionary<string, byte> _adaptedFieldName = new Dictionary<string, byte>()
        { { "A1", 100 }, { "B1", 101 }, { "C1", 102 }, { "D1", 103 }, { "E1", 104 }, { "F1", 105 }, { "G1", 106 }, { "H1", 107 },
          { "A2", 108 }, { "B2", 109 }, { "C2", 110 }, { "D2", 111 }, { "E2", 112 }, { "F2", 113 }, { "G2", 114 }, { "H2", 115 },
          { "A3", 116 }, { "B3", 117 }, { "C3", 118 }, { "D3", 119 }, { "E3", 120 }, { "F3", 121 }, { "G3", 122 }, { "H3", 123 },
          { "A4", 124 }, { "B4", 125 }, { "C4", 126 }, { "D4", 127 }, { "E4", 128 }, { "F4", 129 }, { "G4", 130 }, { "H4", 131 },
          { "A5", 132 }, { "B5", 133 }, { "C5", 134 }, { "D5", 135 }, { "E5", 136 }, { "F5", 137 }, { "G5", 138 }, { "H5", 139 },
          { "A6", 140 }, { "B6", 141 }, { "C6", 142 }, { "D6", 143 }, { "E6", 144 }, { "F6", 145 }, { "G6", 146 }, { "H6", 147 },
          { "A7", 148 }, { "B7", 149 }, { "C7", 150 }, { "D7", 151 }, { "E7", 152 }, { "F7", 153 }, { "G7", 154 }, { "H7", 155 },
          { "A8", 156 }, { "B8", 157 }, { "C8", 158 }, { "D8", 159 }, { "E8", 160 }, { "F8", 161 }, { "G8", 162 }, { "H8", 163 }, };
        
        private static readonly Dictionary<string, byte> _adaptedFieldNameInvert = new Dictionary<string, byte>()
        { { "A1", 163 }, { "B1", 162 }, { "C1", 161 }, { "D1", 160 }, { "E1", 159 }, { "F1", 158 }, { "G1", 157 }, { "H1", 156 }, { "A2", 155 }, { "B2", 154 }, { "C2", 153 }, { "D2", 152 }, { "E2", 151 }, { "F2", 150 }, { "G2", 149 }, { "H2", 148 }, { "A3", 147 }, { "B3", 146 }, { "C3", 145 }, { "D3", 144 }, { "E3", 143 }, { "F3", 142 }, { "G3", 141 }, { "H3", 140 }, { "A4", 139 }, { "B4", 138 }, { "C4", 137 }, { "D4", 136 }, { "E4", 135 }, { "F4", 134 }, { "G4", 133 }, { "H4", 132 }, { "A5", 131 }, { "B5", 130 }, { "C5", 129 }, { "D5", 128 }, { "E5", 127 }, { "F5", 126 }, { "G5", 125 }, { "H5", 124 }, { "A6", 123 }, { "B6", 122 }, { "C6", 121 }, { "D6", 120 }, { "E6", 119 }, { "F6", 118 }, { "G6", 117 }, { "H6", 116 }, { "A7", 115 }, { "B7", 114 }, { "C7", 113 }, { "D7", 112 }, { "E7", 111 }, { "F7", 110 }, { "G7", 109 }, { "H7", 108 }, { "A8", 107 }, { "B8", 106 }, { "C8", 105 }, { "D8", 104 }, { "E8", 103 }, { "F8", 102 }, { "G8", 101 }, { "H8", 100 }, };

        private readonly ConcurrentQueue<LedArrayInformation> _ledQueue = new ConcurrentQueue<LedArrayInformation>();
        private readonly ConcurrentQueue<byte[]> _clockQueue = new ConcurrentQueue<byte[]>();
        private volatile int _delay = 2500;
        private string _prevFenLine = string.Empty;
        private bool _whiteKingOnBasePosition = false;
        private bool _blackKingOnBasePosition = false;
        private readonly bool _switchClockSide;
        private static readonly object _lock = new object();

        public EChessBoard(string basePath, ILogging logger, EChessBoardConfiguration configuration)
        {
            _boardConfiguration = configuration;
            _useBluetooth = configuration.UseBluetooth;
            _delay = _useBluetooth ? 2500 : 20;
            _logger = logger;
            MultiColorLEDs = true;
            BatteryLevel = "---";
            BatteryStatus = "Full";
            _serialCommunication = new SerialCommunication(logger, configuration.PortName, _useBluetooth);
            Information = Constants.Zmartfun;
            _acceptProbingMoves = false;
            ValidForAnalyse = true;
            PieceRecognition = true;
            _switchClockSide = _boardConfiguration.ClockSwitchSide;
            var handleLedThread = new Thread(LedClockThreadHandle) { IsBackground = true };
            handleLedThread.Start();
            _playWithWhite = true;

        }
        public EChessBoard(ILogging logger)
        {
            _logger = logger;
            BatteryLevel = "---";
            BatteryStatus = "Full";
            Information = Constants.Zmartfun;
        }

        private byte[] GetNextLedArray()
        {
            lock (_lock)
            {
                if (_ledQueue.TryDequeue(out var ledInfo))
                {
                    if (ledInfo.Repeat)
                    {
                        _ledQueue.Enqueue(ledInfo);
                    }

                    return ledInfo.LedArray;
                }
                return Array.Empty<byte>();
            }
        }

        private void SetLedArray(byte[] array, bool repeatLed)
        {
            lock (_lock)
            {
                if (!repeatLed)
                {
                    while (_ledQueue.TryDequeue(out _))
                    {

                    }
                }

                var arrayInfo = new LedArrayInformation
                {
                    Repeat = repeatLed,
                    LedArray = new List<byte>(array).ToArray()
                };
                _ledQueue.Enqueue(arrayInfo);
            }
        }

        private void LedClockThreadHandle()
        {
            while (true)
            {
                var array = GetNextLedArray();

                if (_clockQueue.TryDequeue(out var clockArray))
                {
                    _logger?.LogDebug($"Send to clock: {BitConverter.ToString(clockArray)}");
                    _serialCommunication.Send(clockArray, true);
                    Thread.Sleep(5);
                }

                if (array == null || array.Length == 0)
                {
                    Thread.Sleep(5);
                    continue;
                }

                _serialCommunication.Send(array);
                Thread.Sleep(_delay);
            }
        }


        public override void AdditionalInformation(string information)
        {
            // 
        }

        public override void Calibrate()
        {
            if (!EnsureConnection())
            {
                return;
            }
            
        }

        public override bool CheckComPort(string portName)
        {
            return true;
        }

        public override bool CheckComPort(string portName, string baud)
        {
            return true;
        }

        
        public override DataFromBoard GetDumpPiecesFen()
        {
            return GetPiecesFen();
        }

        private bool IsBitSet(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        private ClockFromBoard GetClockFromBoard(string fromBoard)
        {
           
            var clock = new ClockFromBoard();
            try
            {
                if (_switchClockSide)
                {
                    clock.RightHours = Convert.ToInt32($"{fromBoard[6]}{fromBoard[7]}", 16) & 63;
                    clock.RightMinutes = Convert.ToInt32($"{fromBoard[8]}{fromBoard[9]}", 16) & 63;
                    clock.RightSeconds = Convert.ToInt32($"{fromBoard[10]}{fromBoard[11]}", 16) & 63;
                    clock.LeftHours = Convert.ToInt32($"{fromBoard[12]}{fromBoard[13]}", 16) & 63;
                    clock.LeftMinutes = Convert.ToInt32($"{fromBoard[14]}{fromBoard[15]}", 16) & 63;
                    clock.LeftSeconds = Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 63;
                    int clockStateValue = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 128);
                    clock.IsRunning = IsBitSet(Convert.ToByte(clockStateValue), 7);
                    clockStateValue = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 64);
                    clock.RightIsRunning = !IsBitSet(Convert.ToByte(clockStateValue), 6);
                    clock.LeftIsRunning = IsBitSet(Convert.ToByte(clockStateValue), 6);
                }
                else
                {
                    clock.LeftHours = Convert.ToInt32($"{fromBoard[6]}{fromBoard[7]}", 16) & 63;
                    clock.LeftMinutes = Convert.ToInt32($"{fromBoard[8]}{fromBoard[9]}", 16) & 63;
                    clock.LeftSeconds = Convert.ToInt32($"{fromBoard[10]}{fromBoard[11]}", 16) & 63;
                    clock.RightHours = Convert.ToInt32($"{fromBoard[12]}{fromBoard[13]}", 16) & 63;
                    clock.RightMinutes = Convert.ToInt32($"{fromBoard[14]}{fromBoard[15]}", 16) & 63;
                    clock.RightSeconds = Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 63;
                    int clockStateValue = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 128);
                    clock.IsRunning = IsBitSet(Convert.ToByte(clockStateValue), 7);
                    clockStateValue = (Convert.ToInt32($"{fromBoard[16]}{fromBoard[17]}", 16) & 64);
                    clock.LeftIsRunning = !IsBitSet(Convert.ToByte(clockStateValue), 6);
                    clock.RightIsRunning = IsBitSet(Convert.ToByte(clockStateValue), 6);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Invalid clock: {fromBoard} ", ex);
                clock = new ClockFromBoard();
            }
            
            return clock;
        }

        public override DataFromBoard GetPiecesFen()
        {
            if (!EnsureConnection())
            {
                return new DataFromBoard(string.Empty);
            }

            var dataFromBoard = _serialCommunication.GetFromBoard();

            //if (dataFromBoard.FromBoard.Equals(_lastData) || dataFromBoard.FromBoard.Length < 66)
            //{
            //    return new DataFromBoard(_lastResult, dataFromBoard.Repeated);
            //}
            if (dataFromBoard.FromBoard.StartsWith("00FFCA"))
            {
                if (!_boardConfiguration.UseClock || _boardConfiguration.ClockUseIntern)
                {
                    return new DataFromBoard(string.Empty);
                }
                var clock = GetClockFromBoard(dataFromBoard.FromBoard);
                if (clock.ToString().Equals(_lastClock))
                {
                    return new DataFromBoard(string.Empty);
                }
                _logger?.LogDebug($"Clock: {clock}");
                _lastClock = clock.ToString();
                return new DataFromBoard(clock);
            }
            _lastData = dataFromBoard.FromBoard;
            if (string.IsNullOrWhiteSpace(_lastData))
            {
                return new DataFromBoard(_lastResult, dataFromBoard.Repeated);
            }
            if (_lastData.Equals(_basePositionStringWhite))
            {
                _playWithWhite = true;
            }
            if (_lastData.Equals(_basePositionStringBlack))
            {
                _playWithWhite = false;
            }
            _lastResult = ConvertToFen(_lastData);
            if (!_lastResult.Contains("k") || !_lastResult.Contains("K"))
            {
                return new DataFromBoard(_lastResult, dataFromBoard.Repeated);
            }
            if (FenCodes.WhiteBoardBasePosition.StartsWith(_lastResult) || FenCodes.BlackBoardBasePosition.StartsWith(_lastResult))
            {
                return new DataFromBoard(_lastResult, dataFromBoard.Repeated);
            }
            if (!string.IsNullOrWhiteSpace(_prevFenLine) && !_prevFenLine.Equals(_lastResult) )
            {
                var fastChessBoard = new FastChessBoard();
                fastChessBoard.Init(_lastResult, Array.Empty<string>());
                if (_whiteKingOnBasePosition)
                {
                    if (fastChessBoard.WhiteKingOnCastleMove())
                    {
                       
                        return new DataFromBoard(_prevFenLine.Contains(UnknownPieceCode) ? string.Empty : _prevFenLine, 3);
                        
                    }
                }

                if (_blackKingOnBasePosition)
                {
                    if (fastChessBoard.BlackKingOnCastleMove())
                    {
                        return new DataFromBoard(_prevFenLine.Contains(UnknownPieceCode) ? string.Empty : _prevFenLine,3);
                    }
                }
                _whiteKingOnBasePosition = fastChessBoard.WhiteKingOnBasePosition();
                _blackKingOnBasePosition = fastChessBoard.BlackKingOnBasePosition();
            }
            _prevFenLine = _lastResult;
            return new DataFromBoard(_lastResult, 3);

        }

        public override void Release()
        {
            StopClock();
            _release = true;
        }


        public override void SetAllLEDsOff(bool forceOff)
        {
            _serialCommunication.ClearToBoard();
            if (_useBluetooth)
            {
                SetLedArray(new byte[] { 186 }, false);
            }
            else
            {
                SetLedArray(new byte[] { 0, 186 }, false);
            }
        }



        public override void SetLedForFields(SetLEDsParameter ledsParameter)
        {
            if (ledsParameter.FieldNames.Length == 0 &&
                ledsParameter.HintFieldNames.Length == 0 &&
                ledsParameter.InvalidFieldNames.Length == 0)
            {
                return;
            }

            _serialCommunication.ClearToBoard();
            if (ledsParameter.IsProbing)
            {
                var firstOrDefault = ledsParameter.ProbingMoves.OrderByDescending(p => p.Score).FirstOrDefault();
                if (firstOrDefault != null)
                {
                    if (_playWithWhite ? _adaptedFieldName.TryGetValue(firstOrDefault.FieldName, out var fieldNumber) : _adaptedFieldNameInvert.TryGetValue(firstOrDefault.FieldName, out  fieldNumber))
                    {
                        SetLedArray(_useBluetooth ? new byte[] { fieldNumber } : new byte[] { 0, fieldNumber }, true);
                    }
                }
            }
            else
            {
                if (ledsParameter.FieldNames.Length > 0)
                {
                    foreach (var parameterFieldName in ledsParameter.FieldNames)
                    {
                        if (_playWithWhite ? _adaptedFieldName.ContainsKey(parameterFieldName.ToUpper()) : _adaptedFieldNameInvert.ContainsKey(parameterFieldName.ToUpper()))
                        {
                            var fieldNumber = _playWithWhite ? _adaptedFieldName[parameterFieldName.ToUpper()] : _adaptedFieldNameInvert[parameterFieldName.ToUpper()];
                            SetLedArray(_useBluetooth ? new byte[] { fieldNumber } : new byte[] { 0, fieldNumber }, true);
                        }
                    }
                }
                else
                {
                    foreach (var parameterFieldName in ledsParameter.InvalidFieldNames)
                    {
                        if (_playWithWhite ? _adaptedFieldName.ContainsKey(parameterFieldName.ToUpper()) : _adaptedFieldNameInvert.ContainsKey(parameterFieldName.ToUpper()))
                        {
                            var fieldNumber = _playWithWhite ? _adaptedFieldName[parameterFieldName.ToUpper()] : _adaptedFieldNameInvert[parameterFieldName.ToUpper()];
                            SetLedArray(_useBluetooth ? new byte[] { fieldNumber } : new byte[] { 0, fieldNumber }, true);
                        }
                    }

                    foreach (var parameterFieldName in ledsParameter.HintFieldNames)
                    {
                        if (_playWithWhite ? _adaptedFieldName.ContainsKey(parameterFieldName.ToUpper()) : _adaptedFieldNameInvert.ContainsKey(parameterFieldName.ToUpper()))
                        {
                            var fieldNumber = _playWithWhite ? _adaptedFieldName[parameterFieldName.ToUpper()] : _adaptedFieldNameInvert[parameterFieldName.ToUpper()];
                            SetLedArray(!_useBluetooth ? new byte[] { 0, fieldNumber } : new byte[] { fieldNumber }, true);
                        }
                    }
                }
            }
        }

        protected override void SetToNewGame()
        {
            SetAllLEDsOff(true);
        }
        public override void DimLeds(bool dimLeds)
        {
            //
        }

        public override void DimLeds(int level)
        {
            //
        }

        public override void DisplayOnClock(string display)
        {
            //
        }

        public override void FlashMode(EnumFlashMode flashMode)
        {
            //
        }
        public override void RequestDump()
        {
            //
        }

        public override void Reset()
        {
            //
        }

        public override void SendInformation(string message)
        {
            //
        }

        public override void SetAllLEDsOn()
        {
            //
        }

        public override void SetClock(int hourWhite, int minuteWhite, int secondWhite, int hourBlack, int minuteBlack, int secondBlack, int increments)
        {
            
            if (_boardConfiguration.UseClock && _boardConfiguration.ClockUseIntern)
            {
                if (_switchClockSide)
                {
                    _logger?.LogDebug(
                        $"Set clock: switched  {hourBlack}:{minuteBlack}:{secondBlack}  {hourWhite}:{minuteWhite}:{secondWhite}   {increments}");
                }
                else
                {
                    _logger?.LogDebug(
                        $"Set clock: {hourWhite}:{minuteWhite}:{secondWhite}  {hourBlack}:{minuteBlack}:{secondBlack}  {increments}");
                }

                var byteList = new List<byte>();
                {
                    if (_useBluetooth)
                    {
                        byteList.Add(174);
                        if (_switchClockSide)
                        {
                            byteList.Add((byte)hourBlack);
                            byteList.Add((byte)minuteBlack);
                            byteList.Add((byte)secondBlack);
                            byteList.Add((byte)hourWhite);
                            byteList.Add((byte)minuteWhite);
                            byteList.Add((byte)secondWhite);
                        }
                        else
                        {
                            byteList.Add((byte)hourWhite);
                            byteList.Add((byte)minuteWhite);
                            byteList.Add((byte)secondWhite);
                            byteList.Add((byte)hourBlack);
                            byteList.Add((byte)minuteBlack);
                            byteList.Add((byte)secondBlack);
                        }
                        byteList.Add((byte)increments);
                    }

                    else
                    {
                        byteList.Add(0);
                        byteList.Add(174);
                        if (_switchClockSide)
                        {
                            byteList.Add(0);
                            byteList.Add((byte)hourBlack);
                            byteList.Add(0);
                            byteList.Add((byte)minuteBlack);
                            byteList.Add(0);
                            byteList.Add((byte)secondBlack);
                            byteList.Add(0);
                            byteList.Add((byte)hourWhite);
                            byteList.Add(0);
                            byteList.Add((byte)minuteWhite);
                            byteList.Add(0);
                            byteList.Add((byte)secondWhite);
                        }
                        else
                        {
                            byteList.Add(0);
                            byteList.Add((byte)hourWhite);
                            byteList.Add(0);
                            byteList.Add((byte)minuteWhite);
                            byteList.Add(0);
                            byteList.Add((byte)secondWhite);
                            byteList.Add(0);
                            byteList.Add((byte)hourBlack);
                            byteList.Add(0);
                            byteList.Add((byte)minuteBlack);
                            byteList.Add(0);
                            byteList.Add((byte)secondBlack);

                        }
                        byteList.Add(0);
                        byteList.Add((byte)increments);
                    }
                    _clockQueue.Enqueue(byteList.ToArray());
                }
            }
        }
        public override void ResetClock()
        {
            if (_boardConfiguration.UseClock && _boardConfiguration.ClockUseIntern)
            {
                _logger?.LogDebug("Reset clock");
                var byteList = new List<byte>();
                {
                    if (!_useBluetooth)
                    {
                        byteList.Add(0);
                    }
                    byteList.Add(179);
                    _clockQueue.Enqueue(byteList.ToArray());
                }
            }
        }

        public override void SetCurrentColor(int currentColor)
        {
            //
        }

        public override void SetDebounce(int debounce)
        {
            if (_useBluetooth && debounce >= 1000)
            {
                _delay = debounce;
            }
        }

        public override void SetEngineColor(int color)
        {
            //
        }

        public override void SetFen(string fen)
        {
            //
        }

        public override void SetLedCorner(bool upperLeft, bool upperRight, bool lowerLeft, bool lowerRight)
        {
            //
        }

        public override void SetScanTime(int scanTime)
        {
            //
        }

        public override void SpeedLeds(int level)
        {
            //
        }

        public override void StartClock(bool white)
        {
            if (_boardConfiguration.UseClock && _boardConfiguration.ClockUseIntern)
            {
                _logger?.LogDebug("Start clock");
                byte color = 0;
                if (_switchClockSide)
                {
                    color = white ? (byte)181 : (byte)180;
                }
                else
                {
                    color = white ? (byte)180 : (byte)181;
                }
                var byteList = new List<byte>();
                {
                    if (!_useBluetooth)
                    {
                        byteList.Add(0);
                    }

                    byteList.Add(color);
                    _clockQueue.Enqueue(byteList.ToArray());
                }
            }
        }

        public override void StopClock()
        {
            if (_boardConfiguration.UseClock && _boardConfiguration.ClockUseIntern)
            {
                _logger?.LogDebug("Stop clock");
                var byteList = new List<byte>();
                {
                    if (!_useBluetooth)
                    {
                        byteList.Add(0);

                    }
                    byteList.Add(175);
                    _clockQueue.Enqueue(byteList.ToArray());
                }
            }
        }


        private string ConvertToFen(string fromBoard)
        {
            var chessBoard = new ChessBoard();
            chessBoard.Init();
            try
            {
                if (_playWithWhite)
                {
                    for (var i = 0; i < Fields.BoardFields.Length; i += 2)
                    {
                        // Skip leading 00 and swap position
                        chessBoard.SetFigureOnPosition(ConvertPieceCode(fromBoard[i + 3]), Fields.BoardFields[i]);
                        chessBoard.SetFigureOnPosition(ConvertPieceCode(fromBoard[i + 2]), Fields.BoardFields[i + 1]);
                    }
                }
                else
                {
                    for (var i = 0; i < Fields.BoardFields.Length; i += 2)
                    {
                        // Skip leading 00 and swap position
                        chessBoard.SetFigureOnPosition(ConvertPieceCode(fromBoard[Fields.BoardFields.Length+1-i-1]), Fields.BoardFields[i]);
                        chessBoard.SetFigureOnPosition(ConvertPieceCode(fromBoard[Fields.BoardFields.Length+1-i]), Fields.BoardFields[i + 1]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Invalid position data received",ex);
            }

            return chessBoard.GetFenPosition().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
        }
        private int ConvertPieceCode(char boardCode)
        {
            switch (boardCode)
            {
                case '6': return FigureId.WHITE_PAWN;
                case '3': return FigureId.WHITE_ROOK;
                case '4': return FigureId.WHITE_KNIGHT;
                case '5': return FigureId.WHITE_BISHOP;
                case '2': return FigureId.WHITE_QUEEN;
                case '1': return FigureId.WHITE_KING;
                case 'C': return FigureId.BLACK_PAWN;
                case '9': return FigureId.BLACK_ROOK;
                case 'A': return FigureId.BLACK_KNIGHT;
                case 'B': return FigureId.BLACK_BISHOP;
                case '8': return FigureId.BLACK_QUEEN;
                case '7': return FigureId.BLACK_KING;
                default: return FigureId.NO_PIECE;
            }
        }
    }
}
