using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;

namespace www.SoLaNoSoft.com.BearChessDatabase
{

    public class DatabasePuzzle
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public PgnGame PgnGame { get; set; }
        public string White => PgnGame?.PlayerWhite;
        public string Black => PgnGame?.PlayerBlack;
        public string FEN => PgnGame?.FENLine;
        
        public string GameEvent => PgnGame?.GameEvent;
        
        public string MoveList => PgnGame?.MoveList;
        public bool IsSolved { get; set; }

        public int PlayCount { get; set; }

        public DatabasePuzzle()
        {

        }
    }
}