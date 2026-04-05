using System;
using www.SoLaNoSoft.com.BearChessBase.Definitions;

namespace www.SoLaNoSoft.com.BearChessBase;

[Serializable]
public class RepertoireTrainingConfig
{
    public string DatabaseName { get; set; }
    public bool ShowNextMove { get; set; }
    public int NextMoveSeconds { get; set; }
    public bool ExecuteMoveAutomatically { get; set; }
    public int ExecuteForColor { get; set; }
    public bool AllowAllMoves  { get; set; }
    public bool ContinueAsNewGame  { get; set; }
    public bool ShowCurrentGame  { get; set; }

    public RepertoireTrainingConfig()
    {
        DatabaseName = Constants.RepertoireDefaultDBName;
        ShowNextMove = true;
        NextMoveSeconds = 3;
        ExecuteMoveAutomatically = false;
        ExecuteForColor = Fields.COLOR_WHITE;
        AllowAllMoves = true;
        ContinueAsNewGame = true;
        ShowCurrentGame = true;
    }
}