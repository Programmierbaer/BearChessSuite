using System;
using System.Collections.Generic;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;

namespace www.SoLaNoSoft.com.BearChessDatabase
{
    public interface IDatabase : IDisposable
    {
        string FileName { get; }
        void LoadDb(string fileName);
        bool Open();
        void CommitAndClose();
        void Close();
        void Compress();
        string Backup();
        string Restore(string backupFileName);
        void Drop();
    }

    public interface IGamesDatabase : IDatabase
    {
    
        int[] GetGamesIds();
        int Save(DatabaseGame databaseGame, bool updateGame, bool commitTransaction = true, int twicId = 0, string uuid = "");        
        void DeleteGame(int id);
        DatabaseGame LoadGame(int id, PgnConfiguration pgnConfiguration);
        DatabaseGameSimple[] GetGames(GamesFilter gamesFilter);
        DatabaseGameSimple[] GetGames(GamesFilter gamesFilter, string fen);
        DatabaseGameSimple[] GetGames(ulong[] keys, bool duplicatedByMoves);
        int GetTotalGamesCount();
        int SaveDuel(CurrentDuel engineDuel);
        void SaveDuelGamePair(int duelId, int gameId);
        void DeleteAllDuel();
        void DeleteDuelGames(int id);
        void UpdateDuel(int id, CurrentDuel duel);
        void UpdateDuel(CurrentDuel engineDuel, int id);
        void DeleteDuel(int id);
        void DeleteDuelGame(int gameId);
        DatabaseDuel[] LoadDuel();
        DatabaseDuel LoadDuel(int id);
        DatabaseDuel LoadDuelByGame(int gameId);
        int GetDuelGamesCount(int duelId);
        DatabaseGameSimple[] GetDuelGames(int duelId);
        bool IsDuelGame(int gameId);
        void SaveTournamentGamePair(int tournamentId, int gameId, int[] pairing);
        void UpdateTournament(int id, CurrentTournament tournament);
        int CloneTournament(int id);
        int SaveTournament(CurrentTournament tournament, int gamesToPlay, List<int[]> pairing);
        void DeleteAllTournament();
        void DeleteTournamentGamesWithPairing(int id, int pair);
        void DeleteTournamentGames(int id);
        void DeleteTournament(int id);
        DatabaseTournament[] LoadTournament();
        DatabaseTournament LoadTournament(int id);
        DatabaseTournament LoadTournamentByGame(int gameId);
        int GetTournamentGamesCount(int tournamentId);
        bool GameExists(string uuid);
        bool IsTournamentGame(int gameId);
        int GetLatestTournamentGameId(int tournamentId);
        DatabaseGameSimple[] GetTournamentGames(int tournamentId);
        void ResetPairing(int tournamentId, int pair);
        void ResetPairing(int tournamentId, int[] pairing);
        bool PairingExists(int tournamentId);
        int[] GetNextParing(int tournamentId);
        int SaveTWICImport(int twicNumber, string url, string fileName, int numberOfGames, long fileDate);
        DateTime? TWICFileImported(string url);
        DateTime? TWICFileImported(int twicNumber);
        TwicDownload[] GeTwicDownloads();
        int NumberOfTWICInGames(int twicId);
        int MaxTwicNumber();
        void SetNumberOfTWICGames(int twicId, int numberOfGames);
        void DeleteTWICGames(int twicId);
    }
}