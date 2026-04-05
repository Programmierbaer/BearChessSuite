namespace www.SoLaNoSoft.com.BearChessDatabase;

public interface IPuzzleDatabase : IDatabase
{
    
    DatabasePuzzle LoadPuzzle(int id);
    DatabasePuzzle LoadNextPuzzle(int id);
    DatabasePuzzle LoadRandomPuzzle();
    bool SavePuzzle(string label, string pgnEvent, string pgn, int playCount, bool commitTransaction = true);
    int GetTotalPuzzlesCount();
    int GetTotalPuzzlesCount(bool solved);
    void ResetToUnsolved();
    void UpdateSolved(int id, bool solved);
}