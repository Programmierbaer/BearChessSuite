using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;

namespace www.SoLaNoSoft.com.BearChessBase
{
    [Serializable]
    public class OpeningTrainingConfig
    {
        public string OpeningBookId { get; set; }
        public EcoCode EcoCode { get; set; }
        public bool ShowNextMove { get; set; }
        public int NextMoveSeconds { get; set; }
        public bool ExecuteMoveAutomatically { get; set; }
        public int ExecuteForColor { get; set; }
        public OpeningBook.VariationsEnum Variations { get; set; }
        public bool AllowChangeEcodeCode { get; set; }

        public OpeningTrainingConfig()
        {
            OpeningBookId = string.Empty;
            EcoCode = null;
            ShowNextMove = true;
            NextMoveSeconds = 3;
            ExecuteMoveAutomatically = false;
            ExecuteForColor = Fields.COLOR_WHITE;
            Variations = OpeningBook.VariationsEnum.Flexible;
            AllowChangeEcodeCode = false;
        }
    }
}
