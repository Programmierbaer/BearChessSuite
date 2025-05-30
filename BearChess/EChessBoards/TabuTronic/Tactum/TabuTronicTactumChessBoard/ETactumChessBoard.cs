﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessTools;

namespace www.SoLaNoSoft.com.BearChess.TabuTronic.Tactum.ChessBoard
{
    public class ETactumChessBoard : AbstractEBoard
    {
        private class TactumFigure
        {
            public TactumFigure()
            {
                Figure = FigureId.NO_PIECE;
                Field = Fields.COLOR_EMPTY;
                FigureColor = FigureId.OUTSIDE_PIECE;
            }
            public int Figure { get; set; }
            public int Field { get; set; }
            public int FigureColor { get; set; }

            public string FieldName => Fields.GetFieldName(Field);
          
        }

        private const string BASE_POSITION = "255 255 0 0 0 0 255 255";
        private const string NEW_GAME_POSITION = "255 255 0 0 8 0 255 247";

      
        private readonly EChessBoardConfiguration _configuration;
        private readonly IChessBoard _chessBoard;
        private readonly IChessBoard _workingChessBoard;
        private string _prevJoinedString = string.Empty;
        private readonly ConcurrentQueue<DataFromBoard> _fromBoard = new ConcurrentQueue<DataFromBoard>();
        private bool _release = false;
        private TactumFigure _liftUpFigure = null;
        private TactumFigure _liftUpEnemyFigure = null;
        private int _lastFromField = Fields.COLOR_OUTSIDE;
        private int _lastToField = Fields.COLOR_OUTSIDE;
        private string _prevRead = string.Empty;
        private string _prevSend = string.Empty;
        private int _equalReadCount = 0;

        public override event EventHandler BasePositionEvent;
        public override event EventHandler NewGamePositionEvent;
        public override event EventHandler HelpRequestedEvent;
        public override event EventHandler<string> DataEvent;
        public override event EventHandler<string> GameEndEvent;

        private readonly object _lockThinking = new object();
        private string _startFenPosition = string.Empty;
        private readonly bool _useBluetooth;
        private readonly ISpeech _synthesizer;
        private readonly ResourceManager _rm;
        private readonly string _speechLanguageTag;
        private string _lastThinking0 = string.Empty;
        private string _lastThinking1 = string.Empty;
        private string _lastProbing = string.Empty;
        private BoardCodeConverter _awaitingFromBoard = null;
        private int _engineColor = Fields.COLOR_OUTSIDE;
        private int _debounce = 3;


        public ETactumChessBoard(string basePath, ILogging logger, EChessBoardConfiguration configuration)
        {
            _configuration = configuration;
            _useBluetooth = configuration.UseBluetooth;
            _rm = SpeechTranslator.ResourceManager;
            _serialCommunication = new SerialCommunication(logger, _configuration.PortName, _configuration.UseBluetooth);
            _logger = logger;
            BatteryLevel = "---";
            BatteryStatus = _rm.GetString("Full");
            PieceRecognition = false;
            ValidForAnalyse = true;
            MultiColorLEDs = true;
            UseFieldDumpForFEN = true;
            IsConnected = EnsureConnection();
            Information = Constants.TabutronicTactum;
            _chessBoard = new BearChessBase.Implementations.ChessBoard();
            _chessBoard.Init();
            _chessBoard.NewGame();
            _workingChessBoard = new BearChessBase.Implementations.ChessBoard();
            _workingChessBoard.Init();
            _workingChessBoard.NewGame();
            var handleFromBoardThread = new Thread(HandleFomBoard) { IsBackground = true };
            handleFromBoardThread.Start();
            _synthesizer = BearChessSpeech.Instance;
            _speechLanguageTag = Configuration.Instance.GetConfigValue("selectedSpeechCulture", CultureInfo.CurrentCulture.IetfLanguageTag).ToLower();
        }

