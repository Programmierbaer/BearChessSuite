﻿
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessTools;
using PgnCreator = www.SoLaNoSoft.com.BearChessBase.Implementations.pgn.PgnCreator;
using System.Runtime.Serialization.Formatters.Binary;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;
using System.Windows;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;

namespace www.SoLaNoSoft.com.BearChessDatabase
{
    public class Database : IDisposable
    {
        private readonly Window _owner;
        private readonly ILogging _logging;
        private readonly PgnConfiguration _pgnConfiguration;
        private SQLiteConnection _connection;
        private bool _dbExists;
        private bool _inError;
        private int _storageVersion;
        SQLiteTransaction _sqLiteTransaction;

        public Database(Window owner, ILogging logging, string fileName, PgnConfiguration pgnConfiguration)
        {
            _owner = owner;
            _logging = logging;
            _pgnConfiguration = pgnConfiguration;
            FileName = fileName;
        }

        public string FileName { get; private set; }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        private void LoadDb()
        {
            LoadDb(FileName);
        }

        public void LoadDb(string fileName)
        {
            FileName = fileName;
            if (string.IsNullOrWhiteSpace(FileName))
            {
                _logging?.LogError("Load with empty file name");
                _inError = true;
            }

            _dbExists = File.Exists(FileName);
            try
            {
                if (!_dbExists)
                {
                    SQLiteConnection.CreateFile(FileName);
                }

                _connection = new SQLiteConnection($"Data Source = {FileName}; Version = 3;");
                CreateTables();
                _inError = false;
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                _inError = true;
            }
        }

        public bool Open()
        {
            try
            {
                if (_connection == null)
                {
                    LoadDb();
                    if (_inError)
                    {
                        return false;
                    }
                }

                _connection.Open();
                _inError = false;
                return true;
            }
            catch (Exception ex)
            {
                _inError = true;
                _logging?.LogError(ex);
            }

            return false;
        }

        public void Close()
        {
            _connection.Close();
            _inError = false;
        }

        public void Compress()
        {
            if (_inError)
            {
                return;
            }

            try
            {
                _connection.Open();
                string sql = "VACUUM; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.ExecuteNonQuery();
                }

                _connection.Close();
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                _inError = true;
            }
        }

        public string Backup()
        {
            string target = string.Empty;
            try
            {
                target = $"{FileName}.bak_{DateTime.UtcNow.ToFileTime()}";
                Close();
                _connection = null;
                File.Copy(FileName, target);
                LoadDb();
                
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                LoadDb();
                target = $"Error: {ex.Message}";
            }

            return target;
        }

        public string Restore(string backupFileName)
        {
            try
            {
                if (File.Exists(backupFileName))
                {
                    Close();
                    _connection = null;
                    File.Copy(backupFileName, FileName, true);
                    LoadDb();
                    return string.Empty;
                }

                return $"File{Environment.NewLine}{backupFileName}{Environment.NewLine} not found";
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                LoadDb();
                return $"Error: {ex.Message}";
            }
        }

        public void Drop()
        {
            Close();
            try
            {
                _connection?.Dispose();
                File.Delete(FileName);
                SQLiteConnection.CreateFile(FileName);
                _connection = new SQLiteConnection($"Data Source = {FileName}; Version = 3;");
                _inError = false;
                CreateTables();
            }
            catch (Exception ex)
            {
                _inError = true;
                _logging?.LogError(ex);
            }
        }

