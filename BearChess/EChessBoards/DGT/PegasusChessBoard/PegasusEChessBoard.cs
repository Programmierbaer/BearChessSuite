﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.PegasusChessBoard
{

    public  class PegasusEChessBoard : AbstractEBoard
    {

        private class PegasusFigure
        {
            public int Figure
            {
                get; set;
            }

            public int Field
            {
                get; set;
            }

            public int FigureColor
            {
                get; set;
            }

            public string FieldName => Fields.GetFieldName(Field);

            public PegasusFigure()
            {
                Figure = FigureId.NO_PIECE;
                Field = Fields.COLOR_EMPTY;
                FigureColor = FigureId.OUTSIDE_PIECE;
            }
        }

        private readonly byte[] _allLEDsOff = { 96, 2, 0, 0 };
        private readonly byte[] _resetBoard = { 64 }; // @
        private readonly byte[] _dumpBoard = { 66 };  // B
        private readonly byte[] _startReading = { 68 }; // D
        private readonly byte[] _serialNumber = { 69 }; // E
        private readonly byte[] _unknown144 = { 70 }; // F
        private readonly byte[] _requestTrademark = { 71 }; // G
        private readonly byte[] _hardwareVersion = { 72 }; // H
        private readonly byte[] _unknown143 = { 73 }; // I
        private readonly byte[] _batteryState = { 76 }; // L

        private readonly byte[] _requestVersion = { 77 }; // M
        private readonly byte[] _serialNumberLong = { 85 }; // U
        private readonly byte[] _unknown163 = { 86 }; // V
        private readonly byte[] _lockState = { 89 }; // Y
        private readonly byte[] _authorized = { 90 }; // Y
        private readonly byte[] _initialize = { 99, 7, 190, 245, 174, 221, 169, 95, 0 };
        private readonly string _boardDump = "134";
        private readonly string _boardFieldUpdate = "142";
        private readonly string _boardSerialNumber = "145";
        private readonly string _boardTrademark = "146";
        private readonly string _boardVersion = "147";
        private readonly string _boardHWVersion = "150";
        private readonly string _boardBattery = "160";


        private readonly Dictionary<string, byte> _fieldName2FieldByte = new Dictionary<string, byte>()
                                                { { "A8",  0 }, { "B8",  1 }, { "C8",  2 }, { "D8",  3 }, { "E8",  4 }, { "F8",  5 }, { "G8",  6 }, { "H8",  7 },
                                                  { "A7",  8 }, { "B7",  9 }, { "C7", 10 }, { "D7", 11 }, { "E7", 12 }, { "F7", 13 }, { "G7", 14 }, { "H7", 15 },
                                                  { "A6", 16 }, { "B6", 17 }, { "C6", 18 }, { "D6", 19 }, { "E6", 20 }, { "F6", 21 }, { "G6", 22 }, { "H6", 23 },
                                                  { "A5", 24 }, { "B5", 25 }, { "C5", 26 }, { "D5", 27 }, { "E5", 28 }, { "F5", 29 }, { "G5", 30 }, { "H5", 31 },
                                                  { "A4", 32 }, { "B4", 33 }, { "C4", 34 }, { "D4", 35 }, { "E4", 36 }, { "F4", 37 }, { "G4", 38 }, { "H4", 39 },
                                                  { "A3", 40 }, { "B3", 41 }, { "C3", 42 }, { "D3", 43 }, { "E3", 44 }, { "F3", 45 }, { "G3", 46 }, { "H3", 47 },
                                                  { "A2", 48 }, { "B2", 49 }, { "C2", 50 }, { "D2", 51 }, { "E2", 52 }, { "F2", 53 }, { "G2", 54 }, { "H2", 55 },
                                                  { "A1", 56 }, { "B1", 57 }, { "C1", 58 }, { "D1", 59 }, { "E1", 60 }, { "F1", 61 }, { "G1", 62 }, { "H1", 63 }
                                                 };

        private readonly Dictionary<string, byte> _invertedFieldName2FieldByte = new Dictionary<string, byte>()
                                                { { "H1",  0 }, { "G1",  1 }, { "F1",  2 }, { "E1",  3 }, { "D1",  4 }, { "C1",  5 }, { "B1",  6 }, { "A1",  7 },
                                                  { "H2",  8 }, { "G2",  9 }, { "F2", 10 }, { "E2", 11 }, { "D2", 12 }, { "C2", 13 }, { "B2", 14 }, { "A2", 15 },
                                                  { "H3", 16 }, { "G3", 17 }, { "F3", 18 }, { "E3", 19 }, { "D3", 20 }, { "C3", 21 }, { "B3", 22 }, { "A3", 23 },
                                                  { "H4", 24 }, { "G4", 25 }, { "F4", 26 }, { "E4", 27 }, { "D4", 28 }, { "C4", 29 }, { "B4", 30 }, { "A4", 31 },
                                                  { "H5", 32 }, { "G5", 33 }, { "F5", 34 }, { "E5", 35 }, { "D5", 36 }, { "C5", 37 }, { "B5", 38 }, { "A5", 39 },
                                                  { "H6", 40 }, { "G6", 41 }, { "F6", 42 }, { "E6", 43 }, { "D6", 44 }, { "C6", 45 }, { "B6", 46 }, { "A6", 47 },
                                                  { "H7", 48 }, { "G7", 49 }, { "F7", 50 }, { "E7", 51 }, { "D7", 52 }, { "C7", 53 }, { "B7", 54 }, { "A7", 55 },
                                                  { "H8", 56 }, { "G8", 57 }, { "F8", 58 }, { "E8", 59 }, { "D8", 60 }, { "C8", 61 }, { "B8", 62 }, { "A8", 63 }
                                                };

        private readonly Dictionary<byte, string> _fieldByte2FieldName = new Dictionary<byte, string>()
                                              { { 0, "A8" }, { 1, "B8" }, { 2, "C8" }, { 3, "D8" }, { 4, "E8" }, { 5, "F8" }, { 6, "G8" }, { 7, "H8" },
                                                { 8, "A7" }, { 9, "B7" }, {10, "C7" }, {11, "D7" }, {12, "E7" }, {13, "F7" }, {14, "G7" }, {15, "H7" },
                                                {16, "A6" }, {17, "B6" }, {18, "C6" }, {19, "D6" }, {20, "E6" }, {21, "F6" }, {22, "G6" }, {23, "H6" },
                                                {24, "A5" }, {25, "B5" }, {26, "C5" }, {27, "D5" }, {28, "E5" }, {29, "F5" }, {30, "G5" }, {31, "H5" },
                                                {32, "A4" }, {33, "B4" }, {34, "C4" }, {35, "D4" }, {36, "E4" }, {37, "F4" }, {38, "G4" }, {39, "H4" },
                                                {40, "A3" }, {41, "B3" }, {42, "C3" }, {43, "D3" }, {44, "E3" }, {45, "F3" }, {46, "G3" }, {47, "H3" },
                                                {48, "A2" }, {49, "B2" }, {50, "C2" }, {51, "D2" }, {52, "E2" }, {53, "F2" }, {54, "G2" }, {55, "H2" },
                                                {56, "A1" }, {57, "B1" }, {58, "C1" }, {59, "D1" }, {60, "E1" }, {61, "F1" }, {62, "G1" }, {63, "H1" }
                                               };

        private readonly Dictionary<byte, string> _invertedFieldByte2FieldName = new Dictionary<byte, string>()
                                                { { 0, "H1" }, { 1, "G1" }, { 2, "F1" }, { 3, "E1" }, { 4, "D1" }, { 5, "C1" }, { 6, "B1" }, { 7, "A1" },
                                                  { 8, "H2" }, { 9, "G2" }, {10, "F2" }, {11, "E2" }, {12, "D2" }, {13, "C2" }, {14, "B2" }, {15, "A2" },
                                                  {16, "H3" }, {17, "G3" }, {18, "F3" }, {19, "E3" }, {20, "D3" }, {21, "C3" }, {22, "B3" }, {23, "A3" },
                                                  {24, "H4" }, {25, "G4" }, {26, "F4" }, {27, "E4" }, {28, "D4" }, {29, "C4" }, {30, "B4" }, {31, "A4" },
                                                  {32, "H5" }, {33, "G5" }, {34, "F5" }, {35, "E5" }, {36, "D5" }, {37, "C5" }, {38, "B5" }, {39, "A5" },
                                                  {40, "H6" }, {41, "G6" }, {42, "F6" }, {43, "E6" }, {44, "D6" }, {45, "C6" }, {46, "B6" }, {47, "A6" },
                                                  {48, "H7" }, {49, "G7" }, {50, "F7" }, {51, "E7" }, {52, "D7" }, {53, "C7" }, {54, "B7" }, {55, "A7" },
                                                  {56, "H8" }, {57, "G8" }, {58, "F8" }, {59, "E8" }, {60, "D8" }, {61, "C8" }, {62, "B8" }, {63, "A8" }
                                                 };
        private const string BASE_POSITION = "1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 ";

        private readonly EChessBoardConfiguration _configuration;
        private readonly IChessBoard _chessBoard;
        private readonly IChessBoard _workingChessBoard;
        private string _prevJoinedString = string.Empty;
        private readonly ConcurrentQueue<DataFromBoard> _fromBoard = new ConcurrentQueue<DataFromBoard>();
        private bool _release = false;
        private PegasusFigure _liftUpFigure = null;
        private PegasusFigure _liftUpEnemyFigure = null;
        private bool _flashLeds;
        private int _lastFromField = Fields.COLOR_OUTSIDE;
        private int _lastToField = Fields.COLOR_OUTSIDE;
        private string _prevRead = string.Empty;
        private string _prevSend = string.Empty;
        private int _equalReadCount = 0;
        private byte _currentSpeed = 2;
        private byte _currentTimes = 0; // always
        private byte _currentIntensity = 2;

        public override event EventHandler BasePositionEvent;
        public override event EventHandler NewGamePositionEvent;
        public override event EventHandler HelpRequestedEvent;
        public override event EventHandler<string> DataEvent;
        public override event EventHandler<string> GameEndEvent;

        private readonly object _lockThinking = new object();
        private readonly List<string> _thinkingLeds = new List<string>();
        private readonly ConcurrentQueue<ProbingMove[]> _probingFields = new ConcurrentQueue<ProbingMove[]>();
        private string _startFenPosition = string.Empty;
        private DataFromBoard _dumpBoardPosition;
        private volatile int _dumpLoopWait;
        private bool _stopLoop;


        public PegasusEChessBoard(string basePath, ILogging logger, EChessBoardConfiguration configuration)
        {
            _configuration = configuration;

            _serialCommunication = new SerialCommunication(logger, _configuration.PortName, true);
            _dumpLoopWait = 250;
            _logger = logger;
            BatteryLevel = "---";
            BatteryStatus = "Full";
            PieceRecognition = false;
            MultiColorLEDs = true;
            IsConnected = EnsureConnection();
            Information = Constants.Pegasus;
            _chessBoard = new ChessBoard();
            _chessBoard.Init();
            _chessBoard.NewGame();
            _workingChessBoard = new ChessBoard();
            _workingChessBoard.Init();
            _workingChessBoard.NewGame();
            _serialCommunication.Send(_initialize);
            _serialCommunication.Send(_resetBoard);
            _serialCommunication.Send(_startReading);
            _serialCommunication.Send(_requestTrademark);
            var handleFromBoardThread = new Thread(HandleFomBoard) { IsBackground = true };
            handleFromBoardThread.Start();
            var handleThinkingLeDsThread = new Thread(HandleThinkingLeds) { IsBackground = true };
            handleThinkingLeDsThread.Start();
            var probingThread = new Thread(ShowProbingMoves) { IsBackground = true };
            probingThread.Start();
            var requestDumpThread = new Thread(RequestADumpLoop) { IsBackground = true };
            requestDumpThread.Start();

        }

        public PegasusEChessBoard(ILogging logger)
        {
            _logger = logger;
            BatteryLevel = "---";
            BatteryStatus = "Full";
            Information = Constants.Pegasus;
            PieceRecognition = false;
        }

        public override void Reset()
        {
            //
        }

        public override void Release()
        {
            _release = true;
        }

        public override void SetFen(string fen)
        {
            lock (_locker)
            {
                _chessBoard.Init();
                _chessBoard.NewGame();
                _chessBoard.SetPosition(fen, true);
                _workingChessBoard.Init();
                _workingChessBoard.NewGame();
                _workingChessBoard.SetPosition(fen, true);
                _startFenPosition = _chessBoard.GetFenPosition();
                _lastFromField = Fields.COLOR_OUTSIDE;
                _lastToField = Fields.COLOR_OUTSIDE;
                _probingFields.TryDequeue(out _);
            }
        }
        public override void SetClock(int hourWhite, int minuteWhite, int secWhite, int hourBlack, int minuteBlack, int secondBlack)
        {
            //
        }

        public override void StopClock()
        {
            //
        }

        public override void StartClock(bool white)
        {
            //
        }

        public override void DisplayOnClock(string display)
        {
            //
        }

        public override void SetCurrentColor(int currentColor)
        {
            //
        }

        public override void SetEngineColor(int color)
        {
            //
        }

        public override bool CheckComPort(string portName)
        {
            bool result = false;
            _serialCommunication = new SerialCommunication(_logger, portName, true);


            result = _serialCommunication.CheckConnect(portName);
            _serialCommunication.DisConnectFromCheck();
            return result;
        }

        public override bool CheckComPort(string portName, string baud)
        {
            return CheckComPort(portName);
        }
        public override void DimLeds(bool dimLeds)
        {
            _currentIntensity = dimLeds ? (byte)4 : (byte)1;
        }

        public override void DimLeds(int level)
        {
            _currentIntensity = (byte)level;
        }

        public override void SetScanTime(int scanTime)
        {
            // ignore
        }

        public override void SetDebounce(int debounce)
        {
            SpeedLeds(debounce);
        }

        public override void SpeedLeds(int level)
        {
            _currentSpeed = (byte)level;
        }
        public override void SetLedForFields(SetLEDsParameter ledsParameter)
        {
            if (!EnsureConnection())
            {
                return;
            }

            lock (_locker)
            {
                if (!ledsParameter.IsThinking)
                {
                    lock (_lockThinking)
                    {
                        if (_thinkingLeds.Count > 0)
                        {
                            _thinkingLeds.Clear();
                        }
                    }
                }
                if (ledsParameter.IsThinking)
                {
                    lock (_lockThinking)
                    {
                        _thinkingLeds.Clear();
                        _thinkingLeds.Add(ledsParameter.FieldNames[0]);
                        _thinkingLeds.Add(ledsParameter.FieldNames[1]);
                        _probingFields.TryDequeue(out _);
                        return;
                    }
                }
                if (ledsParameter.IsProbing && (_configuration.ShowPossibleMoves || _configuration.ShowPossibleMovesEval))
                {
                    _logger?.LogDebug($"B: set LEDs for probing {ledsParameter}");
                    _probingFields.TryDequeue(out _);
                    _probingFields.Enqueue(ledsParameter.ProbingMoves);
                    lock (_lockThinking)
                    {
                        _thinkingLeds.Clear();
                    }

                    return;
                }
                var fieldNames = ledsParameter.FieldNames.Length > 0
                    ? new List<string>(ledsParameter.FieldNames).ToArray()
                    : new List<string>(ledsParameter.InvalidFieldNames).ToArray();
                if (fieldNames.Length == 0)
                {
                    return;
                }
                _probingFields.TryDequeue(out _);
                var joinedString = string.Join(" ", fieldNames);

                if (!ledsParameter.ForceShow && _prevJoinedString.Equals(joinedString))
                {
                    return;
                }
                _logger?.LogDebug($"B: Set LEDs for {joinedString}");
                _prevJoinedString = joinedString;
                var moveLine = ledsParameter.FieldNames.ToArray();
                if (fieldNames.Length == 2 && _configuration.ShowMoveLine)
                {
                     // moveLine = MoveLineHelper.GetMoveLine(fieldNames[0], fieldNames[1]);
                }
                var fieldNamesLength = moveLine.Length;
                var allBytes = new List<byte>();
                var anzahl = byte.Parse((fieldNamesLength + 5).ToString());
                allBytes.Add(96);
                allBytes.Add(anzahl);
                allBytes.Add(5);
                allBytes.Add(ledsParameter.IsThinking ? (byte)0 : _currentSpeed); // Speed
                allBytes.Add(ledsParameter.IsThinking ? (byte)0 : _currentTimes); // Times
                allBytes.Add(ledsParameter.IsThinking ? (byte)1 : _currentIntensity); // Intensity
                foreach (var fieldName in moveLine)
                {
                    if (PlayingWithWhite)
                    {
                        if (_fieldName2FieldByte.ContainsKey(fieldName.ToUpper()))
                        {
                            allBytes.Add(_fieldName2FieldByte[fieldName.ToUpper()]);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (_invertedFieldName2FieldByte.ContainsKey(fieldName.ToUpper()))
                        {
                            allBytes.Add(_invertedFieldName2FieldByte[fieldName.ToUpper()]);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                allBytes.Add(0);
                _serialCommunication.Send(allBytes.ToArray());
                _serialCommunication.Send(_batteryState);
                if (string.IsNullOrWhiteSpace(Information))
                {
                    _serialCommunication.Send(_requestTrademark);
                }
            }
        }
        public override void SetAllLEDsOff(bool forceOff)
        {
            if (!EnsureConnection())
            {
                return;
            }
            _logger?.LogDebug("B: Send all off");
            _probingFields.TryDequeue(out _);
            lock (_lockThinking)
            {
                _thinkingLeds.Clear();
            }

            _serialCommunication.ClearToBoard();
            _serialCommunication.Send(_allLEDsOff, forceOff, "All off");
            if (string.IsNullOrWhiteSpace(Information))
            {
                _serialCommunication.Send(_requestTrademark);
            }
        }
        public override void SetAllLEDsOn()
        {
            if (!EnsureConnection())
            {
                return;
            }
            lock (_lockThinking)
            {
                _thinkingLeds.Clear();
            }

            _probingFields.TryDequeue(out _);
            _logger?.LogDebug("B: Send all on");
            var currentSpeed = _currentSpeed;
            var currentTimes = _currentTimes;
            var currentIntensity = _currentIntensity;
            _currentSpeed = 10;
            _currentTimes = 3;
            _currentIntensity = 1;
            SetLedForFields(new SetLEDsParameter()
            {
                FieldNames = new[] { "A1", "H1", "H8", "A8" },
                Promote = string.Empty,
                IsThinking = false,
                IsMove = false,
                DisplayString = string.Empty
            });
            Thread.Sleep(3000);
            _currentSpeed = currentSpeed;
            _currentTimes = currentTimes;
            _currentIntensity = currentIntensity;
        }
        public override void FlashMode(EnumFlashMode flashMode)
        {
            _flashLeds = flashMode == EnumFlashMode.FlashSync;
        }
        public override void SetLedCorner(bool upperLeft, bool upperRight, bool lowerLeft, bool lowerRight)
        {
            //
        }
        public override void Calibrate()
        {
            IsCalibrated = true;
        }
        public override void SendInformation(string message)
        {
            //
        }

        public override void AdditionalInformation(string information)
        {
            if (information.StartsWith("stop"))
            {
                _stopLoop = true;
                return;
            }
            if (information.StartsWith("go"))
            {
                _stopLoop = false;
                return;
            }

        }

        public override void RequestDump()
        {
            _serialCommunication.Send(_dumpBoard, true);
        }

        public override DataFromBoard GetPiecesFen()
        {
            if (!_fromBoard.TryDequeue(out var dataFromBoard))
            {
                return new DataFromBoard(_prevSend, 3);
            }

            var strings = dataFromBoard.FromBoard.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (strings.Length == 0)
            {
                return new DataFromBoard(_prevSend, 3);
            }

            if (_prevRead.Equals(dataFromBoard.FromBoard))
            {
                _equalReadCount++;
            }
            else
            {
                _logger?.LogDebug($"GetPiecesFen: Changes from {_prevRead} to {dataFromBoard.FromBoard}");
                _prevRead = dataFromBoard.FromBoard;
                _equalReadCount = 0;
            }

            if (_equalReadCount < 5)
            {
                if (string.IsNullOrWhiteSpace(_prevSend))
                {
                    _prevSend = _workingChessBoard.GetFenPosition()
                        .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                }

                _logger?.LogDebug($"GetPiecesFen: _equalReadCount {_equalReadCount} return {_prevSend}");
                return new DataFromBoard(_prevSend, 3);
            }

            if (strings[0] == _boardDump)
            {
                var isBasePosition = dataFromBoard.FromBoard.EndsWith(BASE_POSITION);
                if (isBasePosition)
                {
                    _chessBoard.Init();
                    _chessBoard.NewGame();
                    _workingChessBoard.Init();
                    _workingChessBoard.NewGame();
                    _startFenPosition = string.Empty;
                    BasePositionEvent?.Invoke(this, null);
                    var basePos = _workingChessBoard.GetFenPosition()
                        .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    _prevSend = basePos;
                    return new DataFromBoard(_prevSend, 3)
                    {
                        BasePosition = true,
                        Invalid = false,
                        IsFieldDump = false
                    };
                }

                var boardCodeConverter = new BoardCodeConverter(dataFromBoard.FromBoard, PlayingWithWhite);
                IChessFigure liftUpFigure = null;
                var liftDownField = Fields.COLOR_OUTSIDE;
                foreach (var boardField in Fields.BoardFields)
                {
                    var isFigureOnBoard = boardCodeConverter.IsFigureOn(boardField);
                    var chessFigure = _workingChessBoard.GetFigureOn(boardField);
                    if (isFigureOnBoard && chessFigure.Color == Fields.COLOR_EMPTY)
                    {

                        _logger?.LogDebug(
                            $"GetPiecesFen: Downfield: {boardField}/{Fields.GetFieldName(boardField)}");
                        liftDownField = boardField;
                    }

                    if (!isFigureOnBoard && chessFigure.Color != Fields.COLOR_EMPTY)
                    {
                        _logger?.LogDebug(
                            $"GetPiecesFen: Lift up field: {boardField}/{Fields.GetFieldName(boardField)} {FigureId.FigureIdToEnName[chessFigure.FigureId]}");

                        liftUpFigure = chessFigure;

                    }
                }

                // Nothing changed?
                if (liftDownField == Fields.COLOR_OUTSIDE && liftUpFigure == null)
                {
                    return new DataFromBoard(_prevSend, 3);
                }

                var codeConverter = new BoardCodeConverter();
                var chessFigures = _chessBoard.GetFigures(Fields.COLOR_WHITE);
                foreach (var chessFigure in chessFigures)
                {
                    codeConverter.SetFigureOn(chessFigure.Field);
                }

                chessFigures = _chessBoard.GetFigures(Fields.COLOR_BLACK);
                foreach (var chessFigure in chessFigures)
                {
                    codeConverter.SetFigureOn(chessFigure.Field);
                }

                if (boardCodeConverter.SamePosition(codeConverter))
                {
                    _workingChessBoard.Init(_chessBoard);
                    _prevSend = _chessBoard.GetFenPosition()
                        .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    _logger?.LogDebug($"GetPiecesFen: back to current position: {_prevSend}");
                    _liftUpFigure = null;
                    _liftUpEnemyFigure = null;
                    _lastFromField = Fields.COLOR_OUTSIDE;
                    _lastToField = Fields.COLOR_OUTSIDE;
                    return new DataFromBoard(_prevSend, 3);
                }

                if (!_inDemoMode && _liftUpEnemyFigure != null)
                {
                    if (liftDownField == _liftUpEnemyFigure.Field &&
                        (_liftUpFigure == null && liftUpFigure == null))
                    {
                        _logger?.LogDebug(
                            $"GetPiecesFen: Equal lift up/down enemy field: {_liftUpEnemyFigure.Field} == {liftDownField}");
                        _workingChessBoard.Init(_chessBoard);
                        _liftUpFigure = null;
                        _liftUpEnemyFigure = null;
                        _prevSend = _chessBoard.GetFenPosition()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                        _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");

                        return new DataFromBoard(_prevSend, 3);

                    }
                }

                if (_liftUpEnemyFigure != null &&
                    (!_inDemoMode && _allowTakeBack && liftDownField.Equals(_lastFromField) &&
                     _liftUpEnemyFigure.Field.Equals(_lastToField)))
                {
                    _logger?.LogInfo("TC: Take move back. Replay all previous moves");
                    var playedMoveList = _chessBoard.GetPlayedMoveList();
                    _chessBoard.Init();
                    _chessBoard.NewGame();
                    if (!string.IsNullOrWhiteSpace(_startFenPosition))
                    {
                        _chessBoard.SetPosition(_startFenPosition, false);
                    }

                    for (var i = 0; i < playedMoveList.Length - 1; i++)
                    {
                        _logger?.LogDebug($"TC: Move {playedMoveList[i]}");
                        _chessBoard.MakeMove(playedMoveList[i]);
                        _lastFromField = playedMoveList[i].FromField;
                        _lastToField = playedMoveList[i].ToField;
                    }

                    _workingChessBoard.Init(_chessBoard);
                    _prevSend = _chessBoard.GetFenPosition()
                        .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");
                    _liftUpFigure = null;
                    _liftUpEnemyFigure = null;
                    return new DataFromBoard(_prevSend, 3);
                }

                if (_liftUpFigure != null)
                {
                    // Put the same figure back
                    if (liftDownField == _liftUpFigure.Field)
                    {
                        _logger?.LogDebug(
                            $"GetPiecesFen: Equal lift up/down field: {_liftUpFigure.Field} == {liftDownField}");
                        _workingChessBoard.Init(_chessBoard);
                        _liftUpFigure = null;
                        _liftUpEnemyFigure = null;
                        _prevSend = _chessBoard.GetFenPosition()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                        _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");

                        return new DataFromBoard(_prevSend, 3);
                    }

                    if (liftDownField != Fields.COLOR_OUTSIDE)
                    {
                        if (!_inDemoMode)
                        {
                            if (!_inDemoMode && _allowTakeBack && liftDownField.Equals(_lastFromField) &&
                                _liftUpFigure.Field.Equals(_lastToField))
                            {

                                _logger?.LogInfo("TC: Take move back. Replay all previous moves");
                                var playedMoveList = _chessBoard.GetPlayedMoveList();
                                _chessBoard.Init();
                                _chessBoard.NewGame();
                                if (!string.IsNullOrWhiteSpace(_startFenPosition))
                                {
                                    _chessBoard.SetPosition(_startFenPosition, false);
                                }

                                for (var i = 0; i < playedMoveList.Length - 1; i++)
                                {
                                    _logger?.LogDebug($"TC: Move {playedMoveList[i]}");
                                    _chessBoard.MakeMove(playedMoveList[i]);
                                    _lastFromField = playedMoveList[i].FromField;
                                    _lastToField = playedMoveList[i].ToField;
                                }

                                _workingChessBoard.Init(_chessBoard);
                                _prevSend = _chessBoard.GetFenPosition()
                                    .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                                _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");
                                _liftUpFigure = null;
                                _liftUpEnemyFigure = null;

                                return new DataFromBoard(_prevSend, 3);
                            }

                            if (_chessBoard.MoveIsValid(_liftUpFigure.Field, liftDownField))
                            {
                                if (_awaitingMoveFromField != Fields.COLOR_OUTSIDE)
                                {
                                    if (_awaitingMoveFromField != _liftUpFigure.Field ||
                                        _awaitingMoveToField != liftDownField)
                                    {
                                        _logger?.LogDebug($"GetPiecesFen: move not awaited!");
                                        _logger?.LogDebug(
                                            $"GetPiecesFen: Awaited: {Fields.GetFieldName(_awaitingMoveFromField)} {Fields.GetFieldName(_awaitingMoveToField)}");
                                        _logger?.LogDebug(
                                            $"GetPiecesFen: Read: {Fields.GetFieldName(_liftUpFigure.Field)} {Fields.GetFieldName(liftDownField)}");
                                        return new DataFromBoard(_prevSend, 3);
                                    }
                                }

                                _logger?.LogDebug(
                                    $"GetPiecesFen: Make move: {Fields.GetFieldName(_liftUpFigure.Field)} {Fields.GetFieldName(liftDownField)}");
                                _lastFromField = _liftUpFigure.Field;
                                _lastToField = liftDownField;

                                _chessBoard.MakeMove(_liftUpFigure.Field, liftDownField);
                                _awaitingMoveFromField = Fields.COLOR_OUTSIDE;
                                _awaitingMoveToField = Fields.COLOR_OUTSIDE;

                                _workingChessBoard.Init(_chessBoard);
                                _liftUpFigure = null;
                                _liftUpEnemyFigure = null;
                                _prevSend = _chessBoard.GetFenPosition()
                                    .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                                _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");

                                return new DataFromBoard(_prevSend, 3);
                            }

                            _lastFromField = _liftUpFigure.Field;
                            _lastToField = liftDownField;
                            //_logger?.LogDebug($"1. Remove working chessboard from {_liftUpFigure.Field} and set to {liftDownField}");   
                            _workingChessBoard.RemoveFigureFromField(_liftUpFigure.Field);
                            _workingChessBoard.SetFigureOnPosition(_liftUpFigure.Figure, liftDownField);
                            _workingChessBoard.CurrentColor = _liftUpFigure.FigureColor == Fields.COLOR_WHITE
                                ? Fields.COLOR_BLACK
                                : Fields.COLOR_WHITE;

                            _liftUpFigure = null;
                            _liftUpEnemyFigure = null;
                            _prevSend = _workingChessBoard.GetFenPosition()
                                .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                            _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");
                            return new DataFromBoard(_prevSend, 3);
                        }

                        _lastFromField = _liftUpFigure.Field;
                        _lastToField = liftDownField;
                        // _logger?.LogDebug($"2. Remove working chessboard from {_liftUpFigure.Field} and set to {liftDownField}");
                        _chessBoard.RemoveFigureFromField(_liftUpFigure.Field);
                        _chessBoard.SetFigureOnPosition(_liftUpFigure.Figure, liftDownField);
                        _chessBoard.CurrentColor = _liftUpFigure.FigureColor == Fields.COLOR_WHITE
                            ? Fields.COLOR_BLACK
                            : Fields.COLOR_WHITE;
                        _workingChessBoard.Init(_chessBoard);
                        _liftUpFigure = null;
                        _liftUpEnemyFigure = null;
                        _prevSend = _chessBoard.GetFenPosition()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                        _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");
                        return new DataFromBoard(_prevSend, 3);
                    }
                }
                if (liftUpFigure != null)
                {
                    // _logger?.LogDebug($"3. Remove working chessboard from {liftUpFigure.Field} ");
                    _workingChessBoard.RemoveFigureFromField(liftUpFigure.Field);
                    if (_inDemoMode || liftUpFigure.Color == _chessBoard.CurrentColor)
                    {
                        if (_lastToField.Equals(liftUpFigure.Field))
                        {
                            liftUpFigure.Field = _lastFromField;
                        }
                        if (_liftUpFigure == null || _liftUpFigure.Field != liftUpFigure.Field)
                        {
                            _liftUpFigure = new PegasusFigure()
                            {
                                Field = liftUpFigure.Field,
                                Figure = liftUpFigure.FigureId,
                                FigureColor = liftUpFigure.Color
                            };
                            _logger?.LogDebug(
                                $"GetPiecesFen: new _liftUpFigure {FigureId.FigureIdToEnName[_liftUpFigure.Figure]}");
                        }
                    }
                    else
                    {
                        _liftUpEnemyFigure = new PegasusFigure()
                        {
                            Field = liftUpFigure.Field,
                            Figure = liftUpFigure.FigureId,
                            FigureColor = liftUpFigure.Color
                        };
                        _logger?.LogDebug(
                            $"GetPiecesFen: new _liftUpEnemyFigure {FigureId.FigureIdToEnName[_liftUpEnemyFigure.Figure]}");
                    }
                }

            }
            if (strings[0] == _boardBattery && strings[1] == "0" && strings[2] == "12")
            {
                BatteryLevel = strings[3];
                BatteryStatus = strings[11].Equals("1") ? "🔋" : "\uD83D\uDD0C";
                ;
            }

            if (strings[0] == _boardTrademark)
            {
                var trademark = string.Empty;
                for (int i = 3; i < strings.Length; i++)
                {
                    trademark += Encoding.UTF8.GetString(new byte[] { byte.Parse(strings[i]) });
                }

                Information = trademark;
            }
            var newSend = _workingChessBoard.GetFenPosition()
                .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
            if (!newSend.Equals((_prevSend)))
            {
                _logger?.LogDebug($"GetPiecesFen: return changes from {_prevSend} to {newSend}");
            }

            _prevSend = newSend;

            return new DataFromBoard(_prevSend, 3);
        }

        public override DataFromBoard GetDumpPiecesFen()
        {
            try
            {
                var newSend = _workingChessBoard.GetFenPosition()
                    .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                return new DataFromBoard(newSend, 3);
            }
            catch
            {
                return new DataFromBoard(_prevSend, 3);
            }
        }

        protected override void SetToNewGame()
        {
            lock (_locker)
            {
                _chessBoard.Init();
                _chessBoard.NewGame();
                _workingChessBoard.Init();
                _workingChessBoard.NewGame();
                _startFenPosition = string.Empty;
                _lastFromField = Fields.COLOR_OUTSIDE;
                _lastToField = Fields.COLOR_OUTSIDE;
                _awaitingMoveFromField = Fields.COLOR_OUTSIDE;
                _awaitingMoveToField = Fields.COLOR_OUTSIDE;
                _liftUpFigure = null;
                _liftUpEnemyFigure = null;
                lock (_lockThinking)
                {
                    _thinkingLeds.Clear();
                }
                _probingFields.TryDequeue(out _);
                _serialCommunication.Send(_batteryState);
                _serialCommunication.Send(_allLEDsOff);
            }
        }


        private void RequestADumpLoop()
        {
            while (!_stopAll)
            {
                Thread.Sleep(_dumpLoopWait);
                if (!_stopLoop)
                {
                    RequestDump();
                }
            }
        }


        private void HandleFomBoard()
        {
            while (!_release)
            {
                Thread.Sleep(10);
                var dataFromBoard = _serialCommunication.GetFromBoard();
                _fromBoard.Enqueue(dataFromBoard);
            }

            while (_fromBoard.TryDequeue(out _)) ;
        }

        private void HandleThinkingLeds()
        {
            List<byte> allBytes = new List<byte>();
            string lastSend = string.Empty;
            string send = string.Empty;
            while (!_release)
            {
                Thread.Sleep(10);
                lock (_lockThinking)
                {
                    if (_thinkingLeds.Count > 1)
                    {
                        send = string.Empty;
                        foreach (var thinkingLed in _thinkingLeds)
                        {
                            send += thinkingLed;
                        }

                        if (send.Equals(lastSend))
                        {
                            continue;
                        }
                        lastSend = send;
                        allBytes.Clear();
                        byte ledCount = byte.Parse((_thinkingLeds.Count + 5).ToString());
                        allBytes.Add(96);
                        allBytes.Add(ledCount);
                        allBytes.Add(5);
                        allBytes.Add(1); // Speed
                        allBytes.Add(0); // Times
                        allBytes.Add(1); // Intensity
                        foreach (var fieldName in _thinkingLeds)
                        {
                            if (PlayingWithWhite)
                            {
                                if (_fieldName2FieldByte.ContainsKey(fieldName.ToUpper()))
                                {
                                    allBytes.Add(_fieldName2FieldByte[fieldName.ToUpper()]);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (_invertedFieldName2FieldByte.ContainsKey(fieldName.ToUpper()))
                                {
                                    allBytes.Add(_invertedFieldName2FieldByte[fieldName.ToUpper()]);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        allBytes.Add(0);
                        _serialCommunication.Send(allBytes.ToArray());
                        Thread.Sleep(20);
                    }
                }
            }
        }

        private void ShowProbingMoves()
        {

            List<byte> allBytes = new List<byte>();
            while (!_release)
            {
                Thread.Sleep(10);
                if (_probingFields.TryDequeue(out ProbingMove[] fields))
                {
                    if (!_acceptProbingMoves)
                    {
                        // SetAllLEDsOff(true);
                        continue;
                    }
                    var probingMove = fields.OrderByDescending(f => f.Score).First();
                    allBytes.Clear();
                    allBytes.Add(96);
                    allBytes.Add(6);
                    allBytes.Add(5);
                    allBytes.Add(1); // Speed
                    allBytes.Add(0); // Times
                    allBytes.Add(1); // Intensity
                    {
                        if (PlayingWithWhite)
                        {
                            if (_fieldName2FieldByte.ContainsKey(probingMove.FieldName.ToUpper()))
                            {
                                allBytes.Add(_fieldName2FieldByte[probingMove.FieldName.ToUpper()]);
                            }
                        }
                        else
                        {
                            if (_invertedFieldName2FieldByte.ContainsKey(probingMove.FieldName.ToUpper()))
                            {
                                allBytes.Add(_invertedFieldName2FieldByte[probingMove.FieldName.ToUpper()]);
                            }
                        }
                    }
                    allBytes.Add(0);
                    _serialCommunication.Send(allBytes.ToArray());
                    Thread.Sleep(10);
                }
            }
        }
    }
}