        public ETactumChessBoard(ILogging logger)
        {
            _logger = logger;
            _rm = SpeechTranslator.ResourceManager;
            BatteryLevel = "---";
            BatteryStatus = _rm.GetString("Full");
            Information = Constants.TabutronicTactum;
            PieceRecognition = false;
            ValidForAnalyse = true;
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
                _logger?.LogDebug($"Set FEN: {fen}");
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
            _serialCommunication = new SerialCommunication(_logger, portName, _useBluetooth);
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
                return !string.IsNullOrEmpty(readLine);
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
            if (!EnsureConnection())
            {
                return;
            }

            lock (_locker)
            {
               // _logger?.LogDebug($"B: set LEDs  {ledsParameter}");
                if (!ledsParameter.IsThinking)
                {
                    lock (_lockThinking)
                    {
                        _lastThinking0 = string.Empty;
                        _lastThinking1 = string.Empty;
                    }
                }
                if (ledsParameter.IsThinking)
                {
                    lock (_lockThinking)
                    {
                        if (_lastThinking0 != ledsParameter.FieldNames[0] ||
                            _lastThinking1 != ledsParameter.FieldNames[1])
                        {
                            _lastThinking0 = ledsParameter.FieldNames[0];
                            _lastThinking1 = ledsParameter.FieldNames[1];

                            _synthesizer?.SpeakAsync(
                                $"{_rm.GetString("BestMove")} {Fields.GetBlindFieldName(ledsParameter.FieldNames[0])} {Fields.GetBlindFieldName(ledsParameter.FieldNames[1])}");

                        }

                        return;
                    }
                }
                if (ledsParameter.IsProbing && (_configuration.ShowPossibleMoves || _configuration.ShowPossibleMovesEval))
                {
                    var probingMove = ledsParameter.ProbingMoves.OrderByDescending(f => f.Score).First();
                    if (_lastProbing != probingMove.FieldName)
                    {
                        _lastProbing = probingMove.FieldName;
                        _synthesizer?.SpeakAsync($"{_rm.GetString("BestField")} {Fields.GetBlindFieldName(probingMove.FieldName)}" );
                        lock (_lockThinking)
                        {
                            _lastThinking0 = string.Empty;
                            _lastThinking1 = string.Empty;
                        }
                    }

                    return;
                }

                var fieldNames = new List<string>(ledsParameter.FieldNames);
                fieldNames.AddRange(ledsParameter.InvalidFieldNames);
                //string[] fieldNames = new List<string>(ledsParameter.FieldNames).AddRange(ledsParameter.InvalidFieldNames.ToList())
                if (fieldNames.Count == 0)
                {
                    fieldNames.AddRange(ledsParameter.BookFieldNames);
                    if (fieldNames.Count == 0)
                    {
                        return;
                    }

                }

                var joinedString = string.Join(" ", fieldNames);

                if (_prevJoinedString.Equals(joinedString))
                {
                    return;
                }
                if (ledsParameter.InvalidFieldNames.Length > 0)
                {
                    for (int i = 0; i < ledsParameter.InvalidFieldNames.Length; i++)
                    {
                        if (_awaitingCastleMoveToField != Fields.COLOR_OUTSIDE && _awaitingCastleMoveToField== Fields.GetFieldNumber(ledsParameter.InvalidFieldNames[i]))
                        {
                            continue;
                        }
                        _synthesizer?.SpeakAsync($"{_rm.GetString("Invalid")} { Fields.GetBlindFieldName(ledsParameter.InvalidFieldNames[i])}");
                    }
                }
                _logger?.LogDebug($"Set LEDs for {joinedString}");
                _prevJoinedString = joinedString;
                byte[] result = { 0, 0, 0, 0, 0, 0, 0, 0 };
            }
        }

        public override void AwaitingMove(int fromField, int toField, int promoteFigure = FigureId.NO_PIECE)
        {
            lock (_locker)
            {
                if ((fromField == toField) || (fromField<=0) || (toField<=0))
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
            _logger?.LogDebug("B: Send all off");

            lock (_lockThinking)
            {
                _lastThinking0 = string.Empty;
                _lastThinking1 = string.Empty;
                _lastProbing = string.Empty;
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
                _lastThinking0 = string.Empty;
                _lastThinking1 = string.Empty;
                _lastProbing = string.Empty;
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
            _debounce = debounce > 0 ? debounce : 3;
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

        public override string RequestInformation(string message)
        {
            return _chessBoard.GetFenPosition();
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
                   // _logger?.LogDebug($"GetPiecesFen: _equalReadCount: {_equalReadCount}");
                }
                else
                {
                    _logger?.LogDebug($"GetPiecesFen: Changes from {_prevRead} to {dataFromBoard.FromBoard}");
                    _prevRead = dataFromBoard.FromBoard;
                    _equalReadCount = 0;
                }
                if (dataFromBoard.FromBoard.StartsWith(BASE_POSITION))
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
                                _synthesizer?.SpeakAsync(
                                    Fields.GetBlindFieldName(Fields.GetFieldName(boardField)), true);
                                _logger?.LogDebug(
                                    $"GetPiecesFen: Invalid for awaiting from board: {boardField}/{Fields.GetFieldName(boardField)}");
                                //  break;
                            }
                        }
                    }
                    _logger?.LogDebug($"GetPiecesFen: return {_prevSend}");
                    return new DataFromBoard(_prevSend, 3);

                }