        public bool CreateTables()
        {
            if (_inError || !Open())
            {
                return false;
            }

            try
            {
                _storageVersion = -1;
                if (!TableExists("storageVersion"))
                {
                    var sql = "CREATE TABLE storageVersion " +
                              "(version INTEGER NOT NULL);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                        command.CommandText = "INSERT INTO storageVersion (version) VALUES (1);";
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    _storageVersion = GetStorageVersion();
                }

                if (!TableExists("games"))
                {
                    var sql = "CREATE TABLE games " +
                              "(id INTEGER PRIMARY KEY," +
                              " white TEXT NOT NULL," +
                              " black TEXT NOT NULL," +
                              " event TEXT NOT NULL," +
                              " site TEXT NOT NULL," +
                              " result TEXT NOT NULL," +
                              " gameDate INTEGER NOT NULL, " +
                              " pgn TEXT NOT NULL," +
                              " pgnXML TEXT NOT NULL," +
                              " pgnHash INTEGER NOT NULL);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX idx_games_white ON games(white);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX idx_games_black ON games(black);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX idx_games_pgnHash ON games(pgnHash);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX idx_games_date ON games(gameDate);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }

                if (!TableExists("fens"))
                {
                    var sql = "CREATE TABLE fens " +
                              "(id INTEGER PRIMARY KEY," +
                              " shortFen TEXT NOT NULL," +
                              " fullFen TEXT NOT NULL);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX idx_fens_shortFen ON fens(shortFen);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }

                if (!TableExists("fenToGames"))
                {
                    var sql = "CREATE TABLE fenToGames " +
                              "(id INTEGER PRIMARY KEY," +
                              " fen_id INTEGER NOT NULL," +
                              " game_id INTEGER NOT NULL," +
                              " moveNumber INTEGER NOT NULL);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX idx_fenToGames_fens_id ON fenToGames(fen_id);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX idx_fenToGames_games_id ON fenToGames(game_id);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }

                if (!TableExists("tournament"))
                {
                    var sql = "CREATE TABLE tournament " +
                              "(id INTEGER PRIMARY KEY," +
                              " event TEXT NOT NULL," +
                              " eventDate INTEGER NOT NULL, " +
                              " gamesToPlay INTEGER NOT NULL," +
                              " configXML TEXT NOT NULL );";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!TableExists("tournamentGames"))
                {
                    var sql = "CREATE TABLE tournamentGames " +
                              "(id INTEGER PRIMARY KEY," +
                              " tournament_id INTEGER NOT NULL, " +
                              " game_id INTEGER NOT NULL);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX idx_tournamentGames_tournId ON tournamentGames(tournament_id);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX idx_tournamentGames_gameId ON tournamentGames(game_id);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }

                if (!TableExists("duel"))
                {
                    var sql = "CREATE TABLE duel " +
                              "(id INTEGER PRIMARY KEY," +
                              " event TEXT NOT NULL," +
                              " eventDate INTEGER NOT NULL, " +
                              " gamesToPlay INTEGER NOT NULL," +
                              " configXML TEXT NOT NULL );";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!TableExists("duelGames"))
                {
                    var sql = "CREATE TABLE duelGames " +
                              "(id INTEGER PRIMARY KEY," +
                              " duel_id INTEGER NOT NULL, " +
                              " game_id INTEGER NOT NULL);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX duelGames_duelId ON duelGames(duel_id);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        sql = "CREATE INDEX duelGames_gamesId ON duelGames(game_id);";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }

                if (_storageVersion <= 1)
                {
                    var sql = "ALTER TABLE games ADD COLUMN round INTEGER DEFAULT 1; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "ALTER TABLE games ADD COLUMN white_elo text DEFAULT ''; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "ALTER TABLE games ADD COLUMN black_elo text DEFAULT ''; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "ALTER TABLE games ADD COLUMN byteXML BLOB NOT NULL DEFAULT 0; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "ALTER TABLE games ADD COLUMN gameHash INTEGER NOT NULL DEFAULT 0; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "CREATE INDEX idx_games_gameHash ON games(gameHash);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "ALTER TABLE games ADD COLUMN twic_id INTEGER NOT NULL DEFAULT 0; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "CREATE INDEX idx_games_twic ON games(twic_id);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "CREATE TABLE twic " +
                          "(id INTEGER PRIMARY KEY," +
                          " twicNumber INTEGER NOT NULL,"+
                          " importedPGNFile TEXT NOT NULL," +
                          " importedUrl TEXT NOT NULL," +
                          " numberOfGames INTEGER NOT NULL DEFAULT 0, "+
                          " importDate INTEGER NOT NULL, "+
                          " fileDate INTEGER NOT NULL); ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    sql = "CREATE INDEX twic_importedUrl ON twic(importedUrl);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "CREATE INDEX twic_twicNumber ON twic(twicNumber);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "UPDATE storageVersion SET version=5; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                }
                if (_storageVersion == 2)
                {
                    var sql = "ALTER TABLE games ADD COLUMN white_elo text DEFAULT ''; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "ALTER TABLE games ADD COLUMN black_elo text DEFAULT ''; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "UPDATE storageVersion SET version=3; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                }
                if (_storageVersion == 3)
                {
                    Close();
                    var databaseGameSimples = GetGames(new GamesFilter() { FilterIsActive = false });
                    foreach (var databaseGameSimple in databaseGameSimples)
                    {
                        Save(LoadGame(databaseGameSimple.Id, _pgnConfiguration), true);
                    }

                    Open();
                    string sql = "UPDATE storageVersion SET version=4; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                }
                if (_storageVersion == 4)
                {
                    string target = $"{FileName}.bak_{DateTime.UtcNow.ToFileTime()}";
                    Close();
                    _connection = null;
                    File.Copy(FileName, target);
                    _connection = new SQLiteConnection($"Data Source = {FileName}; Version = 3;");
                    _connection.Open();

                    //" byteXML BLOB NOT NULL," +
                    string sql = "ALTER TABLE games ADD COLUMN byteXML BLOB NOT NULL DEFAULT 0; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "ALTER TABLE games ADD COLUMN gameHash INTEGER NOT NULL DEFAULT 0; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "CREATE INDEX idx_games_gameHash ON games(gameHash);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "ALTER TABLE games ADD COLUMN twic_id INTEGER NOT NULL DEFAULT 0; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "CREATE INDEX idx_games_twic ON games(twic_id);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "delete from fenToGames; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "delete from fens; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    Close();
                    var databaseGameSimples = GetGames(new GamesFilter() { FilterIsActive = false });
                    var infoWindow = new ProgressWindow
                    {
                        Owner = _owner
                    };
                    
                    infoWindow.SetTitle("Migrate database");
                    infoWindow.SetMaxValue(databaseGameSimples.Length);
                    _logging?.LogDebug($"Migrate database with {databaseGameSimples.Length} games");
                    infoWindow.Show();
                    int i = 0;
                    try
                    {
                        foreach (var databaseGameSimple in databaseGameSimples)
                        {
                            Save(LoadOldGame(databaseGameSimple.Id, _pgnConfiguration), true, true, 0);
                            i++;
                            if (i % 100 == 0)
                            {
                                infoWindow.SetCurrentValue(i, $"{i} of {databaseGameSimples.Length}");
                            }
                        }
                    }
                    finally
                    {
                        CommitAndClose();
                    }
                    infoWindow.Close();
                    _logging?.LogDebug("Database migrated");
                    infoWindow = new ProgressWindow
                    {
                        Owner = _owner
                    };

                    _connection.Open();
                    sql = "CREATE TABLE twic " +
                            "(id INTEGER PRIMARY KEY," +
                            " twicNumber INTEGER NOT NULL," +
                            " importedPGNFile TEXT NOT NULL," +
                            " importedUrl TEXT NOT NULL," +
                            " numberOfGames INTEGER NOT NULL DEFAULT 0, " +
                            " importDate INTEGER NOT NULL, " +
                            " fileDate INTEGER NOT NULL); ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    sql = "CREATE INDEX twic_importedUrl ON twic(importedUrl);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "CREATE INDEX twic_twicNumber ON twic(twicNumber);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    sql = "UPDATE storageVersion SET version=5; ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();

                    }
                    infoWindow.SetTitle("Reduce file size");
                    infoWindow.IsIndeterminate(true);
                    infoWindow.Show();
                    try
                    {
                        sql = "VACUUM; ";
                        using (var command = new SQLiteCommand(sql, _connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        //
                    }
                    infoWindow.Close();
                }

                _dbExists = true;
                Close();
                return true;
            }
            catch (Exception ex)
            {
                _inError = true;
                _logging?.LogError(ex);
            }

            return false;
        }

  

        private bool TableExists(string tableName)
        {
            var sql = $"SELECT name FROM sqlite_master WHERE type = 'table' AND name = '{tableName}' COLLATE NOCASE";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                var executeScalar = command.ExecuteScalar();
                return executeScalar != null;
            }
        }

        private int GetStorageVersion()
        {
            var sql = "SELECT version FROM storageVersion;";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                var executeScalar = Convert.ToInt32(command.ExecuteScalar());
                return executeScalar;
            }
        }

        #region Games

        public int[] GetGamesIds()
        {
            List<int> allIds = new List<int>();
            _connection.Open();
            using (var cmd = new SQLiteCommand("select id from games", _connection))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {

                        allIds.Add(rdr.GetInt32(0));

                    }
                }
            }

            _connection.Close();
            return allIds.ToArray();
        }

        public DatabaseGame LoadOldGame(int id, PgnConfiguration purePGN)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return null;
                }
            }

            DatabaseGame databaseGame = null;

            try
            {
                _connection.Open();
                var sql =
                    "SELECT id, white, black, event, site, result, gameDate, pgn, pgnXml, pgnHash, round FROM games WHERE id=@ID;";
                var xmlSerializer = new XmlSerializer(typeof(DatabaseGame));
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@ID", DbType.Int32).Value = id;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            using (TextReader reader = new StringReader(rdr.GetString(8)))
                            {
                                databaseGame = (DatabaseGame)xmlSerializer.Deserialize(reader);

                                databaseGame.Id = id;

                                var pgnCreator = databaseGame.CurrentGame == null ? new PgnCreator(purePGN) : new PgnCreator(databaseGame.CurrentGame.StartPosition, purePGN);
                                foreach (var databaseGameAllMove in databaseGame.AllMoves)
                                {
                                    pgnCreator.AddMove(databaseGameAllMove);
                                }

                                foreach (var move in pgnCreator.GetAllMoves())
                                {
                                    databaseGame.PgnGame.AddMove(move);
                                }
                            }
                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return databaseGame;
        }

        public int Save(DatabaseGame databaseGame, bool updateGame, bool commitTransaction = true, int twicId = 0)
        {
            if (_inError)
            {
                return -1;
            }

            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return -1;
                }
            }

            var gameId = databaseGame.Id;
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
                string pragma = "pragma journal_mode = memory;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
                pragma = "pragma synchronous = normal;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
                pragma = "pragma temp_store = memory;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
                pragma = "pragma mmap_size = 30000000000;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
            }
            if (_sqLiteTransaction == null)
            {
                _sqLiteTransaction = _connection.BeginTransaction(IsolationLevel.Serializable);
            }
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (var gameAllMove in databaseGame.AllMoves)
                {
                    if (gameAllMove == null)
                    {
                        continue;
                    }
                    sb.Append(gameAllMove.FromFieldName + gameAllMove.ToFieldName);
                }

