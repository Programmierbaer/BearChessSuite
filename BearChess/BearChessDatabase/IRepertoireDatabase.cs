using www.SoLaNoSoft.com.BearChessBase.Implementations;

namespace www.SoLaNoSoft.com.BearChessDatabase;



public interface IRepertoireDatabase : IGamesDatabase
{
    
    RepertoireDatabaseGame GetNextGame(string fen);
}