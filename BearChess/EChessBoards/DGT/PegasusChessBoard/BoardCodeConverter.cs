using System;
using System.Collections.Generic;
using System.Linq;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase.Definitions;

namespace www.SoLaNoSoft.com.BearChess.PegasusChessBoard
{
    public class BoardCodeConverter
    {

        private readonly Dictionary<string, bool> _chessFields = new Dictionary<string, bool>()
        {
            { "A1", false },
            { "A2", false },
            { "A3", false },
            { "A4", false },
            { "A5", false },
            { "A6", false },
            { "A7", false },
            { "A8", false },
            { "B1", false },
            { "B2", false },
            { "B3", false },
            { "B4", false },
            { "B5", false },
            { "B6", false },
            { "B7", false },
            { "B8", false },
            { "C1", false },
            { "C2", false },
            { "C3", false },
            { "C4", false },
            { "C5", false },
            { "C6", false },
            { "C7", false },
            { "C8", false },
            { "D1", false },
            { "D2", false },
            { "D3", false },
            { "D4", false },
            { "D5", false },
            { "D6", false },
            { "D7", false },
            { "D8", false },
            { "E1", false },
            { "E2", false },
            { "E3", false },
            { "E4", false },
            { "E5", false },
            { "E6", false },
            { "E7", false },
            { "E8", false },
            { "F1", false },
            { "F2", false },
            { "F3", false },
            { "F4", false },
            { "F5", false },
            { "F6", false },
            { "F7", false },
            { "F8", false },
            { "G1", false },
            { "G2", false },
            { "G3", false },
            { "G4", false },
            { "G5", false },
            { "G6", false },
            { "G7", false },
            { "G8", false },
            { "H1", false },
            { "H2", false },
            { "H3", false },
            { "H4", false },
            { "H5", false },
            { "H6", false },
            { "H7", false },
            { "H8", false },
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
        public BoardCodeConverter()
        {

        }

        public BoardCodeConverter(string boardCodes, bool playWithWhite)
        {
            if (string.IsNullOrWhiteSpace(boardCodes))
            {
                return;
            }
            var strings = boardCodes.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
          
            for (byte i = 0; i < 64; i++)
            {

                var key = byte.Parse(strings[3 + i]);
                
                {
                    if (playWithWhite)
                    {
                        _chessFields[_fieldByte2FieldName[i]] = key == 1;
                    
                    }
                    else
                    {
                        _chessFields[_invertedFieldByte2FieldName[i]] = key == 1;
                    
                    }
                }

            }
         
        }

        public string[] GetFieldsWithPieces()
        {
            return _chessFields.Keys.Where(field => _chessFields[field]).ToArray();
        }



        public bool SamePosition(BoardCodeConverter boardCodeConverter)
        {
            return _chessFields.Keys.All(chessFieldsKey => _chessFields[chessFieldsKey] == boardCodeConverter.IsFigureOn(chessFieldsKey));
        }

        public void ClearFields()
        {
            foreach (var chessFieldsKey in _chessFields.Keys)
            {
                _chessFields[chessFieldsKey] = false;
            }
        }

        public void SetFigureOn(int fieldId)
        {
            var fieldName = Fields.GetFieldName(fieldId);
            if (!string.IsNullOrWhiteSpace(fieldName) && _chessFields.ContainsKey(fieldName))
            {
                _chessFields[fieldName] = true;
            }
        }


        public bool IsFigureOn(int fieldId)
        {
            var fieldName = Fields.GetFieldName(fieldId);
            return IsFigureOn(Fields.GetFieldName(fieldId));
        }

        public bool IsFigureOn(string fieldName)
        {
            if (!string.IsNullOrWhiteSpace(fieldName) && _chessFields.ContainsKey(fieldName))
            {
                return _chessFields[fieldName.ToUpper()];
            }

            return false;
        }

    }
}