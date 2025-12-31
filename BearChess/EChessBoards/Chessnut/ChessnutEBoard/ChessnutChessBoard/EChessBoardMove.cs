using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.ChessnutChessBoard
{
    public class EChessBoardMove : AbstractEBoard
    {
        private readonly bool _useBluetooth;
        private readonly bool _showMoveLine;
        private bool _release = false;
        private readonly byte[] _startReading = { 0x21, 0x01, 0x00 };
        private readonly byte[] _commandPrefix = { 0x42, 0x21 };
        private readonly byte[] _ledPrefix = { 0x43, 0x20 };
        private readonly byte[] _basePositionsBytes =
        {
            0x42, 0x21, 
            0x58, 0x23, 0x31, 0x85, 0x44, 0x44, 0x44, 0x44,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x77, 0x77, 0x77, 0x77, 0xA6, 0xC9, 0x9B, 0x6A,
            0x00
            
        };

        private readonly byte[] _stopMoveBytes =
        {
            0x42, 0x21,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00

        };

        private readonly byte[] _allLEDsOff =
        {
            0x43, 0x20,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00
        };
        private readonly byte[] _allLEDsOn = {
            0x43, 0x20,
            0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,
            0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,
            0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,
            0x22, 0x22
        };

        private string _prevJoinedString = string.Empty;
        private string _lastResult = string.Empty;
        private int _prevLedField = 0;
        public static byte ColH = 0x1;
        public static byte ColG = 0x1 << 1;
        public static byte ColF = 0x1 << 2;
        public static byte ColE = 0x1 << 3;
        public static byte ColD = 0x1 << 4;
        public static byte ColC = 0x1 << 5;
        public static byte ColB = 0x1 << 6;
        public static byte ColA = 0x1 << 7;

        public override event EventHandler BasePositionEvent;
        public override event EventHandler NewGamePositionEvent;
        public override event EventHandler<string> DataEvent;
        public override event EventHandler HelpRequestedEvent;
        public override event EventHandler<string> GameEndEvent;


        private readonly Dictionary<string, string> _codeToFen = new Dictionary<string, string>()
        {
            { "0", "." },
            { "1", "q" },
            { "2", "k" },
            { "3", "b" },
            { "4", "p" },
            { "5", "n" },
            { "6", "R" },
            { "7", "P" },
            { "8", "r" },
            { "9", "B" },
            { "A", "N" },
            { "B", "Q" },
            { "C", "K" }
        };

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

      
       

        private readonly ConcurrentQueue<string[]> _flashFields = new ConcurrentQueue<string[]>();
        private readonly ConcurrentQueue<ProbingMove[]> _probingFields = new ConcurrentQueue<ProbingMove[]>();
        private readonly EChessBoardConfiguration _boardConfiguration;
        private readonly ExtendedEChessBoardConfiguration _extendedBoardConfiguration;
        private bool _whiteKingOnBasePosition = false;
        private bool _blackKingOnBasePosition = false;
        private string _lastSendFenPosition = string.Empty;
        private string _boardColorOff = "0";
        private string _boardColorRed = "1";
        private string _boardColorGreen = "2";
        private string _boardColorBlue = "3";
        private ulong _repeatedTenthOfSeconds = 0;
        private int _debounce = 1;
        

        public EChessBoardMove(ILogging logger, EChessBoardConfiguration configuration)
        {
            _boardConfiguration = configuration;
            _useBluetooth = true;
            _showMoveLine = configuration.ShowMoveLine;
            _extendedBoardConfiguration = _boardConfiguration.ExtendedConfig.FirstOrDefault(c => c.IsCurrent) ?? new ExtendedEChessBoardConfiguration(false,true);
            _logger = logger;
            MultiColorLEDs = true;
            PieceRecognition = true;
            SelfMoving = _extendedBoardConfiguration.AutoMoveFigures;
            ValidForAnalyse = true;
            BatteryLevel = "---";
            BatteryStatus = "Full";
            _serialCommunication = new SerialCommunication(logger, configuration.PortName, _useBluetooth, Constants.ChessnutMove);
            Information = Constants.ChessnutMove;
            var thread = new Thread(FlashLeds) { IsBackground = true };
            thread.Start();
            var probingThread = new Thread(ShowProbingMoves) { IsBackground = true };
            probingThread.Start();
            _acceptProbingMoves = true;
        }

        private void FlashLeds()
        {
            var switchSide = false;
            while (!_release)
            {
                if (_extendedBoardConfiguration.SendLedCommands)
                {
                    if (_flashFields.TryPeek(out var fields))
                    {
                        var allFields = new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
                        
                        _serialCommunication.Send(GetByteArrayForLeds(allFields, 
                            GetColorCodeFromRGB(_extendedBoardConfiguration.RGBHelp)));
                    }
                }

                Thread.Sleep(500);
            }
        }

        private void ShowProbingMoves()
        {
            var switchSide = false;

            while (!_release)
            {
                if (_extendedBoardConfiguration.SendLedCommands )
                {
                    if (_probingFields.TryPeek(out var fields))
                    {
                        if (!_acceptProbingMoves)
                        {
                            _probingFields.TryDequeue(out _);
                            // SetAllLEDsOff(true);
                            continue;
                        }

                        var probingMove = fields.OrderByDescending(f => f.Score).First();
                       
                        if (_boardConfiguration.ShowPossibleMovesEval)
                        {
                            string currentBestMove = string.Empty;
                            decimal currentBestSore = decimal.Zero;
                            var badMoves = new HashSet<string>(fields.Where(p => p.Score <= -1)
                                .Select(p => p.FieldName)
                                .ToArray(), StringComparer.OrdinalIgnoreCase);
                            var playAbleMovesArray = fields.Where(p => p.Score <= 1 && p.Score >= -1)
                                .OrderByDescending(p => p.Score)
                                .Select(p => p.FieldName)
                                .ToArray();
                            if (playAbleMovesArray.Length > 0)
                            {
                                currentBestMove = playAbleMovesArray[0];
                                currentBestSore = fields.First(p => p.FieldName.Equals(currentBestMove)).Score;
                            }

                            var playAbleMoves =
                                new HashSet<string>(playAbleMovesArray, StringComparer.OrdinalIgnoreCase);
                            var goodMoves = new HashSet<string>(fields.Where(p => p.Score > 1)
                                .Select(p => p.FieldName)
                                .ToArray(), StringComparer.OrdinalIgnoreCase);
                            if (goodMoves.Count == 0 && currentBestSore>0)
                            {
                                goodMoves.Add(currentBestMove);
                            }
                            lock (_locker)
                            {
                                _serialCommunication.Send(GetByteArrayForLeds(badMoves, goodMoves, playAbleMoves));
                            }

                        }
                        else if (_boardConfiguration.ShowPossibleMoves)
                        {
                            var allFields = new HashSet<string>(fields.Select(p => p.FieldName), StringComparer.OrdinalIgnoreCase);
                            lock (_locker)
                            {
                                _serialCommunication.Send(GetByteArrayForLeds(allFields,
                                    GetColorCodeFromRGB(_extendedBoardConfiguration.RGBPossibleMoves)));
                            }
                        }

                        switchSide = !switchSide;
                        Thread.Sleep(500);
                        continue;
                    }

                }            
                Thread.Sleep(10);
            }
        }

        public EChessBoardMove(ILogging logger)
        {
            _logger = logger;
            BatteryLevel = "---";
            BatteryStatus = "Full";
            Information = Constants.ChessnutMove;
        }

        public override void Reset()
        {
            //
        }

        public override bool CheckComPort(string portName)
        {
            return true;
        }

        public override bool CheckComPort(string portName, string baud)
        {
            return true;
        }

        private void SetLedForFields(string[] fieldNames, string rgbCode,  bool addLEDs, string info)
        {
            if (!addLEDs)
            {
                SetAllLEDsOff(true);
            }
            var ledCodes = GetByteArrayForLeds(new HashSet<string>(fieldNames, StringComparer.OrdinalIgnoreCase),
                GetColorCodeFromRGB(rgbCode));
            lock (_locker)
            {
                _serialCommunication.Send(ledCodes);
            }
        }
        public override void AdditionalInformation(string information)
        {
            if (string.IsNullOrWhiteSpace(information) || !information.StartsWith("R:"))
            {
                return;
            }
            int gIndex =   information.IndexOf("G:");
            int bIndex =   information.IndexOf("B:");
            string rFields = information.Substring(2, gIndex-2).Trim();
            string gFields = information.Substring(gIndex+2,bIndex-gIndex-2).Trim();
            string bFields = information.Substring(bIndex+2).Trim();

            var redFields = new HashSet<string>();
            var greenFields = new HashSet<string>();
            var blueFields = new HashSet<string>();
            for (int i = 0; i < rFields.Length; i += 2)
            {
                redFields.Add(rFields.Substring(i, 2));
            }
            for (int i = 0; i < gFields.Length; i += 2)
            {
                greenFields.Add(gFields.Substring(i, 2));
            }
            for (int i = 0; i < bFields.Length; i += 2)
            {
                blueFields.Add(bFields.Substring(i, 2));
            }
            var ledCodes = GetByteArrayForLeds(redFields, greenFields, blueFields);
            lock (_locker)
            {
                _serialCommunication.Send(ledCodes);
            }
        }

        public override void SetLedForFields(SetLEDsParameter ledsParameter)
        {
            if (ledsParameter==null || !EnsureConnection())
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(ledsParameter.FenString))
            {
                SendFenToBoard(ledsParameter.FenString);
            }

            if (!_extendedBoardConfiguration.SendLedCommands)
            {
                return;
            }
            _logger?.LogDebug($"Set LEDs: {ledsParameter}");
            if (ledsParameter.IsProbing)
            {
                _logger?.LogDebug($"Is probing!");
            }
            if (ledsParameter.BookFieldNames.Length > 0)
            {
                var bookFieldNames = new List<string>(ledsParameter.BookFieldNames).ToArray();
                if (bookFieldNames.Length > 0)
                {
                    var ledCodes = GetByteArrayForLeds(new HashSet<string>(bookFieldNames, StringComparer.OrdinalIgnoreCase),
                        GetColorCodeFromRGB(_extendedBoardConfiguration.RGBBookMove));
                    _logger?.LogDebug($"SendFields for book moves : {string.Join(" ", bookFieldNames)}");
                    lock (_locker)
                    {
                        _serialCommunication.Send(ledCodes);
                    }

                }
                
                return;
            }

            var fieldNames = ledsParameter.FieldNames.Length > 0
                ? new List<string>(ledsParameter.FieldNames).ToArray()
                : new List<string>(ledsParameter.InvalidFieldNames).ToArray();
            if (fieldNames.Length == 0)
            {
                fieldNames = new List<string>(ledsParameter.BookFieldNames).ToArray();
                if (fieldNames.Length > 0)
                {
                    var ledCodes = GetByteArrayForLeds(new HashSet<string>(fieldNames, StringComparer.OrdinalIgnoreCase),
                        GetColorCodeFromRGB(_extendedBoardConfiguration.RGBBookMove));
                    _logger?.LogDebug($"SendFields : {string.Join(" ", fieldNames)}");
                    lock (_locker)
                    {
                        _serialCommunication.Send(ledCodes);
                    }

                }
                return;
            }

            var joinedString = string.Join(" ", fieldNames);
            _flashFields.TryDequeue(out _);
            if (ledsParameter.IsProbing &&
                (_boardConfiguration.ShowPossibleMoves || _boardConfiguration.ShowPossibleMovesEval))
            {
                _logger?.LogDebug($"B: set LEDs for probing {ledsParameter}");
                _probingFields.TryDequeue(out _);
                _probingFields.Enqueue(ledsParameter.ProbingMoves);
                return;
            }

            if (ledsParameter.IsThinking)
            {
                _prevLedField = _prevLedField == 1 ? 0 : 1;
            }
            else
            {
                if (_prevJoinedString.Equals(joinedString))
                {
                    return;
                }
            }

            _logger?.LogDebug($"B: set LEDs for {joinedString}");
            if (ledsParameter.IsThinking && fieldNames.Length > 1)
            {
                _logger?.LogDebug($"B: set LEDs as thinking");
                _flashFields.Enqueue(fieldNames);
                return;
            }

            _prevJoinedString = joinedString;
           
            var allFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            byte[] allCodes = null;
            
            if (ledsParameter.IsError)
            {
                _logger?.LogDebug($"B: set LEDs as error");
                foreach (var fieldName in fieldNames)
                {
                    allFields.Add(fieldName);
                }
                allCodes = GetByteArrayForLeds(allFields, GetColorCodeFromRGB(_extendedBoardConfiguration.RGBInvalid));
                _logger?.LogDebug($"SendFields : {string.Join(" ", fieldNames)}");
                lock (_locker)
                {
                    _serialCommunication.Send(allCodes);
                }
                return;
            }

       
            if (fieldNames.Length == 2)
            {
                if (_showMoveLine)
                {
                    var moveLine = MoveLineHelper.GetMoveLine(fieldNames[0], fieldNames[1]);
                    for (int i = 0; i < moveLine.Length-1; i++)
                    {
                        allFields.Add(moveLine[i]);
                    }
                    allCodes = GetByteArrayForLeds(allFields, moveLine[moveLine.Length-2],
                        GetColorCodeFromRGB(_extendedBoardConfiguration.RGBMoveFrom),
                        GetColorCodeFromRGB(_extendedBoardConfiguration.RGBMoveTo));
                }
                else
                {
                    allCodes = GetByteArrayForLeds(fieldNames[0], fieldNames[1],
                        GetColorCodeFromRGB(_extendedBoardConfiguration.RGBMoveFrom),
                        GetColorCodeFromRGB(_extendedBoardConfiguration.RGBMoveTo));
                }
            }
            else
            {
                foreach (var fieldName in fieldNames)
                {
                    allFields.Add(fieldName);
                }
                allCodes = GetByteArrayForLeds(allFields, GetColorCodeFromRGB(_extendedBoardConfiguration.RGBMoveFrom));
            }

            
            _logger?.LogDebug($"SendFields : {string.Join(" ", fieldNames)}");
            lock (_locker)
            {
                _serialCommunication.Send(allCodes);
            }
        }


        private string GetColorCodeFromRGB(string rgb)
        {
            if (string.IsNullOrEmpty(rgb) || rgb.Length != 3)
            {
                return _boardColorOff;
            }
            if (rgb[0].Equals('1'))
            {
                return _boardColorRed;
            }
            if (rgb[1].Equals('1'))
            {
                return _boardColorGreen;
            }
            if (rgb[2].Equals('1'))
            {
                return _boardColorBlue;
            }

            return _boardColorOff;
        }


        private byte[] GetByteArrayForLeds(HashSet<string> fieldNamesColorRed, HashSet<string> fieldNamesColorGreen, HashSet<string> fieldNamesColorBlue)
        {
            var allCodes = new List<byte>(_ledPrefix);
            var byteCode = string.Empty;
            foreach (var i in _fieldOrder)
            {
                var fieldName = Fields.GetFieldName(i);
                if (fieldNamesColorRed.Contains(fieldName))
                {
                    byteCode += _boardColorRed;
                }
                else if (fieldNamesColorGreen.Contains(fieldName))
                {
                    byteCode += _boardColorGreen;
                }
                else if (fieldNamesColorBlue.Contains(fieldName))
                {
                    byteCode += _boardColorBlue;
                }
                else
                {
                    byteCode += _boardColorOff;
                }
            }

            allCodes.AddRange(StringToByteArray(byteCode));
            return allCodes.ToArray();
        }

        private byte[] GetByteArrayForLeds(HashSet<string> fieldNames, string colorCode)
        {
            var allCodes = new List<byte>(_ledPrefix);
            var byteCode = string.Empty;
            foreach (var i in _fieldOrder)
            {
                if (fieldNames.Contains(Fields.GetFieldName(i)))
                {
                    byteCode += colorCode;
                }
                else
                {
                    byteCode += _boardColorOff;
                }
            }

            allCodes.AddRange(StringToByteArray(byteCode));
            return allCodes.ToArray();
        }

        private byte[] GetByteArrayForLeds(string fieldName0, string fieldName1, string colorCode0, string colorCode1)
        {
            var allCodes = new List<byte>(_ledPrefix);
            var byteCode = string.Empty;
            foreach (var i in _fieldOrder)
            {
                var fieldName = Fields.GetFieldName(i);
                if (fieldName0.Equals(fieldName))
                {
                    byteCode += colorCode0;
                }
                else if (fieldName1.Equals(fieldName))
                {
                    byteCode += colorCode1;
                }
                else
                {
                    byteCode += _boardColorOff;
                }
            }

            allCodes.AddRange(StringToByteArray(byteCode));
            return allCodes.ToArray();
        }

        private byte[] GetByteArrayForLeds(HashSet<string> fieldNames, string fieldName1, string colorCode0,
            string colorCode1)
        {
            var allCodes = new List<byte>(_ledPrefix);
            var byteCode = string.Empty;
            foreach (var i in _fieldOrder)
            {
                var fieldName = Fields.GetFieldName(i);
                if (fieldNames.Contains(fieldName))
                {
                    byteCode += colorCode0;
                }
                else if (fieldName1.Equals(fieldName))
                {
                    byteCode += colorCode1;
                }
                else
                {
                    byteCode += _boardColorOff;
                }
            }

            allCodes.AddRange(StringToByteArray(byteCode));
            return allCodes.ToArray();
        }

        public override void SetAllLEDsOff(bool forceOff)
        {
            if (!EnsureConnection())
            {
                return;
            }
            
            _probingFields.TryDequeue(out _);
            _flashFields.TryDequeue(out _);
            if (!_extendedBoardConfiguration.SendLedCommands)
            {
                return;
            }
            _logger?.LogDebug("Set all LEDs off");
            lock (_locker)
            {
                _serialCommunication.Send(_allLEDsOff);
            }
        }

        public override void SetAllLEDsOn()
        {
            if (!EnsureConnection())
            {
                return;
            }
            _probingFields.TryDequeue(out _);
            _flashFields.TryDequeue(out _);
            if (!_extendedBoardConfiguration.SendLedCommands)
            {
                return;
            }
            _logger?.LogDebug("Set all LEDs on");
            lock (_locker)
            {
                _serialCommunication.Send(_allLEDsOn);
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
            // ignore
        }

        public override void SetDebounce(int debounce)
        {
            _debounce = debounce <1 ? 1 : debounce;
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
            if (!EnsureConnection())
            {
                return;
            }
            Information = _serialCommunication.DeviceName;
            _serialCommunication.Send(_startReading);
        }

        public override void SendInformation(string message)
        {
            //
        }

     

        public override void RequestDump()
        {
            //
        }

        public override DataFromBoard GetPiecesFen()
        {
            if (!EnsureConnection())
            {
                return new DataFromBoard(string.Empty);
            }

            var readingOne = false;
            var reading243D = false;
            var result = string.Empty;
            var prevData = string.Empty;
            var reading243DCode = _useBluetooth ? "24" : "3D";
            var dataFromBoard = _serialCommunication.GetFromBoard();

            var allData = dataFromBoard.FromBoard.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < allData.Length; i++)
            {
                if (allData[i] == "01" && !readingOne)
                {
                    readingOne = true;
                    prevData = allData[i];
                    continue;
                }

                if (allData[i] == reading243DCode && readingOne && !reading243D)
                {
                    if (prevData == "01")
                    {
                        reading243D = true;
                        continue;
                    }

                    readingOne = false;
                    continue;
                }

                prevData = allData[i];
                if (readingOne && reading243D)
                {
                    var pLine = "";
                    if (allData.Length - i > 32)
                    {
                        readingOne = false;
                        reading243D = false;

                        for (var j = 28; j >= 0; j -= 4)
                        {
                            for (var k = 0; k < 4; k++)
                            {
                                var pCode = allData[i + j + k];
                                if (_codeToFen.ContainsKey(pCode[0].ToString()) &&
                                    _codeToFen.ContainsKey(pCode[1].ToString()))
                                {
                                    pLine = _codeToFen[pCode[0].ToString()] + _codeToFen[pCode[1].ToString()] + pLine;
                                }
                                else
                                {
                                    _logger.LogError($"Unknown code {pCode}");
                                }
                            }

                        }

                        i = i + 31;
                    }

                    if (string.IsNullOrWhiteSpace(pLine))
                    {
                        return new DataFromBoard(_lastResult, dataFromBoard.Repeated);
                    }
                    pLine = "s" + pLine;
                    if (pLine.StartsWith(
                            "sRNBKQBNRPPPPPPPP................................pppppppprnbkqbnr"))
                    {
                        _playWithWhite = false;

                    }

                    if (pLine.StartsWith(
                            "srnbqkbnrpppppppp................................PPPPPPPPRNBQKBNR"))
                    {
                        _playWithWhite = true;
                    }


                    result = GetPiecesFen(pLine, _playWithWhite);
                }
            }
            if (!result.Contains("k") || !result.Contains("K"))
            {
                return new DataFromBoard(result, dataFromBoard.Repeated);
            }
            if (!string.IsNullOrWhiteSpace(_lastResult) && !_lastResult.Equals(result))
            {

                var fastChessBoard = new FastChessBoard();
                fastChessBoard.Init(result, Array.Empty<string>());
                if (_whiteKingOnBasePosition)
                {
                    if (fastChessBoard.WhiteKingOnCastleMove())
                    {
                        return new DataFromBoard(
                            _lastResult.Contains(UnknownPieceCode) ? string.Empty : _lastResult,
                            dataFromBoard.Repeated);
                    }
                }

                if (_blackKingOnBasePosition)
                {
                    if (fastChessBoard.BlackKingOnCastleMove())
                    {
                        return new DataFromBoard(
                            _lastResult.Contains(UnknownPieceCode) ? string.Empty : _lastResult,
                            dataFromBoard.Repeated);
                    }
                }
                _whiteKingOnBasePosition = fastChessBoard.WhiteKingOnBasePosition();
                _blackKingOnBasePosition = fastChessBoard.BlackKingOnBasePosition();
            }
            if (result.Contains(UnknownPieceCode))
            {
                return new DataFromBoard(_lastResult, dataFromBoard.Repeated);
            }
            _lastResult = result;
            _logger?.LogDebug($"GetPiecesFen: {dataFromBoard.Repeated} times {_lastResult}");
            _repeatedTenthOfSeconds = dataFromBoard.Repeated / (ulong)(9 * _debounce);
            return new DataFromBoard(_lastResult, _repeatedTenthOfSeconds );
        }

        public override DataFromBoard GetDumpPiecesFen()
        {
            return GetPiecesFen();
        }

        private static string GetPiecesFen(string boardFen, bool playWithWhite)
        {
            var result = string.Empty;


            if (playWithWhite)
            {
                for (var i = 1; i < 58; i += 8)
                {
                    var substring = boardFen.Substring(i, 8);
                    result += GetFenLine(string.Concat(substring.Reverse()));
                }
            }
            else
            {
                for (var i = 57; i > 0; i -= 8)
                {
                    var substring = boardFen.Substring(i, 8);
                    result += GetFenLine(substring);
                }
            }

            return result.Substring(0, result.Length - 1);

        }

        private static string GetFenLine(string substring)
        {
            var result = string.Empty;
            var noFigureCounter = 0;
            for (var i = substring.Length - 1; i >= 0; i--)
            {
                var piecesFromCode = substring[i];
                if (piecesFromCode == '.')
                {
                    noFigureCounter++;
                    continue;
                }

                if (noFigureCounter > 0)
                {
                    result += noFigureCounter;
                }

                noFigureCounter = 0;
                result += piecesFromCode;
            }

            if (noFigureCounter > 0)
            {
                result += noFigureCounter;
            }

            return result + "/";
        }

        private void SendFenToBoard(string fenPosition)
        {
            _logger?.LogDebug($"Send Fen: {fenPosition}");
            if (!_extendedBoardConfiguration.AutoMoveFigures || (_lastSendFenPosition.Equals(fenPosition) && fenPosition.StartsWith(_lastResult)))
            {
                _logger?.LogDebug($"Set fen equal last send");
                return;
            }
            var fastChessBoard = new FastChessBoard();
            fastChessBoard.Init(fenPosition, Array.Empty<string>());
            var allCodes = new List<byte>(_commandPrefix);
            var byteCode = string.Empty;
            foreach (var i in _fieldOrder)
            {
                byteCode += _fenToCode[fastChessBoard.GetFigureOnField(i)];

            }

            allCodes.AddRange(StringToByteArray(byteCode));
            allCodes.Add(0);
            _lastSendFenPosition = fenPosition;
            _logger?.LogDebug($"Send fen to board: {fenPosition}");
            _serialCommunication.Send(allCodes.ToArray());
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    

        protected override void SetToNewGame()
        {
            _whiteKingOnBasePosition = true;
            _blackKingOnBasePosition = true;
            _probingFields.TryDequeue(out _);
            SendFenToBoard(FenCodes.WhiteBoardBasePosition);
        }


        public override void Release()
        {
            _release = true;
            _probingFields.TryDequeue(out _);
        }

        public override void SetFen(string fen)
        {
            SendFenToBoard(fen);
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
            //
        }

        public override void SetEngineColor(int color)
        {
            //
        }

        public override void SpeedLeds(int level)
        {
            //
        }
    }
}