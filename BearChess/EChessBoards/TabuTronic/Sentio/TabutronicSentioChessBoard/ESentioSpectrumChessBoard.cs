using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.Tabutronic.Sentio.ChessBoard
{
    public class ESentioSpectrumChessBoard : AbstractEBoard
    {
        private class SentioFigure
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

            public SentioFigure()
            {
                Figure = FigureId.NO_PIECE;
                Field = Fields.COLOR_EMPTY;
                FigureColor = FigureId.OUTSIDE_PIECE;
            }
        }

        private const string BASEPOSITION = "255 255 0 0 0 0 255 255";


        private readonly Dictionary<string, byte> _colName2ColByte = new Dictionary<string, byte>()
        {
            {"A", ColA},
            {"B", ColB},
            {"C", ColC},
            {"D", ColD},
            {"E", ColE},
            {"F", ColF},
            {"G", ColG},
            {"H", ColH},
        };

        private readonly Dictionary<string, byte> _flippedColName2ColByte = new Dictionary<string, byte>()
        {
            {"H", ColA},
            {"G", ColB},
            {"F", ColC},
            {"E", ColD},
            {"D", ColE},
            {"C", ColF},
            {"B", ColG},
            {"A", ColH},
        };

        private const byte ColA = 0x1;
        private const byte ColB = 0x1 << 1;
        private const byte ColC = 0x1 << 2;
        private const byte ColD = 0x1 << 3;
        private const byte ColE = 0x1 << 4;
        private const byte ColF = 0x1 << 5;
        private const byte ColG = 0x1 << 6;
        private const byte ColH = 0x1 << 7;

        private static readonly byte[] AllOff = Enumerable.Repeat<byte>(0, 247).ToArray();
        private static readonly byte[] AllOn = Enumerable.Repeat<byte>(125, 247).ToArray();
        private static readonly byte[] FieldArray = Enumerable.Repeat<byte>(0, 243).ToArray();
        private static readonly byte[] SendArray = Enumerable.Repeat<byte>(0, 247).ToArray();
        private readonly byte[] _lastSendBytes = Enumerable.Repeat<byte>(0, 243).ToArray();

        private int _currentColor;
        private int _prevColor;
        private int _currentEvalColor;
        private string _currentEval = "0";
        private readonly EChessBoardConfiguration _configuration;
        private readonly IChessBoard _chessBoard;
        private readonly IChessBoard _workingChessBoard;
        private string _prevJoinedString = string.Empty;
        private readonly ConcurrentQueue<DataFromBoard> _fromBoard = new ConcurrentQueue<DataFromBoard>();
        private bool _release = false;
        private SentioFigure _liftUpFigure = null;
        private SentioFigure _liftUpEnemyFigure = null;
        private int _lastFromField = Fields.COLOR_OUTSIDE;
        private int _lastToField = Fields.COLOR_OUTSIDE;
        private string _prevRead = string.Empty;
        private string _prevSend = string.Empty;
        private int _equalReadCount = 0;
        private int _debounce = 1;

        public override event EventHandler BasePositionEvent;
        public override event EventHandler NewGamePositionEvent;
        public override event EventHandler HelpRequestedEvent;
        public override event EventHandler<string> DataEvent;
        public override event EventHandler<string> GameEndEvent;

        private readonly object _lockThinking = new object();
        private readonly bool _showMoveLine;
        private readonly bool _showEvaluationValue;
        private readonly bool _showCurrentColor;
        private string _lastThinking0 = string.Empty;
        private string _lastThinking1 = string.Empty;
        private string _startFenPosition = string.Empty;
        private readonly bool _useBluetooth;
        private ResourceManager _rm;
        private readonly ExtendedEChessBoardConfiguration _extendedConfiguration;
        private static readonly object _lock = new object();
        private static readonly byte _whiteOnMoveFieldByte = 1;
        private static readonly byte _blackOnMoveFieldByte = 73;
        private readonly ConcurrentQueue<string[]> _flashFields = new ConcurrentQueue<string[]>();
        private readonly Dictionary<int, List<int>> _whiteFieldNamesToLedArray = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, List<int>> _blackFieldNamesToLedArray = new Dictionary<int, List<int>>();
        private static readonly byte[] _whiteAdvantageFieldBytes = { 9, 8, 7, 6, 5, 4, 3, 2 };
        private static readonly byte[] _blackAdvantageFieldBytes = { 81, 80, 79, 78, 77, 76, 75, 74 };
        private static readonly byte[] _bearChessFieldBytes1 = { 72, 71, 70, 63, 60, 54, 53, 52, 45, 36, 35, 34, 27, 24, 18, 17, 16, 69, 51, 33, 15 };
        private static readonly byte[] _bearChessFieldBytes2 = { 66, 65, 64, 58, 49, 40, 31, 22, 10, 11, 12 };
        private int _engineColor = Fields.COLOR_OUTSIDE;
        private BoardCodeConverter _awaitingFromBoard = null;

        public ESentioSpectrumChessBoard(string basePath, ILogging logger, EChessBoardConfiguration configuration)
        {
            _configuration = configuration;
            _extendedConfiguration = configuration.ExtendedConfig.First(e => e.IsCurrent);
            _serialCommunication = new SerialCommunication(logger, _configuration.PortName, _configuration.UseBluetooth, Constants.TabutronicSentio);
            _rm = SpeechTranslator.ResourceManager;
            _showCurrentColor = _extendedConfiguration.ShowCurrentColor;
            _useBluetooth = configuration.UseBluetooth;
            _currentColor = Fields.COLOR_WHITE;
            _prevColor = Fields.COLOR_BLACK;
            _logger = logger;
            BatteryLevel = "---";
            BatteryStatus = _rm.GetString("Full");
            PieceRecognition = false;
            ValidForAnalyse = false;
            MultiColorLEDs = true;
            SendArray[0] = 255;
            SendArray[1] = 85;
            SendArray[245] = 13;
            SendArray[246] = 10;
            AllOff[0] = 255;
            AllOff[1] = 85;
            AllOff[245] = 13;
            AllOff[246] = 10;
            AllOn[0] = 255;
            AllOn[1] = 85;
            AllOn[245] = 13;
            AllOn[246] = 10;
            InitFieldNamesToLedArray();
            IsConnected = EnsureConnection();
            Information = Constants.TabutronicSentioSpectrum;
            _chessBoard = new BearChessBase.Implementations.ChessBoard();
            _chessBoard.Init();
            _chessBoard.NewGame();
            _workingChessBoard = new BearChessBase.Implementations.ChessBoard();
            _workingChessBoard.Init();
            _workingChessBoard.NewGame();
            MultiColorLEDs = true;
            _acceptProbingMoves = true;
            var thread = new Thread(FlashLEDs) { IsBackground = true };
            thread.Start();
            var handleFromBoardThread = new Thread(HandleFomBoard) { IsBackground = true };
            handleFromBoardThread.Start();
        }

        public ESentioSpectrumChessBoard(ILogging logger)
        {
            _logger = logger;
            _rm = SpeechTranslator.ResourceManager;
            BatteryLevel = "---";
            BatteryStatus = _rm.GetString("Full");
            Information = Constants.TabutronicSentioSpectrum;
            PieceRecognition = false;
            ValidForAnalyse = false;
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
                _awaitingFromBoard = null;
                _chessBoard.Init();
                _chessBoard.NewGame();
                _chessBoard.SetPosition(fen, true);
                _workingChessBoard.Init();
                _workingChessBoard.NewGame();
                _workingChessBoard.SetPosition(fen, true);
                _startFenPosition = _workingChessBoard.GetFenPosition();
                _lastFromField = Fields.COLOR_OUTSIDE;
                _lastToField = Fields.COLOR_OUTSIDE;
                _awaitingMoveFromField = Fields.COLOR_OUTSIDE;
                _awaitingMoveToField = Fields.COLOR_OUTSIDE;
            }
        }

        public override void SetClock(int hourWhite, int minuteWhite, int secondWhite, int hourBlack, int minuteBlack, int secondBlack, int increments)
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
        public override void ResetClock()
        {
            //
        }
        public override void SetCurrentColor(int currentColor)
        {
            _currentColor = currentColor;
            ShowCurrentColorInfos();
        }

        public override void SetEngineColor(int color)
        {
            //
        }

        private void HandleFomBoard()
        {
            while (!_release)
            {
                Thread.Sleep(10);
                var dataFromBoard = _serialCommunication.GetFromBoard();
                _fromBoard.Enqueue(dataFromBoard);
            }

            while (_fromBoard.TryDequeue(out _))
            {
            }
        }
        public override bool CheckComPort(string portName)
        {
            _serialCommunication = new SerialCommunication(_logger, portName, _useBluetooth, Constants.TabutronicSentioSpectrum);
            if (_serialCommunication.CheckConnect(portName))
            {
                var readLine = string.Empty;
                var count = 0;
                while (string.IsNullOrWhiteSpace(readLine) && count < 10)
                {
                    readLine = _serialCommunication.GetRawFromBoard(string.Empty);
                    count++;
                    Thread.Sleep(10);
                }
                _serialCommunication.DisConnectFromCheck();
                return readLine?.Length > 0;
            }
            _serialCommunication.DisConnectFromCheck();
            return false;
        }

        public override bool CheckComPort(string portName, string baud)
        {
            return CheckComPort(portName);
        }

        public override void SetLedForFields(SetLEDsParameter ledsParameter)
        {
            lock (_lock)
            {
                //    _logger.LogDebug($"EB: Request set LEDs for {ledsParameter}");
                var fieldNamesList = new List<string>();
                var allFieldNamesList = new List<string>();
                var rgbMoveFrom = _extendedConfiguration.RGBMoveFrom;
                var rgbMoveTo = _extendedConfiguration.RGBMoveTo;
                var rgbInvalid = _extendedConfiguration.RGBInvalid;
                var rgbHelp = _extendedConfiguration.RGBHelp;
                var rgbTakeBack = _extendedConfiguration.RGBTakeBack;
                var rgbBookMove = _extendedConfiguration.RGBBookMove;
                if (ledsParameter.IsProbing && _extendedConfiguration.ShowPossibleMoves)
                {
                    _flashFields.TryDequeue(out _);
                    ResetLastSendBytes();
                    var currentBestMove = string.Empty;
                    var currentBestSore = decimal.Zero;
                    if (ledsParameter.ProbingMoves.Length < 1 || !_extendedConfiguration.ShowPossibleMovesEval)
                    {
                        SetLedForFields(ledsParameter.FieldNames, rgbMoveFrom);
                        if (ledsParameter.HintFieldNames.Length > 1)
                        {
                            SetLedForFields(ledsParameter.HintFieldNames, _extendedConfiguration.RGBPossibleMoves);
                        }
                        SetLedForFields(ledsParameter.BookFieldNames, rgbBookMove);
                    }
                    else
                    {
                        SetLedForFields(ledsParameter.BookFieldNames, rgbBookMove);

                        SetLedForFields(ledsParameter.FieldNames, rgbMoveFrom);
                        var strings = ledsParameter.ProbingMoves.Where(p => p.Score <= 1 && p.Score >= -1)
                            .OrderByDescending(p => p.Score).Select(p => p.FieldName).ToArray();
                        if (strings.Length > 0)
                        {
                            currentBestMove = strings[0];
                            currentBestSore = ledsParameter.ProbingMoves.First(p => p.FieldName.Equals(currentBestMove))
                                .Score;
                            SetLedForFields(strings, _extendedConfiguration.RGBPossibleMovesPlayable);
                        }
                        strings = ledsParameter.ProbingMoves.Where(p => p.Score > 1).OrderByDescending(p => p.Score)
                            .Select(p => p.FieldName).ToArray();
                        if (strings.Length > 0)
                        {
                            SetLedForFields(strings, _extendedConfiguration.RGBPossibleMovesGood);
                        }
                        else
                        {
                            if (currentBestSore > 0)
                            {
                                SetLedForFields(new[] { currentBestMove }, _extendedConfiguration.RGBPossibleMovesGood);
                            }
                        }

                        strings = ledsParameter.ProbingMoves.Where(p => p.Score <= -1).Select(p => p.FieldName)
                            .ToArray();
                        if (strings.Length > 0)
                        {
                            SetLedForFields(strings, _extendedConfiguration.RGBPossibleMovesBad);
                        }
                    }

                    return;
                }

                if (ledsParameter.FieldNames.Length == 2)
                {
                    if (_showMoveLine && !ledsParameter.IsError)
                    {
                        var moveLine =
                            MoveLineHelper.GetMoveLine(ledsParameter.FieldNames[0], ledsParameter.FieldNames[1]);
                        fieldNamesList.AddRange(moveLine);
                    }
                    else
                    {
                        fieldNamesList.AddRange(ledsParameter.FieldNames);
                    }
                }
                else
                {
                    fieldNamesList.AddRange(ledsParameter.FieldNames);

                }

                allFieldNamesList.AddRange(fieldNamesList);
                allFieldNamesList.AddRange(ledsParameter.InvalidFieldNames);
                allFieldNamesList.AddRange(ledsParameter.BookFieldNames);
                if (allFieldNamesList.Count == 0)
                {
                    return;
                }

                if (ledsParameter.IsThinking)
                {
                    _flashFields.TryDequeue(out _);
                    ResetLastSendBytes();
                    if (ledsParameter.RepeatLastMove)
                    {

                        _logger.LogDebug($"EB: Set thinking LEDs for {ledsParameter}");
                        SetLedForFields(fieldNamesList.ToArray(), rgbHelp);
                    }
                    else
                    {
                        _logger.LogDebug($"EB: Set thinking LEDs for {ledsParameter}");
                        SetLedForFields(fieldNamesList.ToArray(), rgbHelp);
                    }
                    SetLedForFields(ledsParameter.BookFieldNames, rgbBookMove);
                    return;
                }

                if (ledsParameter.IsMove)
                {
                    //_logger.LogDebug($"EB: Set move LEDs for {ledsParameter}");

                    if (_extendedConfiguration.SplitMoveFromMoveTo() && fieldNamesList.Count == 2)
                    {
                        var f = fieldNamesList.ToArray();
                        SetLedForFields(new[] { f[0] }, rgbMoveFrom);
                        SetLedForFields(new[] { f[1] }, rgbMoveTo);
                    }
                    else
                    {
                        SetLedForFields(fieldNamesList.ToArray(), rgbMoveFrom);
                    }

                    SetLedForFields(ledsParameter.InvalidFieldNames, rgbInvalid);
                    SetLedForFields(ledsParameter.HintFieldNames, rgbHelp);
                    SetLedForFields(ledsParameter.BookFieldNames, rgbBookMove);
                    _flashFields.TryDequeue(out _);
                    _flashFields.Enqueue(new string[] { fieldNamesList[0], fieldNamesList[1] });
                    return;
                }

                if (ledsParameter.IsTakeBack)
                {
                    _flashFields.TryDequeue(out _);
                    _logger.LogDebug($"EB: Set take back LEDs for {ledsParameter}");
                    SetLedForFields(fieldNamesList.ToArray(), rgbTakeBack);
                    return;
                }

                if (ledsParameter.IsError)
                {
                    _flashFields.TryDequeue(out _);
                    _logger.LogDebug($"EB: Set error LEDs for {ledsParameter}");
                    SetLedForFields(fieldNamesList.ToArray(), rgbMoveFrom);
                    SetLedForFields(ledsParameter.InvalidFieldNames, rgbInvalid);


                    return;
                }

                if (ledsParameter.BookFieldNames.Length > 0)
                {
                    SetAllLEDsOff(true);
                    SetLedForFields(ledsParameter.BookFieldNames, rgbBookMove);
                }

                _logger.LogError($"EB: Request without valid indicator set LEDs for {ledsParameter}");
            }
        }

        public override void AwaitingMove(int fromField, int toField, int promoteFigure = FigureId.NO_PIECE)
        {
            lock (_locker)
            {

                return;
                if ((fromField == toField) || (fromField <= 0) || (toField <= 0))
                {
                    return;
                }

                _engineColor = _chessBoard.GetFigureOn(fromField).Color;
                base.AwaitingMove(fromField, toField, promoteFigure);
                var awaitingChessBoard = new BearChessBase.Implementations.ChessBoard();
                awaitingChessBoard.Init(_chessBoard);
                awaitingChessBoard.MakeMove(fromField, toField, promoteFigure);
                _awaitingFromBoard = new BoardCodeConverter(_playWithWhite);
                var awaitingChessFigures = awaitingChessBoard.GetFigures(Fields.COLOR_WHITE);
                foreach (var chessFigure in awaitingChessFigures)
                {
                    _awaitingFromBoard.SetFigureOn(chessFigure.Field);
                }

                awaitingChessFigures = awaitingChessBoard.GetFigures(Fields.COLOR_BLACK);
                foreach (var chessFigure in awaitingChessFigures)
                {
                    _awaitingFromBoard.SetFigureOn(chessFigure.Field);
                }
            }
        }
        public override void SetAllLEDsOff(bool forceOff)
        {
            if (!EnsureConnection())
            {
                return;
            }
            _flashFields.TryDequeue(out _);
            lock (_locker)
            {
                _logger?.LogDebug("B: Send all off");
                _serialCommunication.ClearToBoard();
                Array.Copy(FieldArray, _lastSendBytes, FieldArray.Length);
                if (forceOff)
                {
                    _serialCommunication.Send(AllOff, true);
                }
                else
                {
                    ShowCurrentColorInfos();
                }
            }
        }

        public override void SetAllLEDsOn()
        {
            if (!EnsureConnection())
            {
                return;
            }

            var boardFields = new List<string>();
            var result = Enumerable.Repeat<byte>(0, 243).ToArray();
            foreach (var boardField in _bearChessFieldBytes1)
            {
                result[boardField * 3 - 1] = 0;
                result[boardField * 3 - 2] = 155;
                result[boardField * 3 - 3] = 00;
            }

            foreach (var boardField in _bearChessFieldBytes2)
            {
                result[boardField * 3 - 1] = 0;
                result[boardField * 3 - 2] = 0;
                result[boardField * 3 - 3] = 155;
            }


            foreach (var boardField in Fields.BoardFields)
            {
                boardFields.Add(Fields.GetFieldName(boardField));
            }

            lock (_locker)
            {
                _logger?.LogDebug("B: Send all on");
                //SetLedForFields(boardFields.ToArray(), "055", "000", "000");
                //Thread.Sleep(1200);
                //SetLedForFields(boardFields.ToArray(), "000", "055", "000");
                //Thread.Sleep(1200);
                //SetLedForFields(boardFields.ToArray(), "000", "000", "055");
                //Thread.Sleep(1200);
                Array.Copy(result, 0, SendArray, 2, result.Length);
                _serialCommunication.Send(SendArray);
                Thread.Sleep(1200);
            }
        }

        public override void DimLeds(bool dimLeds)
        {
            //
        }

        public override void DimLeds(int level)
        {
            //
        }

        public override void SetScanTime(int scanTime)
        {
            //
        }

        public override void SetDebounce(int debounce)
        {
            _debounce = debounce > 0 ? debounce : 1;
        }

        public override void FlashMode(EnumFlashMode flashMode)
        {
            //
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
            //
        }

        public override void RequestDump()
        {
            //
        }

        public override DataFromBoard GetPiecesFen()
        {
            // return GetDumpPiecesFen();
            string[] changes = { "", "" };
            BoardCodeConverter boardCodeConverter = null;
            if (_fromBoard.TryDequeue(out var dataFromBoard))
            {
                var strings = dataFromBoard.FromBoard.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (strings.Length < 8)
                {
                    return new DataFromBoard(_workingChessBoard.GetFenPosition().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0], 3);
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
                if (dataFromBoard.FromBoard.StartsWith(BASEPOSITION))
                {
                    _awaitingFromBoard = null;
                }

                if (_awaitingFromBoard != null)
                {
                    var bc = new BoardCodeConverter(strings, _playWithWhite);
                    if (bc.SamePosition(_awaitingFromBoard))
                    {
                        _logger?.LogDebug("GetPiecesFen: read awaited board position ");
                        _liftUpFigure = null;
                        _liftUpEnemyFigure = null;
                        _prevSend = _chessBoard.GetFenPosition()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                        _awaitingFromBoard = null;
                        if (_awaitingMoveFromField != Fields.COLOR_OUTSIDE)
                        {
                            _chessBoard.MakeMove(_awaitingMoveFromField, _awaitingMoveToField);
                            _prevSend = _chessBoard.GetFenPosition()
                                .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                            var allMoveClass = _chessBoard.GetPrevMove();
                            var move = allMoveClass.GetMove(_chessBoard.EnemyColor);
                            _logger?.LogDebug(
                                $"GetPiecesFen: Confirm move: {Fields.GetFieldName(move.FromField)} {Fields.GetFieldName(move.ToField)}");
                            MoveExtentions.ConfirmMove(move);
                        }

                        if (_awaitingCastleMoveFromField != Fields.COLOR_OUTSIDE)
                        {
                            _chessBoard.MakeMove(_awaitingCastleMoveFromField, _awaitingCastleMoveToField);
                            _prevSend = _chessBoard.GetFenPosition()
                                .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                        }

                        _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");
                        _workingChessBoard.Init(_chessBoard);
                        _awaitingMoveFromField = Fields.COLOR_OUTSIDE;
                        _awaitingMoveToField = Fields.COLOR_OUTSIDE;
                        _awaitingCastleMoveFromField = Fields.COLOR_OUTSIDE;
                        _awaitingCastleMoveToField = Fields.COLOR_OUTSIDE;
                        _engineColor = Fields.COLOR_OUTSIDE;
                        return new DataFromBoard(_prevSend, 3);
                    }

                    if (_equalReadCount == 0)
                    {
                        boardCodeConverter = new BoardCodeConverter(strings, _playWithWhite);
                        foreach (var boardField in Fields.BoardFields)
                        {
                            var isFigureOnBoard = boardCodeConverter.IsFigureOn(boardField);
                            var chessFigure = _chessBoard.GetFigureOn(boardField);
                            if (isFigureOnBoard && chessFigure.Color == Fields.COLOR_EMPTY)
                            {
                               
                                _logger?.LogDebug(
                                    $"GetPiecesFen: Invalid for awaiting from board: {boardField}/{Fields.GetFieldName(boardField)}");
                                //  break;
                            }
                        }
                    }
                    _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");
                    return new DataFromBoard(_prevSend, 3);

                }


                if (_equalReadCount < _debounce * 10)
                {
                    if (string.IsNullOrWhiteSpace(_prevSend))
                    {
                        _prevSend = _workingChessBoard.GetFenPosition().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    }

                    // _logger?.LogDebug($"GetPiecesFen: _equalReadCount {_equalReadCount} return {_prevSend}");
                    return new DataFromBoard(_prevSend, 3);
                }
                // _logger?.LogDebug($"GetPiecesFen: fromBoard: {dataFromBoard.FromBoard}");
                if (dataFromBoard.FromBoard.StartsWith(BASEPOSITION))
                {
                    _chessBoard.Init();
                    _chessBoard.NewGame();
                    _workingChessBoard.Init();
                    _workingChessBoard.NewGame();
                    _startFenPosition = string.Empty;
                    BasePositionEvent?.Invoke(this, null);
                    var basePos = _workingChessBoard.GetFenPosition().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    //if (_configuration.SayLiftUpDownFigure && _prevSend != basePos)


                    _prevSend = basePos;
                    return new DataFromBoard(_prevSend, 3)
                    {
                        BasePosition = true,
                        Invalid = false,
                        IsFieldDump = false
                    };
                }

                boardCodeConverter = new BoardCodeConverter(strings, _playWithWhite);
              //  _logger?.LogDebug($"GetPieceFen: Dump{Environment.NewLine}{boardCodeConverter.DumpBoard()}");
                // _logger?.LogDebug($"strings: {string.Join(" ",strings)}");
                changes[0] = string.Empty;
                changes[1] = string.Empty;
                IChessFigure liftUpFigure = null;
                var liftDownField = Fields.COLOR_OUTSIDE;
                foreach (var boardField in Fields.BoardFields)
                {
                    var isFigureOnBoard = boardCodeConverter.IsFigureOn(boardField);
                    var chessFigure = _workingChessBoard.GetFigureOn(boardField);
                    if (isFigureOnBoard && chessFigure.Color == Fields.COLOR_EMPTY)
                    {
                        changes[1] = Fields.GetFieldName(boardField);
                        _logger?.LogDebug($"GetPiecesFen: Downfield: {boardField}/{changes[1]}");
                        liftDownField = boardField;

                    }

                    if (!isFigureOnBoard && chessFigure.Color != Fields.COLOR_EMPTY)
                    {
                        changes[0] = Fields.GetFieldName(boardField);
                        _logger?.LogDebug($"GetPiecesFen: Lift up field: {boardField}/{changes[0]} {FigureId.FigureIdToEnName[chessFigure.FigureId]}");

                        liftUpFigure = chessFigure;

                    }
                }
                // Nothing changed?
                if (liftDownField == Fields.COLOR_OUTSIDE && liftUpFigure == null)
                {
                    return new DataFromBoard(_prevSend, 3);
                }

                var codeConverter = new BoardCodeConverter(_playWithWhite);
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
                    if (liftDownField == _liftUpEnemyFigure.Field && (_liftUpFigure == null && liftUpFigure == null))
                    {
                        _logger?.LogDebug($"GetPiecesFen: Equal lift up/down field: {_liftUpEnemyFigure.Field} == {liftDownField}");
                        _workingChessBoard.Init(_chessBoard);
                        _liftUpFigure = null;
                        _liftUpEnemyFigure = null;
                        //    _logger?.LogDebug($"GetPiecesFen: return valid {fenPosition1}");
                        _prevSend = _chessBoard.GetFenPosition()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                        _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");

                        return new DataFromBoard(_prevSend, 3);

                    }
                }

                if (_liftUpEnemyFigure != null && (!_inDemoMode && _allowTakeBack && liftDownField.Equals(_lastFromField) &&
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

                    for (int i = 0; i < playedMoveList.Length - 1; i++)
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
                        _logger?.LogDebug($"GetPiecesFen: Equal lift up/down field: {_liftUpFigure.Field} == {liftDownField}");
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
                                        _logger?.LogDebug($"GetPiecesFen: Awaited: {Fields.GetFieldName(_awaitingMoveFromField)} {Fields.GetFieldName(_awaitingMoveToField)}");
                                        _logger?.LogDebug($"GetPiecesFen: Read: {Fields.GetFieldName(_liftUpFigure.Field)} {Fields.GetFieldName(liftDownField)}");
                                        return new DataFromBoard(_prevSend, 3);
                                    }
                                }
                                _logger?.LogDebug($"GetPiecesFen: 1. Make move: {Fields.GetFieldName(_liftUpFigure.Field)} {Fields.GetFieldName(liftDownField)}");
                                _lastFromField = _liftUpFigure.Field;
                                _lastToField = liftDownField;

                                _awaitingMoveFromField = Fields.COLOR_OUTSIDE;
                                _awaitingMoveToField = Fields.COLOR_OUTSIDE;
                                _awaitingCastleMoveFromField = Fields.COLOR_OUTSIDE;
                                _awaitingCastleMoveToField = Fields.COLOR_OUTSIDE;
                                var aChessBoard = new BearChessBase.Implementations.ChessBoard();
                                aChessBoard.Init(_chessBoard);
                                aChessBoard.MakeMove(_liftUpFigure.Field, liftDownField);
                                var allMoveClass = aChessBoard.GetPrevMove();
                                var move = allMoveClass.GetMove(aChessBoard.EnemyColor);
                                if (move.IsCastleMove())
                                {
                                    _awaitingCastleMoveFromField = _liftUpFigure.Field;
                                    _awaitingCastleMoveToField = liftDownField;
                                    _logger?.LogDebug("GetPiecesFen: Awaiting castle position");
                                    _awaitingFromBoard = new BoardCodeConverter(_playWithWhite);
                                    var awaitingChessFigures = aChessBoard.GetFigures(Fields.COLOR_WHITE);
                                    foreach (var chessFigure in awaitingChessFigures)
                                    {
                                        _awaitingFromBoard.SetFigureOn(chessFigure.Field);
                                    }

                                    awaitingChessFigures = aChessBoard.GetFigures(Fields.COLOR_BLACK);
                                    foreach (var chessFigure in awaitingChessFigures)
                                    {
                                        _awaitingFromBoard.SetFigureOn(chessFigure.Field);
                                    }

                                    return new DataFromBoard(_prevSend, 3);
                                }
                                _awaitingFromBoard = null;
                                _logger?.LogDebug($"GetPiecesFen: 2. Make move: {Fields.GetFieldName(_liftUpFigure.Field)} {Fields.GetFieldName(liftDownField)}");
                                _chessBoard.MakeMove(_liftUpFigure.Field, liftDownField);
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
                            _workingChessBoard.CurrentColor = _liftUpFigure.FigureColor == Fields.COLOR_WHITE ? Fields.COLOR_BLACK : Fields.COLOR_WHITE;

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
                        _chessBoard.CurrentColor = _liftUpFigure.FigureColor == Fields.COLOR_WHITE ? Fields.COLOR_BLACK : Fields.COLOR_WHITE;
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
                            _liftUpFigure = new SentioFigure() { Field = liftUpFigure.Field, Figure = liftUpFigure.FigureId, FigureColor = liftUpFigure.Color };
                            _logger?.LogDebug($"GetPiecesFen: new _liftUpFigure {FigureId.FigureIdToEnName[_liftUpFigure.Figure]}");

                        }
                    }
                    else
                    {
                        _liftUpEnemyFigure = new SentioFigure { Field = liftUpFigure.Field, Figure = liftUpFigure.FigureId, FigureColor = liftUpFigure.Color };
                        _logger?.LogDebug($"GetPiecesFen: new _liftUpEnemyFigure {FigureId.FigureIdToEnName[_liftUpEnemyFigure.Figure]}");
                    }
                }
            }
            var newSend = _workingChessBoard.GetFenPosition().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
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
            if (_fromBoard.TryDequeue(out var dataFromBoard))
            {
                var strings =
                    dataFromBoard.FromBoard.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (strings.Length < 8)
                {
                    return new DataFromBoard(string.Empty, 3);
                }

                var boardCodeConverter = new BoardCodeConverter(strings, _playWithWhite);
                var dumpFields = new List<string>();
                foreach (var boardField in Fields.BoardFields)
                {
                    if (boardCodeConverter.IsFigureOn(boardField))
                    {
                        dumpFields.Add(Fields.GetFieldName(boardField));
                    }
                }

                return new DataFromBoard(string.Join(",", dumpFields), 3)
                {
                    IsFieldDump = true,
                    BasePosition = false
                };
            }
            return new DataFromBoard(string.Empty, 3);
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
                _awaitingCastleMoveFromField = Fields.COLOR_OUTSIDE;
                _awaitingCastleMoveToField = Fields.COLOR_OUTSIDE;
                _liftUpFigure = null;
                _liftUpEnemyFigure = null;
                _currentEval = string.Empty;
                lock (_lockThinking)
                {
                    _lastThinking0 = string.Empty;
                    _lastThinking1 = string.Empty;
                }
            }
        }

        public override void SpeedLeds(int level)
        {
            //
        }

        private void ResetLastSendBytes()
        {
            Array.Copy(FieldArray, _lastSendBytes, FieldArray.Length);
        }

        private void ShowCurrentColorInfos()
        {
            _logger?.LogDebug($"B: Show current color infos {_currentColor}");
            _prevColor = _currentColor;
            var red = _showCurrentColor ? _extendedConfiguration.RGBCurrentColor.Substring(0, 3) : "000";
            var green = _showCurrentColor ? _extendedConfiguration.RGBCurrentColor.Substring(3, 3) : "000";
            var blue = _showCurrentColor ? _extendedConfiguration.RGBCurrentColor.Substring(6, 3) : "000";
            var result = Enumerable.Repeat<byte>(0, 243).ToArray();
            Array.Copy(FieldArray, result, FieldArray.Length);
            if (_showCurrentColor)
            {
                _logger?.LogDebug("B: Add LED for current color indication");
                if (_currentColor == Fields.COLOR_WHITE)
                {
                    if (_playWithWhite)
                    {
                        result[_whiteOnMoveFieldByte * 3 - 1] = byte.Parse(blue);
                        result[_whiteOnMoveFieldByte * 3 - 2] = byte.Parse(green);
                        result[_whiteOnMoveFieldByte * 3 - 3] = byte.Parse(red);
                    }
                    else
                    {
                        result[_blackOnMoveFieldByte * 3 - 1] = byte.Parse(blue);
                        result[_blackOnMoveFieldByte * 3 - 2] = byte.Parse(green);
                        result[_blackOnMoveFieldByte * 3 - 3] = byte.Parse(red);
                    }
                }
                else
                {
                    if (_playWithWhite)
                    {
                        result[_blackOnMoveFieldByte * 3 - 1] = byte.Parse(blue);
                        result[_blackOnMoveFieldByte * 3 - 2] = byte.Parse(green);
                        result[_blackOnMoveFieldByte * 3 - 3] = byte.Parse(red);
                    }
                    else
                    {
                        result[_whiteOnMoveFieldByte * 3 - 1] = byte.Parse(blue);
                        result[_whiteOnMoveFieldByte * 3 - 2] = byte.Parse(green);
                        result[_whiteOnMoveFieldByte * 3 - 3] = byte.Parse(red);
                    }
                }
            }

            if (!_showEvaluationValue)
            {
                Array.Copy(result, 0, SendArray, 2, result.Length);
                _serialCommunication.Send(SendArray);
                return;
            }
            if (!_currentEvalColor.Equals(_currentColor))
            {
                Array.Copy(result, 0, SendArray, 2, result.Length);
                _serialCommunication.Send(SendArray);
                return;
            }
            if (string.IsNullOrEmpty(_currentEval))
            {
                _currentEval = "0";
            }
            if (decimal.TryParse(_currentEval.Replace(".", ","), out var eval))
            {
                if (eval > 0)
                {
                    red = _extendedConfiguration.RGBEvalAdvantage.Substring(0, 3);
                    green = _extendedConfiguration.RGBEvalAdvantage.Substring(3, 3);
                    blue = _extendedConfiguration.RGBEvalAdvantage.Substring(6, 3);
                }
                else
                {
                    red = _extendedConfiguration.RGBEvalDisAdvantage.Substring(0, 3);
                    green = _extendedConfiguration.RGBEvalDisAdvantage.Substring(3, 3);
                    blue = _extendedConfiguration.RGBEvalDisAdvantage.Substring(6, 3);
                }

                var number = GetNumberForEval(eval);
                if (_currentColor == Fields.COLOR_WHITE)
                {
                    for (var i = 0; i < number; i++)
                    {
                        if (_playWithWhite)
                        {
                            result[_whiteAdvantageFieldBytes[i] * 3 - 1] = byte.Parse(blue);
                            result[_whiteAdvantageFieldBytes[i] * 3 - 2] = byte.Parse(green);
                            result[_whiteAdvantageFieldBytes[i] * 3 - 3] = byte.Parse(red);
                        }
                        else
                        {
                            result[_blackAdvantageFieldBytes[i] * 3 - 1] = byte.Parse(blue);
                            result[_blackAdvantageFieldBytes[i] * 3 - 2] = byte.Parse(green);
                            result[_blackAdvantageFieldBytes[i] * 3 - 3] = byte.Parse(red);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < number; i++)
                    {
                        if (_playWithWhite)
                        {
                            result[_blackAdvantageFieldBytes[i] * 3 - 1] = byte.Parse(blue);
                            result[_blackAdvantageFieldBytes[i] * 3 - 2] = byte.Parse(green);
                            result[_blackAdvantageFieldBytes[i] * 3 - 3] = byte.Parse(red);
                        }
                        else
                        {
                            result[_whiteAdvantageFieldBytes[i] * 3 - 1] = byte.Parse(blue);
                            result[_whiteAdvantageFieldBytes[i] * 3 - 2] = byte.Parse(green);
                            result[_whiteAdvantageFieldBytes[i] * 3 - 3] = byte.Parse(red);
                        }
                    }
                }
                _logger?.LogDebug($"B: Add LEDs for evaluation score {eval}");
                Array.Copy(result, 0, SendArray, 2, result.Length);
                _serialCommunication.Send(SendArray);
            }
        }

        private byte[] UpdateLEDsForField(string fieldName, byte[] current, byte red, byte green, byte blue)
        {
            // Exact two letters expected, e.g. "E2"
            if (string.IsNullOrWhiteSpace(fieldName) || fieldName.Length != 2)
            {
                return current;
            }

            var fieldNumber = Fields.GetFieldNumber(fieldName);
            if (fieldNumber == Fields.COLOR_EMPTY)
            {
                return current;
            }
            var result = Enumerable.Repeat<byte>(0, 243).ToArray();
            Array.Copy(current, result, current.Length);
            if (_playWithWhite)
            {
                foreach (var x1 in _whiteFieldNamesToLedArray[fieldNumber])
                {
                    result[x1 * 3 - 1] = blue;
                    result[x1 * 3 - 2] = green;
                    result[x1 * 3 - 3] = red;
                }
            }
            else
            {
                foreach (var x1 in _blackFieldNamesToLedArray[fieldNumber])
                {
                    result[x1 * 3 - 1] = blue;
                    result[x1 * 3 - 2] = green;
                    result[x1 * 3 - 3] = red;
                }
            }
            return result;
        }

        private void InitFieldNamesToLedArray()
        {
            var count = 1;
            for (var i = 1; i < 9; i++)
            {
                foreach (var boardField in Fields.RowFields(i).OrderByDescending((f => f)))
                {
                    _whiteFieldNamesToLedArray[boardField] = new List<int> { count, count + 1, count + 9, count + 10 };
                    count++;
                }
                count++;
            }
            count = 1;
            for (var i = 8; i > 0; i--)
            {
                foreach (var boardField in Fields.RowFields(i).OrderBy((f => f)))
                {
                    _blackFieldNamesToLedArray[boardField] = new List<int> { count, count + 1, count + 9, count + 10 };
                    count++;
                }
                count++;
            }
        }

        private void SetLedForFields(string[] fieldNames, string rgbCodes)
        {
            if (rgbCodes.Length < 9)
            {
                return;
            }
            SetLedForFields(fieldNames, rgbCodes.Substring(0, 3), rgbCodes.Substring(3, 3), rgbCodes.Substring(6, 3));
        }

        private void SetLedForFields(string[] fieldNames, string red, string green, string blue)
        {
            var result = Enumerable.Repeat<byte>(0, 243).ToArray();
            Array.Copy(_lastSendBytes, result, _lastSendBytes.Length);
            foreach (var fieldName in fieldNames)
            {
                if (fieldName == "CC")
                {
                    result[_whiteOnMoveFieldByte * 3 - 1] = byte.Parse(blue);
                    result[_whiteOnMoveFieldByte * 3 - 2] = byte.Parse(green);
                    result[_whiteOnMoveFieldByte * 3 - 3] = byte.Parse(red);
                    break;
                }
                if (fieldName == "AD")
                {
                    foreach (var whiteAdvantageFieldByte in _whiteAdvantageFieldBytes)
                    {
                        result[whiteAdvantageFieldByte * 3 - 1] = byte.Parse(blue);
                        result[whiteAdvantageFieldByte * 3 - 2] = byte.Parse(green);
                        result[whiteAdvantageFieldByte * 3 - 3] = byte.Parse(red);

                    }
                    break;
                }
                if (fieldName == "DA")
                {
                    foreach (var x1 in _whiteAdvantageFieldBytes.OrderByDescending(f => f).ToArray())
                    {
                        result[x1 * 3 - 1] = byte.Parse(blue);
                        result[x1 * 3 - 2] = byte.Parse(green);
                        result[x1 * 3 - 3] = byte.Parse(red);
                    }

                    break;
                }
                result = UpdateLEDsForField(fieldName, result, byte.Parse(red), byte.Parse(green), byte.Parse(blue));
            }
            Array.Copy(result, 0, SendArray, 2, result.Length);
            Array.Copy(result, _lastSendBytes, result.Length);
            _serialCommunication.Send(SendArray);
        }

        private void SetLedForFlash(string fieldName, string red, string green, string blue)
        {
            var result = Enumerable.Repeat<byte>(0, 243).ToArray();
            

                result = UpdateLEDsForField(fieldName, result, byte.Parse(red), byte.Parse(green), byte.Parse(blue));
            
            Array.Copy(result, 0, SendArray, 2, result.Length);

            _serialCommunication.Send(SendArray);
        }

        private void FlashLEDs()
        {
            bool switchSide = false;
            while (!_release)
            {
                if (_flashFields.TryPeek(out string[] fields))
                {
                    if (_extendedConfiguration.FlashMoveFrom || _extendedConfiguration.FlashMoveTo)
                    {
                        if (switchSide)
                        {

                            SetLedForFlash(fields[1], _extendedConfiguration.RGBMoveTo.Substring(0, 3),
                                _extendedConfiguration.RGBMoveTo.Substring(3, 3),
                                _extendedConfiguration.RGBMoveTo.Substring(6, 3));
                        }
                        else
                        {
                            SetLedForFlash(fields[0], _extendedConfiguration.RGBMoveFrom.Substring(0, 3),
                                _extendedConfiguration.RGBMoveFrom.Substring(3, 3),
                                _extendedConfiguration.RGBMoveFrom.Substring(6, 3));
                        }

                        switchSide = !switchSide;
                    }

                    Thread.Sleep(1200);
                    continue;

                }

                Thread.Sleep(10);
            }
        }
        
        private int GetNumberForEval(decimal eval)
        {
            int number = 0;
            if (eval < 0)
            {
                if (eval <= -1)
                {
                    number = 1;
                }

                if (eval <= -2)
                {
                    number = 2;
                }

                if (eval <= -3)
                {
                    number = 3;
                }

                if (eval <= -4)
                {
                    number = 4;
                }

                if (eval <= -5)
                {
                    number = 5;
                }

                if (eval <= -6)
                {
                    number = 6;
                }

                if (eval <= -7)
                {
                    number = 7;
                }
            }
            else
            {
                if (eval >= 1)
                {
                    number = 1;
                }

                if (eval >= 2)
                {
                    number = 2;
                }

                if (eval >= 3)
                {
                    number = 3;
                }

                if (eval >= 4)
                {
                    number = 4;
                }

                if (eval >= 5)
                {
                    number = 5;
                }

                if (eval >= 6)
                {
                    number = 6;
                }

                if (eval >= 7)
                {
                    number = 7;
                }
            }
            return number;
        }

      
    }
}