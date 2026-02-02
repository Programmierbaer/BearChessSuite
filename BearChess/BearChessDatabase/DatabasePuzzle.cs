using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;

namespace www.SoLaNoSoft.com.BearChessDatabase
{

    public class DatabasePuzzle
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public PgnGame PgnGame { get; set; }        

        public bool IsSolved { get; set; }

        public int PlayCount { get; set; }

        public DatabasePuzzle()
        {

        }
    }
}