                if (_equalReadCount < _debounce * 35)
                {
                    if (string.IsNullOrWhiteSpace(_prevSend))
                    {
                        _prevSend = _workingChessBoard.GetFenPosition()
                            .Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    }

                    // _logger?.LogDebug($"GetPiecesFen: _equalReadCount {_equalReadCount} return {_prevSend}");
                    return new DataFromBoard(_prevSend, 3);
                }

                //if (_prevRead.Equals(dataFromBoard.FromBoard))
                //{
                //    _equalReadCount=0;
                //    _logger?.LogDebug($"GetPiecesFen: set _equalReadCount to {_equalReadCount}");
                //}
                // _logger?.LogDebug($"GetPiecesFen: fromBoard: {dataFromBoard.FromBoard}");
                if (dataFromBoard.FromBoard.StartsWith(BASE_POSITION))
                {
                    _chessBoard.Init();
                    _chessBoard.NewGame();
                    _workingChessBoard.Init();
                    _workingChessBoard.NewGame();
                    _startFenPosition = string.Empty;
                    BasePositionEvent?.Invoke(this, null);
                    string basePos =_workingChessBoard.GetFenPosition().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    //if (_configuration.SayLiftUpDownFigure && _prevSend != basePos)
                    if (_prevSend != basePos)
                    {
                        _synthesizer?.SpeakForce($"{SpeechTranslator.ResourceManager.GetString("BasePosition")}");
                    }

                    _prevSend = basePos;
                    return new DataFromBoard(_prevSend, 3)
                    {
                        BasePosition = true,
                        Invalid = false,
                        IsFieldDump = false
                    };
                }
                if (dataFromBoard.FromBoard.StartsWith(NEW_GAME_POSITION))
                {
                   
                  //  NewGamePositionEvent?.Invoke(this, null);
                    //string basePos = _workingChessBoard.GetFenPosition().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    if (_equalReadCount == 15)
                    {
                        _synthesizer?.SpeakForce($"{SpeechTranslator.ResourceManager.GetString("NewGame")}");
                        _synthesizer?.SpeakForce($"{SpeechTranslator.ResourceManager.GetString("GoBackToBasePosition")}");
                        NewGamePositionEvent?.Invoke(this, null);
                    }

                    //_prevSend = basePos;
                    DataEvent?.Invoke(this, "NewGame");
                    //return new DataFromBoard(_prevSend, 3)
                    //{
                       
                    //    Invalid = false,
                    //    IsFieldDump = false,
                       
                    //};
                }