                var deterministicHashCode = sb.ToString().CalculateHash();
                var deterministicGameHashCode = databaseGame.PgnGame.GetGame().CalculateHash();
                MemoryStream ms = new MemoryStream();
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, databaseGame);
                byte[] data = ms.ToArray();
                if (databaseGame.CurrentGame != null && (databaseGame.CurrentGame.RepeatedGame || updateGame))
                {

                    var sql = @"UPDATE games set event=@event, result=@result, gameDate=@gameDate, pgn=@pgn, pgnXML=@pgnXML, byteXML=@byteXML, pgnHash=@pgnHash, gameHash=@gameHash, twic_id=@twic_id 
                           WHERE id=@id; ";
                    using (var command2 = new SQLiteCommand(sql, _connection))
                    {
                        DateTime gameDate = databaseGame.GameDate;
                        if (gameDate.Equals(DateTime.MinValue))
                        {
                            if (!DateTime.TryParse(databaseGame.PgnGame.GameDate.Replace("??", "01"), out gameDate))
                            {
                                gameDate = DateTime.UtcNow;
                            }
                        }

                        command2.Parameters.Add("@event", DbType.String).Value = databaseGame.GameEvent;
                        command2.Parameters.Add("@result", DbType.String).Value = databaseGame.Result;
                        command2.Parameters.Add("@gameDate", DbType.Int64).Value = gameDate.ToFileTime();
                        command2.Parameters.Add("@pgn", DbType.String).Value = databaseGame.PgnGame.MoveList;
                        command2.Parameters.Add("@pgnXML", DbType.String).Value = "";//xmlResult;
                        command2.Parameters.Add("@byteXML", DbType.Object).Value = data;
                        command2.Parameters.Add("@pgnHash", DbType.UInt64).Value = deterministicHashCode;
                        command2.Parameters.Add("@gameHash", DbType.UInt64).Value = deterministicGameHashCode;
                        command2.Parameters.Add("@twic_id", DbType.Int32).Value = twicId;
                        command2.Parameters.Add("@id", DbType.Int32).Value = databaseGame.Id;

                        command2.ExecuteNonQuery();
                    }
                    sql = @"DELETE FROM fenToGames  WHERE game_id=@game_id; ";
                    using (var command2 = new SQLiteCommand(sql, _connection))
                    {
                        command2.Parameters.Add("@game_id", DbType.String).Value = databaseGame.Id;
                        command2.ExecuteNonQuery();
                    }

                }
                else
                {
                    var sql =
                        @"INSERT INTO games (white, black, event,site, result, gameDate, pgn, pgnXml,byteXML, pgnHash, gameHash, round, white_elo, black_elo, twic_id)
                           VALUES (@white, @black, @event, @site,  @result, @gameDate, @pgn, @pgnXml,@byteXML, @pgnHash, @gameHash, @round, @white_elo, @black_elo, @twic_id); ";
                    using (var command2 = new SQLiteCommand(sql, _connection))
                    {
                        command2.Parameters.Add("@white", DbType.String).Value = databaseGame.White;
                        command2.Parameters.Add("@black", DbType.String).Value = databaseGame.Black;
                        command2.Parameters.Add("@event", DbType.String).Value = databaseGame.PgnGame.GameEvent;
                        command2.Parameters.Add("@site", DbType.String).Value = databaseGame.PgnGame.GameSite;
                        command2.Parameters.Add("@result", DbType.String).Value = databaseGame.Result;
                        command2.Parameters.Add("@gameDate", DbType.Int64).Value = databaseGame.GameDate.ToFileTime();
                        command2.Parameters.Add("@pgn", DbType.String).Value = databaseGame.PgnGame.MoveList;
                        command2.Parameters.Add("@pgnXml", DbType.String).Value = ""; //xmlResult;
                        command2.Parameters.Add("@byteXML", DbType.Object).Value = data;
                        command2.Parameters.Add("@pgnHash", DbType.UInt64).Value = deterministicHashCode;
                        command2.Parameters.Add("@gameHash", DbType.UInt64).Value = deterministicGameHashCode;
                        command2.Parameters.Add("@round", DbType.Int32).Value = databaseGame.Round;
                        command2.Parameters.Add("@white_elo", DbType.String).Value = databaseGame.PgnGame.WhiteElo;
                        command2.Parameters.Add("@black_elo", DbType.String).Value = databaseGame.PgnGame.BlackElo;
                        command2.Parameters.Add("@twic_id", DbType.Int32).Value = twicId;
                        command2.ExecuteNonQuery();
                    }


                    using (var command2 = new SQLiteCommand("SELECT LAST_INSERT_ROWID();", _connection))
                    {
                        var executeScalar = command2.ExecuteScalar();
                        gameId = int.Parse(executeScalar.ToString());
                    }
                }
                var moveNumber = 1;
                var fastBoard = new FastChessBoard();
             
                foreach (var move in databaseGame.MoveList)
                {
                    if (move == null)
                    {
                        continue;
                    }
                    fastBoard.SetMove($"{move.FromFieldName}{move.ToFieldName}".ToLower());
                    string shortFen = fastBoard.GetPositionHashCode();
                    // chessBoard.MakeMove(move);
                    //var fen = chessBoard.GetFenPosition();
                    var fenId = 0;
                    //var shortFen = fen.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    var sql = "SELECT id FROM fens WHERE shortFen=@shortFen;";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.Parameters.Add("@shortFen", DbType.String).Value = shortFen;
                        var executeScalar = command.ExecuteScalar(CommandBehavior.SingleRow);
                        if (executeScalar == null)
                        {
                            sql = @"INSERT INTO fens (shortFen, fullFen) VALUES (@shortFen, @fullFen); ";
                            using (var command2 = new SQLiteCommand(sql, _connection))
                            {
                                command2.Parameters.Add("@shortFen", DbType.String).Value = shortFen;
                                command2.Parameters.Add("@fullFen", DbType.String).Value = "";
                                command2.ExecuteNonQuery();
                            }

                            using (var command2 = new SQLiteCommand("SELECT LAST_INSERT_ROWID();", _connection))
                            {
                                executeScalar = command2.ExecuteScalar();
                                fenId = int.Parse(executeScalar.ToString());
                            }
                        }
                        else
                        {
                            fenId = int.Parse(executeScalar.ToString());
                        }
                    }

                    sql =
                        @"INSERT INTO fenToGames (fen_id, game_id, moveNumber) VALUES (@fen_id, @game_id, @moveNumber); ";
                    using (var command2 = new SQLiteCommand(sql, _connection))
                    {
                        moveNumber++;
                        command2.Parameters.Add("@fen_id", DbType.Int32).Value = fenId;
                        command2.Parameters.Add("@game_id", DbType.Int32).Value = gameId;
                        command2.Parameters.Add("@moveNumber", DbType.Int32).Value = moveNumber / 2;
                        command2.ExecuteNonQuery();
                    }
                }

                if (commitTransaction)
                {
                    _sqLiteTransaction.Commit();
                    _sqLiteTransaction = null;
                }
            }
            catch (Exception ex)
            {
                _sqLiteTransaction.Rollback();
                _sqLiteTransaction = null;
                _logging?.LogError(ex);
                return -1;
            }
            finally
            {
                if (commitTransaction)
                {
                    _connection.Close();
                }
            }

            return gameId;
        }

        public void CommitAndClose()
        {
            if (_sqLiteTransaction != null)
            {
                _sqLiteTransaction.Commit();
                _connection.Close();
                _sqLiteTransaction = null;
            }
        }


        public void DeleteGame(int id)
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
                string pragma = "pragma journal_mode = memory;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
                pragma = "pragma synchronous = normal;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
                pragma = "pragma temp_store = memory;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
                pragma = "pragma mmap_size = 30000000000;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
            }
            var sqLiteTransaction = _connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var sql = @"DELETE FROM fenToGames WHERE game_id=@game_id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@game_id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM games WHERE id=@id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM tournamentGames WHERE game_id=@id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM duelGames WHERE game_id=@id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM fens WHERE id NOT IN (SELECT fen_id FROM fenToGames); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.ExecuteNonQuery();
                }

                sqLiteTransaction.Commit();
            }
            catch (Exception ex)
            {
                sqLiteTransaction.Rollback();
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public DatabaseGame LoadGame(int id, PgnConfiguration pgnConfiguration)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return null;
                }
            }

            DatabaseGame databaseGame = null;
           
            try
            {
                _connection.Open();
                var sql = "SELECT id, white, black, event, site, result, gameDate, pgn, pgnXml, byteXML, pgnHash, round FROM games WHERE id=@ID;";
                var xmlSerializer = new XmlSerializer(typeof(DatabaseGame));
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@ID", DbType.Int32).Value = id;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            if (_storageVersion < 5)
                            {
                                using (TextReader reader = new StringReader(rdr.GetString(8)))
                                {
                                    databaseGame = (DatabaseGame)xmlSerializer.Deserialize(reader);

                                    databaseGame.Id = id;

                                    var pgnCreator = databaseGame.CurrentGame == null ? new PgnCreator(pgnConfiguration) : new PgnCreator(databaseGame.CurrentGame.StartPosition, pgnConfiguration);
                                    foreach (var databaseGameAllMove in databaseGame.AllMoves)
                                    {
                                        pgnCreator.AddMove(databaseGameAllMove);
                                    }

                                    foreach (var move in pgnCreator.GetAllMoves())
                                    {
                                        databaseGame.PgnGame.AddMove(move);
                                    }
                                }
                            }
                            else
                            {
                                BinaryFormatter bf = new BinaryFormatter();
                                using (MemoryStream memoryStream = new MemoryStream(rdr.GetValue(9) as byte[]))
                                {
                                    databaseGame = bf.Deserialize(memoryStream) as DatabaseGame;
                                    databaseGame.Id = id;

                                    var pgnCreator = databaseGame.CurrentGame == null ? new PgnCreator(pgnConfiguration) : new PgnCreator(databaseGame.CurrentGame.StartPosition, pgnConfiguration);
                                    foreach (var databaseGameAllMove in databaseGame.AllMoves)
                                    {
                                        pgnCreator.AddMove(databaseGameAllMove);
                                    }

                                    foreach (var move in pgnCreator.GetAllMoves())
                                    {
                                        databaseGame.PgnGame.AddMove(move);
                                    }
                                }

                            }

                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return databaseGame;
        }

        public DatabaseGameSimple[] GetGames(GamesFilter gamesFilter)
        {
            return GetGames(gamesFilter, string.Empty);
        }

        private DatabaseGameSimple[] FilterByFen(string fen)
        {
            if (_inError)
            {
                return Array.Empty<DatabaseGameSimple>();
            }

            if (fen.StartsWith("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"))
            {
                // g.id, g.white, g.black, g.event, g.site, g.result, g.gameDate, g.pgn, g.pgnXml, g.pgnHash, g.round, g.white_elo, g.black_elo
                return GetBySql(
                    "SELECT id, white, black, event, site, result, gameDate, pgn, pgnXml, pgnHash, round, white_elo, black_elo, gameHash FROM games ORDER BY ID;");
            }
            var fastBoard = new FastChessBoard();
            fastBoard.Init(fen, Array.Empty<string>());
            DatabaseGameSimple[] allGames = null;
            _connection.Open();
            using (var cmd = new SQLiteCommand(
                "SELECT g.id, g.white, g.black, g.event, g.site, g.result, g.gameDate, g.pgn, g.pgnXml, g.pgnHash, g.round, g.white_elo, g.black_elo, g.gameHash" +
                " FROM games as g JOIN fenToGames as fg ON (g.ID=fg.game_id) " +
                " JOIN fens as f ON (fg.fen_id = f.id)" +
                "WHERE f.shortFen=@shortFen;", _connection))
            {
               // cmd.Parameters.Add("@shortFen", DbType.String).Value = fen.Split(" ".ToCharArray())[0];
                cmd.Parameters.Add("@shortFen", DbType.String).Value = fastBoard.GetPositionHashCode();
                using (var rdr = cmd.ExecuteReader())
                {
                    allGames = GetByReader(rdr);
                    rdr.Close();
                }
            }

            _connection.Close();
            return allGames;
        }

        public DatabaseGameSimple[] GetGames(GamesFilter gamesFilter, string fen)
        {
            if (_inError)
            {
                return Array.Empty<DatabaseGameSimple>();
            }

            try
            {
                if (!_dbExists)
                {
                    LoadDb();
                    if (!CreateTables())
                    {
                        return Array.Empty<DatabaseGameSimple>();
                    }
                }

                if (!gamesFilter.FilterIsActive)
                {
                    if (string.IsNullOrWhiteSpace(fen))
                    {
                        return GetBySql(
                            "SELECT id, white, black, event, site, result, gameDate, pgn, pgnXml, pgnHash, round, white_elo, black_elo, gameHash FROM games ORDER BY ID;");
                    }

                    return FilterByFen(fen);
                }

                string filterSQl = string.Empty;
                if (gamesFilter.NoDuelGames)
                {
                    filterSQl += " AND g.Id NOT IN (SELECT game_id FROM  duelGames) ";
                }
                if (gamesFilter.NoTournamentGames)
                {
                    filterSQl += " AND g.Id NOT IN (SELECT game_id FROM  tournamentGames) ";
                }
                if (gamesFilter.WhitePlayerWhatever)
                {
                    filterSQl += " AND (g.white LIKE @white OR g.black LIKE @white) ";
                }
                else
                {
                    filterSQl += " AND (g.white LIKE @white )";
                }
                if (gamesFilter.BlackPlayerWhatever)
                {
                    filterSQl += " AND (g.black LIKE @black OR g.white LIKE @black) ";
                }
                else
                {
                    filterSQl += " AND (g.black LIKE @black )";
                }

                string fenSQl = string.Empty;
                if (!string.IsNullOrWhiteSpace(fen))
                {
                    fenSQl = " JOIN fenToGames as fg ON (g.ID=fg.game_id) " +
                             " JOIN fens as f ON (fg.fen_id = f.id)";
                    filterSQl += " AND (f.shortFen=@shortFen) ";
                }
                DatabaseGameSimple[] allGames = null;
                _connection.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT g.id, g.white, g.black, g.event, g.site, g.result, g.gameDate, g.pgn, g.pgnXml, g.pgnHash, g.round, g.white_elo, g.black_elo, g.gameHash " +
                    " FROM games g  " +
                    fenSQl+
                    " WHERE g.Event LIKE @gameEvent" +
                    " AND (gameDate BETWEEN @fromDate AND @toDate) " +
                    filterSQl +
                    " ORDER BY g.ID;", _connection))
                {
                    cmd.Parameters.Add("@gameEvent", DbType.String).Value = string.IsNullOrWhiteSpace(gamesFilter.GameEvent) ? "%" : gamesFilter.GameEvent.Replace("*","%");
                    cmd.Parameters.Add("@fromDate", DbType.Int64).Value = gamesFilter.FromDate?.ToFileTime() ?? 0;
                    cmd.Parameters.Add("@toDate", DbType.Int64).Value = gamesFilter.ToDate?.ToFileTime() ?? long.MaxValue;

                    cmd.Parameters.Add("@black", DbType.String).Value = string.IsNullOrWhiteSpace(gamesFilter.BlackPlayer) ? "%" : gamesFilter.BlackPlayer.Replace("*", "%");
                    cmd.Parameters.Add("@white", DbType.String).Value = string.IsNullOrWhiteSpace(gamesFilter.WhitePlayer) ? "%" : gamesFilter.WhitePlayer.Replace("*", "%");
                    if (!string.IsNullOrWhiteSpace(fen))
                    {
                        cmd.Parameters.Add("@shortFen", DbType.String).Value = fen.Split(" ".ToCharArray())[0];
                    }

                    using (var rdr = cmd.ExecuteReader())
                    {
                        allGames = GetByReader(rdr);
                        rdr.Close();
                    }
                }

                _connection.Close();
                return allGames;
            }
            catch (Exception ex)
            {
                _inError = true;
                _logging?.LogError(ex);
            }

            return Array.Empty<DatabaseGameSimple>();
        }

        public int GetTotalGamesCount()
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return 0;
                }
            }

            try
            {
                _connection.Open();
                var sql = "SELECT COUNT(*) FROM games;";
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    var executeScalar = cmd.ExecuteScalar();
                    return int.Parse(executeScalar.ToString());
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                return 0;
            }
            finally
            {
                _connection.Close();
            }
        }

        private DatabaseGameSimple[] GetByReader(SQLiteDataReader rdr)
        {

            var allGames = new List<DatabaseGameSimple>(10000);
            while (rdr.Read())
            {
                var pgnHash = rdr.GetInt64(9);
                var gameHash = rdr.GetInt64(13);
                var databaseGameSimple = new DatabaseGameSimple
                                         {
                                             Id = rdr.GetInt32(0),
                                             White = rdr.GetString(1),
                                             Black = rdr.GetString(2),
                                             GameEvent = rdr.GetString(3),
                                             GameSite = rdr.GetString(4),
                                             Result = rdr.GetString(5),
                                             GameDate = DateTime.FromFileTime(rdr.GetInt64(6)),
                                             MoveList = rdr.GetString(7),
                                             Round = rdr.GetInt32(10).ToString(),
                                             WhiteElo = rdr.GetString(11),
                                             BlackElo = rdr.GetString(12),
                                             PgnHash = (ulong)pgnHash,
                                             GameHash = (ulong)gameHash
                                         };
                allGames.Add(databaseGameSimple);
            }

            return allGames.ToArray();
        }

   

        private DatabaseGameSimple[] GetBySql(string sql)
        {
            DatabaseGameSimple[] allGames = null;
            _connection.Open();
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    allGames = GetByReader(rdr);
                    rdr.Close();
                }
            }

            _connection.Close();
            return allGames;
        }

        public DatabaseGameSimple[] GetGames(ulong[] keys, bool duplicatedByMoves)
        {
            List<DatabaseGameSimple> allGames = new List<DatabaseGameSimple>();
            foreach (var key in keys)
            {
                DatabaseGameSimple[] games = GetDatabaseGameSimple(key, duplicatedByMoves);
                allGames.AddRange(games);
            }
            return allGames.ToArray();
        }

        private DatabaseGameSimple[] GetDatabaseGameSimple(ulong id, bool duplicatedByMoves)
        {
            if (_inError)
            {
                return null;
            }

            string column = duplicatedByMoves ? "pgnHash" : "gameHash";
            try
            {
               return GetBySql(
                        "SELECT id, white, black, event, site, result, gameDate, pgn, pgnXml, pgnHash, round, white_elo, black_elo, gameHash FROM games WHERE "+column+"=" + id + " ORDER BY ID;");

            }
            catch (Exception ex)
            {
                _inError = true;
                _logging?.LogError(ex);
            }

            return null;
        }
        #endregion

        #region Duel
        public int SaveDuel(CurrentDuel engineDuel)
        {
            if (_inError)
            {
                return -1;
            }

            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return -1;
                }
            }

            var tournamentId = 0;
            _connection.Open();

            try
            {

                var aSerializer = new XmlSerializer(typeof(CurrentDuel));
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                aSerializer.Serialize(sw, engineDuel);
                var xmlResult = sw.GetStringBuilder().ToString();
                var sql = @"INSERT INTO duel (event, eventDate, gamesToPlay, configXML)
                           VALUES (@event, @eventDate, @gamesToPlay, @configXML); ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
                    command2.Parameters.Add("@event", DbType.String).Value = engineDuel.GameEvent;
                    command2.Parameters.Add("@eventDate", DbType.Int64).Value = DateTime.UtcNow.ToFileTime();
                    command2.Parameters.Add("@gamesToPlay", DbType.Int32).Value = engineDuel.Cycles;
                    command2.Parameters.Add("@configXML", DbType.String).Value = xmlResult;
                    command2.ExecuteNonQuery();
                }

                using (var command2 = new SQLiteCommand("SELECT LAST_INSERT_ROWID();", _connection))
                {
                    var executeScalar = command2.ExecuteScalar();
                    tournamentId = int.Parse(executeScalar.ToString());
                }
            }
            catch (Exception ex)
            {

                _logging?.LogError(ex);
                return -1;
            }
            finally
            {
                _connection.Close();
            }

            return tournamentId;
        }

        public void SaveDuelGamePair(int duelId, int gameId)
        {
            if (_inError)
            {
                return;
            }

            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return;
                }
            }

            _connection.Open();
            try
            {
                var sql = @"INSERT INTO duelGames (duel_id, game_id) VALUES (@duel_id, @game_id); ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
                    command2.Parameters.Add("@duel_id", DbType.Int32).Value = duelId;
                    command2.Parameters.Add("@game_id", DbType.Int32).Value = gameId;
                    command2.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }
        
        public void DeleteAllDuel()
        {

            _connection.Open();
            var sqLiteTransaction = _connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                string sql;
                sql = @"DELETE FROM fenToGames WHERE game_id IN (select game_id FROM duelGames); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {

                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM games WHERE id IN (select game_id FROM duelGames); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {

                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM duelGames; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM duel; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {

                    command.ExecuteNonQuery();
                }

                sqLiteTransaction.Commit();
            }
            catch (Exception ex)
            {
                sqLiteTransaction.Rollback();
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public void DeleteDuelGames(int id)
        {
            try
            {
                _connection.Open();
                string sql;

                sql = @"DELETE FROM fenToGames WHERE game_id IN (select game_id FROM duelGames  WHERE duel_id=@id); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM games WHERE id IN (select game_id FROM duelGames  WHERE duel_id=@id); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM duelGames  WHERE duel_id=@id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }



            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public void UpdateDuel(int id, CurrentDuel duel)
        {
            if (_inError)
            {
                return;
            }

            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return;
                }
            }
            _connection.Open();

            try
            {

                var aSerializer = new XmlSerializer(typeof(CurrentDuel));
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                aSerializer.Serialize(sw, duel);
                var xmlResult = sw.GetStringBuilder().ToString();
                var sql = @"UPDATE duel SET event=@event, configXML=@configXML
                           WHERE id=@id; ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
                    command2.Parameters.Add("@event", DbType.String).Value = duel.GameEvent;
                    command2.Parameters.Add("@configXML", DbType.String).Value = xmlResult;
                    command2.Parameters.Add("@id", DbType.Int32).Value = id;
                    command2.ExecuteNonQuery();
                }


            }
            catch (Exception ex)
            {

                _logging?.LogError(ex);

            }
            finally
            {
                _connection.Close();
            }

        }

        public void DeleteDuel(int id)
        {
            try
            {
                _connection.Open();
                string sql;

                sql = @"DELETE FROM fenToGames WHERE game_id IN (select game_id FROM duelGames  WHERE duel_id=@id); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM games WHERE id IN (select game_id FROM duelGames  WHERE duel_id=@id); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM duelGames  WHERE duel_id=@id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM duel  WHERE id=@id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public void DeleteDuelGame(int gameId)
        {
            try
            {
                _connection.Open();
                string sql;

                sql = @"DELETE FROM fenToGames WHERE game_id = @gameId; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@gameId", DbType.Int32).Value = gameId;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM games WHERE id = @gameId; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@gameId", DbType.Int32).Value = gameId;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM duelGames  WHERE game_id=@gameId; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@gameId", DbType.Int32).Value = gameId;
                    command.ExecuteNonQuery();
                }



            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }
        public DatabaseDuel[] LoadDuel()
        {
            var allDuels = new List<DatabaseDuel>();
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return allDuels.ToArray();
                }
            }

            try
            {
                _connection.Open();
                var sql = "SELECT t.id, t.configXML, t.gamesToPlay, t.eventDate, count(g.id) as playedGames " +
                          "FROM duel as T left join duelGames as g on (t.id=g.duel_id) " +
                          "group by t.configXML,t.gamesToPlay,t.eventDate " +
                          "ORDER BY t.id;";
                var xmlSerializer = new XmlSerializer(typeof(CurrentDuel));
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            CurrentDuel currentDuel;
                            using (TextReader reader = new StringReader(rdr.GetString(1)))
                            {
                                currentDuel = (CurrentDuel)xmlSerializer.Deserialize(reader);
                            }

                            allDuels.Add(new DatabaseDuel()
                                         {
                                             DuelId = rdr.GetInt32(0),
                                             CurrentDuel = currentDuel,
                                             GamesToPlay = rdr.GetInt32(2),
                                             PlayedGames = rdr.GetInt32(4),
                                             State = rdr.GetInt32(2) == rdr.GetInt32(4) ? "Finished" : "Running",
                                             EventDate = DateTime.FromFileTime(rdr.GetInt64(3)),
                                             Participants = string.Join(", ", currentDuel.Players.Select(c => c.Name))
                                         });
                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return allDuels.ToArray();
        }

        public DatabaseDuel LoadDuel(int id)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return null;
                }
            }

            DatabaseDuel dbDuel = null;
            try
            {
                _connection.Open();
                var sql = "SELECT t.Id, t.configXML, t.gamesToPlay, t.eventDate, count(g.id) as playedGames " +
                          "FROM duel as T left join duelGames as g on (t.id=g.duel_id) " +
                          "WHERE t.id=@ID " +
                          "group by t.configXML,t.gamesToPlay,t.eventDate " +
                          "ORDER BY t.id;";
                var xmlSerializer = new XmlSerializer(typeof(CurrentDuel));
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@ID", DbType.Int32).Value = id;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            CurrentDuel currentDuel = null;
                            using (TextReader reader = new StringReader(rdr.GetString(1)))
                            {
                                currentDuel = (CurrentDuel) xmlSerializer.Deserialize(reader);
                            }

                            dbDuel = new DatabaseDuel()
                                     {
                                         DuelId = rdr.GetInt32(0),
                                         CurrentDuel = currentDuel,
                                         GamesToPlay = rdr.GetInt32(2),
                                         PlayedGames = rdr.GetInt32(4),
                                         State = rdr.GetInt32(2) == rdr.GetInt32(4) ? "Finished" : "Running",
                                         EventDate = DateTime.FromFileTime(rdr.GetInt64(3)),
                                         Participants = string.Join(", ", currentDuel.Players.Select(c => c.Name))
                                     };
                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return dbDuel;
        }

        public DatabaseDuel LoadDuelByGame(int gameId)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return null;
                }
            }

            DatabaseDuel dbDuel = null;


            int id = 0;
            try
            {

                _connection.Open();
                var sql = "SELECT duel_id FROM duelGames WHERE game_id = @game_Id";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@game_Id", DbType.Int32).Value = gameId;

                    var executeScalar = command.ExecuteScalar();
                    id = int.Parse(executeScalar.ToString());

                }

                sql = "SELECT t.Id, t.configXML, t.gamesToPlay, t.eventDate, count(g.id) as playedGames " +
                      "FROM duel as T left join duelGames as g on (t.id=g.duel_id) " +
                      "WHERE t.id=@ID " +
                      "group by t.configXML,t.gamesToPlay,t.eventDate " +
                      "ORDER BY t.id;";
                var xmlSerializer = new XmlSerializer(typeof(CurrentDuel));
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@ID", DbType.Int32).Value = id;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            CurrentDuel currentDuel = null;
                            using (TextReader reader = new StringReader(rdr.GetString(1)))
                            {
                                currentDuel = (CurrentDuel)xmlSerializer.Deserialize(reader);
                            }

                            dbDuel = new DatabaseDuel()
                                     {
                                         DuelId = rdr.GetInt32(0),
                                         CurrentDuel = currentDuel,
                                         GamesToPlay = rdr.GetInt32(2),
                                         PlayedGames = rdr.GetInt32(4),
                                         State = rdr.GetInt32(2) == rdr.GetInt32(4) ? "Finished" : "Running",
                                         EventDate = DateTime.FromFileTime(rdr.GetInt64(3)),
                                         Participants = string.Join(", ", currentDuel.Players.Select(c => c.Name))
                                     };
                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return dbDuel;
        }

        public int GetDuelGamesCount(int duelId)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return 0;
                }
            }

            int gamesCount = 0;
            try
            {
                _connection.Open();
                var sql = "SELECT COUNT(*) FROM duelGames WHERE duel_id=@duel_id;";

                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@duel_id", DbType.Int32).Value = duelId;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            gamesCount = rdr.GetInt32(0);
                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return gamesCount;
        }

        public DatabaseGameSimple[] GetDuelGames(int duelId)
        {

            if (_inError)
            {
                return Array.Empty<DatabaseGameSimple>();
            }

            DatabaseGameSimple[] allGames = null;
            _connection.Open();
            using (var cmd = new SQLiteCommand(
                "SELECT g.id, g.white, g.black, g.event, g.site, g.result, g.gameDate, g.pgn, g.pgnXml, g.pgnHash, g.round, g.white_elo, g.black_elo,  g.gameHash" +
                " FROM games as g  " +
                " JOIN duelGames t ON (t.game_id=g.id)" +
                "WHERE t.duel_id=@duelId;", _connection))
            {
                cmd.Parameters.Add("@duelId", DbType.Int32).Value = duelId;
                using (var rdr = cmd.ExecuteReader())
                {
                    allGames = GetByReader(rdr);
                    rdr.Close();
                }
            }

            _connection.Close();
            return allGames;
        }

        public bool IsDuelGame(int gameId)
        {
            if (_inError)
            {
                return true;
            }

            bool isDuelGame = false;
            _connection.Open();
            var sql = "SELECT duel_id FROM duelGames WHERE game_id = @game_Id";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.Add("@game_Id", DbType.Int32).Value = gameId;
                var executeScalar = command.ExecuteScalar();
                isDuelGame = executeScalar != null;
            }
            _connection.Close();
            return isDuelGame;
        }

        public void UpdateDuel(CurrentDuel engineDuel, int id)
        {
            if (_inError)
            {
                return;
            }

            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return;
                }
            }

            _connection.Open();

            try
            {

                var aSerializer = new XmlSerializer(typeof(CurrentDuel));
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                aSerializer.Serialize(sw, engineDuel);
                var xmlResult = sw.GetStringBuilder().ToString();
                var sql = @"UPDATE duel SET gamesToPlay=@gamesToPlay, configXML=@configXML
                           WHERE ID=@ID; ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
                    command2.Parameters.Add("@ID", DbType.Int32).Value = id;
                    command2.Parameters.Add("@gamesToPlay", DbType.Int32).Value = engineDuel.Cycles;
                    command2.Parameters.Add("@configXML", DbType.String).Value = xmlResult;
                    command2.ExecuteNonQuery();
                }

             
            }
            catch (Exception ex)
            {

                _logging?.LogError(ex);

            }
            finally
            {
                _connection.Close();
            }

        }
        #endregion

        #region Tournament
        public void SaveTournamentGamePair(int tournamentId, int gameId)
        {
            if (_inError)
            {
                return;
            }

            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return;
                }
            }

            _connection.Open();
            try
            {
                var sql = @"INSERT INTO tournamentGames (tournament_id, game_id) VALUES (@tournament_id, @game_id); ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
                    command2.Parameters.Add("@tournament_id", DbType.Int32).Value = tournamentId;
                    command2.Parameters.Add("@game_id", DbType.Int32).Value = gameId;
                    command2.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public void UpdateTournament(int id, CurrentTournament tournament)
        {
            if (_inError)
            {
                return;
            }

            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return;
                }
            }
            _connection.Open();

            try
            {

                var aSerializer = new XmlSerializer(typeof(CurrentTournament));
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                aSerializer.Serialize(sw, tournament);
                var xmlResult = sw.GetStringBuilder().ToString();
                var sql = @"UPDATE tournament SET event=@event, configXML=@configXML
                           WHERE id=@id; ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
                    command2.Parameters.Add("@event", DbType.String).Value = tournament.GameEvent;
                    command2.Parameters.Add("@configXML", DbType.String).Value = xmlResult;
                    command2.Parameters.Add("@id",DbType.Int32).Value = id;
                    command2.ExecuteNonQuery();
                }

               
            }
            catch (Exception ex)
            {

                _logging?.LogError(ex);
                
            }
            finally
            {
                _connection.Close();
            }

        }

        public int SaveTournament(CurrentTournament tournament, int gamesToPlay)
        {
            if (_inError)
            {
                return -1;
            }

            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return -1;
                }
            }

            var tournamentId = 0;
            _connection.Open();

            try
            {

                var aSerializer = new XmlSerializer(typeof(CurrentTournament));
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                aSerializer.Serialize(sw, tournament);
                var xmlResult = sw.GetStringBuilder().ToString();
                var sql = @"INSERT INTO tournament (event, eventDate, gamesToPlay, configXML)
                           VALUES (@event, @eventDate, @gamesToPlay, @configXML); ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
                    command2.Parameters.Add("@event", DbType.String).Value = tournament.GameEvent;
                    command2.Parameters.Add("@eventDate", DbType.Int64).Value = DateTime.UtcNow.ToFileTime();
                    command2.Parameters.Add("@gamesToPlay", DbType.Int32).Value = gamesToPlay;
                    command2.Parameters.Add("@configXML", DbType.String).Value = xmlResult;
                    command2.ExecuteNonQuery();
                }

                using (var command2 = new SQLiteCommand("SELECT LAST_INSERT_ROWID();", _connection))
                {
                    var executeScalar = command2.ExecuteScalar();
                    tournamentId = int.Parse(executeScalar.ToString());
                }
            }
            catch (Exception ex)
            {

                _logging?.LogError(ex);
                return -1;
            }
            finally
            {
                _connection.Close();
            }

            return tournamentId;
        }

        public void DeleteAllTournament()
        {
            _connection.Open();
            var sqLiteTransaction = _connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                string sql;
                sql = @"DELETE FROM fenToGames WHERE game_id IN (select game_id FROM tournamentGames); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {

                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM games WHERE id IN (select game_id FROM tournamentGames); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {

                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM tournamentGames; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM tournament; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {

                    command.ExecuteNonQuery();
                }

                sqLiteTransaction.Commit();
            }
            catch (Exception ex)
            {
                sqLiteTransaction.Rollback();
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public void DeleteTournamentGames(int id)
        {
            try
            {
                _connection.Open();
                string sql;

                sql = @"DELETE FROM fenToGames WHERE game_id IN (select game_id FROM tournamentGames  WHERE tournament_id=@id); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM games WHERE id IN (select game_id FROM tournamentGames  WHERE tournament_id=@id); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM tournamentGames  WHERE tournament_id=@id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

        

            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public void DeleteTournament(int id)
        {
            try
            {
                _connection.Open();
                string sql;
                
                    sql = @"DELETE FROM fenToGames WHERE game_id IN (select game_id FROM tournamentGames  WHERE tournament_id=@id); "; 
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.Parameters.Add("@id", DbType.Int32).Value = id;
                        command.ExecuteNonQuery();
                    }

                    sql = @"DELETE FROM games WHERE id IN (select game_id FROM tournamentGames  WHERE tournament_id=@id); ";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.Parameters.Add("@id", DbType.Int32).Value = id;
                        command.ExecuteNonQuery();
                    }
                
                sql = @"DELETE FROM tournamentGames  WHERE tournament_id=@id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM tournament  WHERE id=@id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public DatabaseTournament[] LoadTournament()
        {
            List<DatabaseTournament> allTournaments = new List<DatabaseTournament>();
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return allTournaments.ToArray();
                }
            }

            try
            {
                _connection.Open();
                var sql = "SELECT t.id, t.configXML, t.gamesToPlay, t.eventDate, count(g.id) as playedGames " +
                          "FROM tournament as T left join tournamentGames as g on (t.id=g.tournament_id) " +
                          "group by t.configXML,t.gamesToPlay,t.eventDate " +
                          "ORDER BY t.id;";
                var xmlSerializer = new XmlSerializer(typeof(CurrentTournament));
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            CurrentTournament currentTournament;
                            using (TextReader reader = new StringReader(rdr.GetString(1)))
                            {
                                currentTournament = (CurrentTournament)xmlSerializer.Deserialize(reader);
                            }

                            allTournaments.Add(new DatabaseTournament()
                                               {
                                                   TournamentId = rdr.GetInt32(0),
                                                   CurrentTournament = currentTournament,
                                                   GamesToPlay = rdr.GetInt32(2),
                                                   PlayedGames = rdr.GetInt32(4),
                                                   State = rdr.GetInt32(2) == rdr.GetInt32(4) ? "Finished" : "Running",
                                                   EventDate = DateTime.FromFileTime(rdr.GetInt64(3)),
                                                   Participants = string.Join(", ", currentTournament.Players.Select(c => c.Name))
                                               });
                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return allTournaments.ToArray();
        }

        public DatabaseTournament LoadTournament(int id)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return null;
                }
            }

            DatabaseTournament dbTournament = null;
            try
            {
                _connection.Open();
                var sql = "SELECT t.Id, t.configXML, t.gamesToPlay, t.eventDate, count(g.id) as playedGames " +
                          "FROM tournament as T left join tournamentGames as g on (t.id=g.tournament_id) " +
                          "WHERE t.id=@ID "+
                          "group by t.configXML,t.gamesToPlay,t.eventDate " +
                          "ORDER BY t.id;";
                var xmlSerializer = new XmlSerializer(typeof(CurrentTournament));
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@ID", DbType.Int32).Value = id;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            CurrentTournament currentTournament = null;
                            using (TextReader reader = new StringReader(rdr.GetString(1)))
                            {
                                currentTournament = (CurrentTournament) xmlSerializer.Deserialize(reader);
                            }
                            dbTournament = new DatabaseTournament()
                                         {
                                TournamentId = rdr.GetInt32(0),
                                CurrentTournament = currentTournament,
                                GamesToPlay = rdr.GetInt32(2),
                                PlayedGames = rdr.GetInt32(4),
                                State = rdr.GetInt32(2) == rdr.GetInt32(4) ? "Finished" : "Running",
                                EventDate = DateTime.FromFileTime(rdr.GetInt64(3)),
                                Participants = string.Join(", ", currentTournament.Players.Select(c => c.Name))
                            };
                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return dbTournament;
        }

        public DatabaseTournament LoadTournamentByGame(int gameId)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return null;
                }
            }

            try
            {
                _connection.Open();
                var sql = "SELECT tournament_id FROM tournamentGames WHERE game_id = @game_Id";
                int id;
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@game_id", DbType.Int32).Value = gameId;

                    var executeScalar = command.ExecuteScalar();
                    id = int.Parse(executeScalar.ToString());

                }
                _connection.Close();
                return LoadTournament(id);
            
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            return null;
        }

        public int GetTournamentGamesCount(int tournamentId)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return 0;
                }
            }

            int gamesCount = 0;
            try
            {
                _connection.Open();
                var sql = "SELECT COUNT(*) FROM tournamentGames WHERE tournament_id=@tournament_id;";
                
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@tournament_id", DbType.Int32).Value = tournamentId;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            gamesCount = rdr.GetInt32(0);
                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return gamesCount;
        }

        public bool IsTournamentGame(int gameId)
        {
            if (_inError)
            {
                return true;
            }

            bool isTournamentGame = false;
            _connection.Open();
            var sql = "SELECT tournament_id FROM tournamentGames WHERE game_id = @game_Id";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.Add("@game_Id", DbType.Int32).Value = gameId;
                var executeScalar = command.ExecuteScalar();
                isTournamentGame = executeScalar != null;
            }
            _connection.Close();
            return isTournamentGame;
        }

        public int GetLatestTournamentGameId(int tournamentId)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return 0;
                }
            }

            int gamesId = 0;
            try
            {
                _connection.Open();
                var sql = "SELECT games_id FROM tournamentGames WHERE id = (select max(id) FROM tournamentGames WHERE tournament_id=@tournament_id);";

                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@tournament_id", DbType.Int32).Value = tournamentId;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            gamesId = rdr.GetInt32(0);
                        }

                        rdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
            }

            _connection.Close();
            return gamesId;
        }

        public DatabaseGameSimple[] GetTournamentGames(int tournamentId)
        {


            if (_inError)
            {
                return Array.Empty<DatabaseGameSimple>();
            }

            DatabaseGameSimple[] allGames = null;
            _connection.Open();
            using (var cmd = new SQLiteCommand(
                "SELECT g.id, g.white, g.black, g.event, g.site, g.result, g.gameDate, g.pgn, g.pgnXml, g.pgnHash, g.round, g.white_elo, g.black_elo,  g.gameHash" +
                " FROM games as g  " +
                " JOIN tournamentGames t ON (t.game_id=g.id)" +
                "WHERE t.tournament_id=@tournamentId;", _connection))
            {
                cmd.Parameters.Add("@tournamentId", DbType.Int32).Value = tournamentId;
                using (var rdr = cmd.ExecuteReader())
                {
                    allGames = GetByReader(rdr);
                    rdr.Close();
                }
            }

            _connection.Close();
            return allGames;
        }


        #endregion

        #region TWIC

        public int SaveTWICImport(int twicNumber, string url, string fileName, int numberOfGames, long fileDate)
        {
            int twicId = -1;
            if (_inError)
            {
                return -1;
            }

            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return -1;
                }
            }
           
            _connection.Open();
            try
            {
                var sql = @"INSERT INTO twic (twicNumber,importedPGNFile,importedUrl, numberOfGames, importDate, fileDate) VALUES (@twicNumber,@importedPGNFile,@importedUrl,@numberOfGames, @importDate, @fileDate); ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
                    command2.Parameters.Add("@twicNumber", DbType.Int32).Value = twicNumber;
                    command2.Parameters.Add("@importedPGNFile", DbType.String).Value = fileName;
                    command2.Parameters.Add("@importedUrl", DbType.String).Value = url;
                    command2.Parameters.Add("@numberOfGames", DbType.Int32).Value = numberOfGames;
                    command2.Parameters.Add("@importDate", DbType.Int64).Value = DateTime.UtcNow.ToFileTime();
                    command2.Parameters.Add("@fileDate", DbType.Int64).Value = fileDate;
                    command2.ExecuteNonQuery();
                    using (var command3 = new SQLiteCommand("SELECT LAST_INSERT_ROWID();", _connection))
                    {
                        var executeScalar = command3.ExecuteScalar();
                        twicId = int.Parse(executeScalar.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                twicId = -1;
            }
            finally
            {
                _connection.Close();
            }
            return twicId;
        }

        public DateTime? TWICFileImported(string url)
        {
            if (_inError)
            {
                return null;
            }

            DateTime? result = null;
            _connection.Open();
            var sql = "SELECT importDate FROM twic WHERE importedUrl = @importedUrl";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.Add("@importedUrl", DbType.String).Value = url;
                var executeScalar = command.ExecuteScalar();
                if (executeScalar != null)
                {
                    result = DateTime.FromFileTime((long)executeScalar);
                }
            }
            _connection.Close();
            return result;
        }

        public DateTime? TWICFileImported(int twicNumber)
        {
            if (_inError)
            {
                return null;
            }

            DateTime? result = null;
            _connection.Open();
            var sql = "SELECT importDate FROM twic WHERE twicNumber = @twicNumber";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.Add("@twicNumber", DbType.Int32).Value = twicNumber;
                var executeScalar = command.ExecuteScalar();
                if (executeScalar != null)
                {
                    result = DateTime.FromFileTime((long)executeScalar);
                }
            }
            _connection.Close();
            return result;
        }

        public TwicDownload[] GeTwicDownloads()
        {
            if (_inError)
            {
                return Array.Empty<TwicDownload>();
            }
            TwicDownload[] allDownloads = null;
            _connection.Open();
            using (var cmd = new SQLiteCommand(
                       "SELECT id, twicNumber, importedPGNFile, numberOfGames, importedUrl, importDate, fileDate " +
                       " FROM twic order by twicNumber", _connection))
            {
               
                using (var rdr = cmd.ExecuteReader())
                {
                    allDownloads = GetTwicByReader(rdr);
                    rdr.Close();
                }
            }

            _connection.Close();

            return allDownloads;
        }

        public int NumberOfTWICInGames(int twicId)
        {
            if (_inError)
            {
                return -1;
            }

            int result = -1;
            _connection.Open();
            var sql = "SELECT count(*) as number FROM games WHERE twic_id = @twic_id";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.Add("@twic_id", DbType.Int32).Value = twicId;
                var executeScalar = command.ExecuteScalar();
                if (executeScalar != null)
                {
                    result = int.Parse(executeScalar.ToString());
                }
            }

            _connection.Close();
            return result;
        }

        public int MaxTwicNumber()
        {
            if (_inError)
            {
                return -1;
            }

            int result = -1;
            _connection.Open();
            var sql = "SELECT MAX(twicNumber) as number FROM twic";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                var executeScalar = command.ExecuteScalar();
                if (executeScalar != null)
                {
                    int.TryParse(executeScalar.ToString(), out result);
                }
            }
            _connection.Close();
            return result;
        }

        public void SetNumberOfTWICGames(int twicId, int numberOfGames)
        {
            if (_inError)
            {
                return;
            }

            
            _connection.Open();
            var sql = "UPDATE twic set NumberOfGames=@NumberOfGames WHERE id = @twic_id";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.Add("@twic_id", DbType.Int32).Value = twicId;
                command.Parameters.Add("@NumberOfGames", DbType.Int32).Value = numberOfGames;
                var executeScalar = command.ExecuteNonQuery();
              
            }
            _connection.Close();
        }

        public void DeleteTWICGames(int twicId)
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
                string pragma = "pragma journal_mode = memory;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
                pragma = "pragma synchronous = normal;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
                pragma = "pragma temp_store = memory;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
                pragma = "pragma mmap_size = 30000000000;";
                using (var command2 = new SQLiteCommand(pragma, _connection))
                {
                    command2.ExecuteNonQuery();
                }
            }
            var sqLiteTransaction = _connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var sql = @"DELETE FROM fenToGames WHERE game_id IN (select id from games where twic_id=@twic_id); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@twic_id", DbType.Int32).Value = twicId;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM games WHERE twic_id=@twic_id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@twic_id", DbType.Int32).Value = twicId;
                    command.ExecuteNonQuery();
                }

                sql = @"DELETE FROM fens WHERE id NOT IN (SELECT fen_id FROM fenToGames); ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.ExecuteNonQuery();
                }
                sql = @"DELETE FROM twic WHERE id=@twic_id; ";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.Add("@twic_id", DbType.Int32).Value = twicId;
                    command.ExecuteNonQuery();
                }
                sqLiteTransaction.Commit();
            }
            catch (Exception ex)
            {
                sqLiteTransaction.Rollback();
                _logging?.LogError(ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        private TwicDownload[] GetTwicByReader(SQLiteDataReader rdr)
        {
            // "id, twicNumber, importedPGNFile, numberOfGames, importedUrl, importDate"
            var allGames = new List<TwicDownload>(100);
            while (rdr.Read())
            {
                var databaseGameSimple = new TwicDownload
                {
                    Id = rdr.GetInt32(0),
                    TwicNumber = rdr.GetInt32(1),
                    NumberOfGames = rdr.GetInt32(3),
                    ImportDate = DateTime.FromFileTime(rdr.GetInt64(5)),
                    FileDate = DateTime.FromFileTime(rdr.GetInt64(6)),
                    FileName = rdr.GetString(2),
                };
                allGames.Add(databaseGameSimple);
            }

            return allGames.ToArray();
        }

        #endregion
    }
}