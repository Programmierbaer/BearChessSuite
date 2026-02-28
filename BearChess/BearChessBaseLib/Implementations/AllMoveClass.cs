using System.Collections.Generic;
using www.SoLaNoSoft.com.BearChessBase.Definitions;

namespace www.SoLaNoSoft.com.BearChessBase.Implementations
{
    public class AllMoveClass
    {
        public int MoveNumber { get; }
        private readonly Dictionary<int, Move> _moves = new Dictionary<int, Move>();
        private readonly Dictionary<int, string> _fens = new Dictionary<int, string>();

        public AllMoveClass(int moveNumber)
        {
            MoveNumber = moveNumber;
        }

        public void SetMove(int color, Move move, string fenPosition)
        {
            _moves[color] = (Move)move.Clone();
            _moves[color].Fen = fenPosition;
            _fens[color] = fenPosition;
        }

        public Move GetMove(int color)
        {
            return _moves.ContainsKey(color) ? _moves[color] : null;
        }

        public string GetFen(int color)
        {
            return _fens.TryGetValue(color, out var fen) ? fen : null;
        }

      
        public string GetMoveString(bool longFormat = true, DisplayCountryType countryType=DisplayCountryType.GB)
        {
            Move move;
            if (_moves.ContainsKey(Fields.COLOR_BLACK))
            {
                move = _moves[Fields.COLOR_BLACK];
            }
            else
            {
                move = _moves[Fields.COLOR_WHITE];
            }
            return move.GetMoveString();
        }
    }
}