                boardCodeConverter = new BoardCodeConverter(strings, _playWithWhite);
               // _logger?.LogDebug($"strings: {string.Join(" ",strings)}");
                IChessFigure liftUpFigure = null;
                int liftDownField = Fields.COLOR_OUTSIDE;
                foreach (var boardField in Fields.BoardFields)
                {
                    var isFigureOnBoard = boardCodeConverter.IsFigureOn(boardField);
                    var chessFigure = _workingChessBoard.GetFigureOn(boardField);
                    if (isFigureOnBoard && chessFigure.Color == Fields.COLOR_EMPTY)
                    {

                        _logger?.LogDebug($"GetPiecesFen: Downfield: {boardField}/{Fields.GetFieldName(boardField)}");
                        liftDownField = boardField;
                        if (_configuration.SayLiftUpDownFigure)
                        {
                            _synthesizer?.SpeakAsync(Fields.GetBlindFieldName(changes[1]));
                        }
                    }

                    if (!isFigureOnBoard && chessFigure.Color != Fields.COLOR_EMPTY)
                    {
                        _logger?.LogDebug($"GetPiecesFen: Lift up field: {boardField}/{Fields.GetFieldName(boardField)} {FigureId.FigureIdToEnName[chessFigure.FigureId]}");
                        if (_configuration.SayLiftUpDownFigure)
                        {
                            _synthesizer?.SpeakAsync(SpeechTranslator.GetFigureName(chessFigure.FigureId,_speechLanguageTag,Configuration.Instance)+ " " + Fields.GetBlindFieldName(changes[0]));
                        }
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
                    _logger?.LogInfo("GetPiecesFen: Take move back. Replay all previous moves");
                    var playedMoveList = _chessBoard.GetPlayedMoveList();
                    _chessBoard.Init();
                    _chessBoard.NewGame();
                    if (!string.IsNullOrWhiteSpace(_startFenPosition))
                    {
                        _chessBoard.SetPosition(_startFenPosition, false);
                    }

                    for (int i = 0; i < playedMoveList.Length - 1; i++)
                    {
                        _logger?.LogDebug($"GetPiecesFen: Move {playedMoveList[i]}");
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

                                _logger?.LogInfo("GetPiecesFen: Take move back. Replay all previous moves");
                                var playedMoveList = _chessBoard.GetPlayedMoveList();
                                _chessBoard.Init();
                                _chessBoard.NewGame();
                                if (!string.IsNullOrWhiteSpace(_startFenPosition))
                                {
                                    _chessBoard.SetPosition(_startFenPosition, false);
                                }
                                for (int i = 0; i < playedMoveList.Length - 1; i++)
                                {
                                    _logger?.LogDebug($"GetPiecesFen: Move {playedMoveList[i]}");
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

                            if ((_chessBoard.CurrentColor != _engineColor) && (_chessBoard.MoveIsValid(_liftUpFigure.Field, liftDownField)))
                            {
                                _logger?.LogDebug($"GetPiecesFen: Make move: {Fields.GetFieldName(_liftUpFigure.Field)} {Fields.GetFieldName(liftDownField)}");
                                _lastFromField = _liftUpFigure.Field;
                                _lastToField = liftDownField;
                                var aChessBoard = new BearChessBase.Implementations.ChessBoard();
                                aChessBoard.Init(_chessBoard);
                                aChessBoard.MakeMove(_liftUpFigure.Field, liftDownField);
                                var allMoveClass = aChessBoard.GetPrevMove();
                                var move = allMoveClass.GetMove(aChessBoard.EnemyColor);
                                if (_awaitingMoveFromField != Fields.COLOR_OUTSIDE)
                                {
                                    _logger?.LogDebug($"GetPiecesFen: Confirm move: {Fields.GetFieldName(move.FromField)} {Fields.GetFieldName(move.ToField)}");
                                    MoveExtentions.ConfirmMove(move);
                                }
                                else
                                {
                                    _synthesizer?.SpeakAsync(Fields.GetBlindFieldName(Fields.GetFieldName(liftDownField)), true);
                                }

                                _awaitingMoveFromField = Fields.COLOR_OUTSIDE;
                                _awaitingMoveToField = Fields.COLOR_OUTSIDE;
                                _awaitingCastleMoveFromField = Fields.COLOR_OUTSIDE;
                                _awaitingCastleMoveToField = Fields.COLOR_OUTSIDE;
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
                                else
                                {
                                    _awaitingFromBoard = null;
                                    _chessBoard.MakeMove(_liftUpFigure.Field, liftDownField);
                                }

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
                        _chessBoard.RemoveFigureFromField(_liftUpFigure.Field);
                        _chessBoard.SetFigureOnPosition(_liftUpFigure.Figure, liftDownField);
                        _chessBoard.CurrentColor = _liftUpFigure.FigureColor == Fields.COLOR_WHITE ? Fields.COLOR_BLACK : Fields.COLOR_WHITE;
                        _workingChessBoard.Init(_chessBoard);
                        _liftUpFigure = null;
                        _liftUpEnemyFigure = null;
                        _prevSend = _chessBoard.GetFenPosition().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
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
                            _liftUpFigure = new TactumFigure { Field = liftUpFigure.Field, Figure = liftUpFigure.FigureId, FigureColor = liftUpFigure.Color };
                            _logger?.LogDebug($"GetPiecesFen: new _liftUpFigure {FigureId.FigureIdToEnName[_liftUpFigure.Figure]}");
                          
                        }
                    }
                    else
                    {
                        _liftUpEnemyFigure = new TactumFigure { Field = liftUpFigure.Field, Figure = liftUpFigure.FigureId, FigureColor = liftUpFigure.Color };
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
                _awaitingCastleMoveFromField = Fields.COLOR_OUTSIDE;
                _awaitingCastleMoveToField = Fields.COLOR_OUTSIDE;
                _awaitingMoveFromField = Fields.COLOR_OUTSIDE;
                _awaitingMoveToField = Fields.COLOR_OUTSIDE;
                _engineColor = Fields.COLOR_OUTSIDE;
                _liftUpFigure = null;
                _liftUpEnemyFigure = null;
                _awaitingFromBoard = null;
                lock (_lockThinking)
                {
                    _lastThinking0 = string.Empty;
                    _lastThinking1 = string.Empty;
                    _lastProbing = string.Empty;
                }
            }
        }

        public override void SpeedLeds(int level)
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
                ;
            }
        }

    }
}
