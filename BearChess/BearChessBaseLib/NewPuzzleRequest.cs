using System;

namespace www.SoLaNoSoft.com.BearChessBase
{
    public class NewPuzzleRequest
    {
        public PuzzleSource PuzzleSource
        {
            get;
            set;
        }

        public bool Random
        {
            get;
            set;
        }

        public bool ByDate
        {
            get;
            set;
        }
        
        public DateTime? SelectedDate
        {
            get;
            set;
        }

        public NewPuzzleRequest()
        {
            SelectedDate = DateTime.MinValue;
        }        
    }
}