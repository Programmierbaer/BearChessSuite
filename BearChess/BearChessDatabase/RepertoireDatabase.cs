using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChessDatabase;

public class RepertoireDatabase : Database, IRepertoireDatabase
{

    public static string[] GetDatabaseFileNames(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return [];
        }
        return Directory.GetFiles(path);
    }

    public RepertoireDatabase(ILogging logging, string fileName, Window ownerWindow) : base(ownerWindow, logging, fileName)
    {
    }
    
    public RepertoireDatabaseGame GetNextGame(string fen)
    {
        var games = FilterByFen(fen);
        if (games.Length == 0)
        {
            return null;
        }
        var gamesIndex = new Random().Next(games.Length);
        var game = games[gamesIndex];
        var dbGame = LoadGame(game.Id, Configuration.Instance.GetPgnConfiguration());
        var moveIndex = 0;
        var fenPos = fen.Split(" ".ToCharArray())[0];
        for (var i = 0; i < dbGame.AllMoves.Length; i++)
        {
            if (dbGame.AllMoves[i].Fen.StartsWith(fenPos))
            {
                moveIndex = i + 1;
                break;
            }
        }
        return moveIndex >= dbGame.AllMoves.Length ? null : new RepertoireDatabaseGame() { Game = dbGame, NextMoveIndex = moveIndex };
    }

    public RepertoireDatabaseGame[] GetRepertoireGames()
    {
        var pgnConfig = Configuration.Instance.GetPgnConfiguration();
        var games = GetGames(new GamesFilter(), string.Empty);
        var allGames = new List<RepertoireDatabaseGame>();
        for (var g = 0; g < games.Length; g++)
        {
            var game = games[g];
            var dbGame = LoadGame(game.Id, pgnConfig);

            allGames.Add(new RepertoireDatabaseGame() { Game = dbGame, NextMoveIndex = 0 });

        }

        return allGames.ToArray();
    }

    public RepertoireDatabaseGame[] GetNextGames(string fen)
    {
        var pgnConfig = Configuration.Instance.GetPgnConfiguration();
        var allGames = new List<RepertoireDatabaseGame>();
        var games = FilterByFen(fen);
        var fenPos = fen.Split(" ".ToCharArray())[0];
        for (var g = 0; g < games.Length; g++)
        {
            var game = games[g];
            var dbGame = LoadGame(game.Id, pgnConfig);
            var moveIndex = 0;
            for (var i = 0; i < dbGame.AllMoves.Length - 1; i++)
            {
                if (dbGame.AllMoves[i].Fen.StartsWith(fenPos))
                {
                    moveIndex = i + 1;
                    break;
                }
            }

            if (moveIndex < dbGame.AllMoves.Length)
            {
                allGames.Add(new RepertoireDatabaseGame() { Game = dbGame, NextMoveIndex = moveIndex });
            }
        }
        return allGames.ToArray();
    }

    public RepertoireDatabaseGame[] LoadByFen(string fen)
    {
        if (InError)
        {
            return [];
        }
        var pgnConfig = Configuration.Instance.GetPgnConfiguration();
        var fastBoard = new FastChessBoard();
        fastBoard.Init(fen, []);
        var allGames = new List<RepertoireDatabaseGame>();
        var tuples = new List<Tuple<int, int>>();
        _connection.Open();
        using (var cmd = new SQLiteCommand(
                   "SELECT fg.game_id, fg.moveNumber " +
                   " FROM fenToGames as fg " +
                   " JOIN fens as f ON (fg.fen_id = f.id)" +
                   " WHERE f.shortFen=@shortFen;", _connection))
        {
            cmd.Parameters.Add("@shortFen", DbType.String).Value = fastBoard.GetPositionHashCode();
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    tuples.Add(new Tuple<int, int>(rdr.GetInt32(0), rdr.GetInt32(1)));
                }
                rdr.Close();
            }
        }

        _connection.Close();
        foreach (var tuple in tuples)
        {
            var aGame = LoadGame(tuple.Item1, pgnConfig);
            allGames.Add(new RepertoireDatabaseGame() { Game = aGame, NextMoveIndex = tuple.Item2 });
        }

        return allGames.ToArray();
    }
}