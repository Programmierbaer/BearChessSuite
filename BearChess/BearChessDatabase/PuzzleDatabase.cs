using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Xml.Serialization;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChessDatabase
{
    public class PuzzleDatabase : IDatabase
    {

        public static string BearChessDbFileName = "puzzle_7040F19DE68142369C2368FC0A6D585F.db";
        public static string ImportDbFileName = "puzzle_5D3926EBE6A84D32AD5F66A3FD2989A6.db";

        public bool InError => _inError;

        private readonly ILogging _logging;
        private SQLiteConnection _connection = null;
        private bool _dbExists = false;
        private bool _inError = false;
        private int _storageVersion = -1;
        private SQLiteTransaction _sqLiteTransaction = null;

        public string FileName { get; }

        public PuzzleDatabase(ILogging logging, string fileName)
        {
            _logging = logging;
            FileName = fileName;
            LoadDb(fileName);
        }

        public static void InstallBearChessDatabase(string binPath, string dbPath, FileLogger fileLogger)
        {
            try
            {
                if (string.IsNullOrEmpty(binPath) || string.IsNullOrEmpty(dbPath))
                {
                    return;
                }
                var sourceFile = Path.Combine(binPath, PuzzleDatabase.BearChessDbFileName);
                fileLogger?.LogDebug($"Check for origin BearChess-DB: {sourceFile}");
                if (!File.Exists(sourceFile))
                {
                    fileLogger?.LogDebug($"File not found");
                    return;
                }

                var targetFile = Path.Combine(dbPath, PuzzleDatabase.BearChessDbFileName);
                if (File.Exists(targetFile))
                {
                    fileLogger?.LogDebug($"Target database {targetFile} already exists. No copy");
                    return;
                }
                fileLogger?.LogDebug($"Copy {sourceFile} {targetFile}");
                File.Copy(sourceFile, targetFile, true);

            }
            catch (Exception ex)
            {
                fileLogger?.LogError(ex);
            }
        }



        public bool Open()
        {
            try
            {
                if (_connection == null)
                {
                    LoadDb();
                    if (_inError || _connection == null)
                    {
                        return false;
                    }
                }
                _inError = false;
                if (_connection.State == ConnectionState.Open)
                {
                    return true;
                }
                _connection.Open();                
                return true;
            }
            catch (Exception ex)
            {
                _inError = true;
                _logging?.LogError(ex);
            }

            return false;
        }


        public DatabasePuzzle LoadPuzzle(int id)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return null;
                }
            }
        
            var puzzle = new DatabasePuzzle();

            try
            {

                Open();
                string sql = @"SELECT id, label, event, pgn, playCount, solved from puzzles WHERE id=@id;";                
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@id", DbType.Int32).Value = id;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            var pgnLoader = new PgnLoader();
                            var pgn = rdr.GetString(3);
                            puzzle.Id = rdr.GetInt32(0);
                            puzzle.Label = rdr.GetString(1);
                            puzzle.PgnGame = pgnLoader.GetGame(pgn);
                            puzzle.PlayCount = rdr.GetInt32(4);
                            puzzle.IsSolved = false;
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

            return puzzle;
        }

        public DatabasePuzzle LoadNextPuzzle(int id)
        {
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return null;
                }
            }

            var puzzle = new DatabasePuzzle();

            try
            {

                Open();
                string sql = @"SELECT id, label, event, pgn, playCount, solved from puzzles WHERE solved=0 and id>@id ORDER BY id limit 1;";
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.Add("@id", DbType.Int32).Value = id;
                    using (var rdr = cmd.ExecuteReader())
                    {                        
                        if (rdr.Read())
                        {
                            var pgnLoader = new PgnLoader();
                            var pgn = rdr.GetString(3);
                            puzzle.Id = rdr.GetInt32(0);
                            puzzle.Label = rdr.GetString(1);
                            puzzle.PgnGame = pgnLoader.GetGame(pgn);
                            puzzle.PlayCount = rdr.GetInt32(4);
                            puzzle.IsSolved = false;
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

            return puzzle;
        }

        public DatabasePuzzle LoadRandomPuzzle()
        {            
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return null;
                }
            }
            var count = GetTotalPuzzlesCount(false);
            if (count == 0) 
            { 
                return null;
            }
            var random = new Random();
            var offset = random.Next(0, count - 1);

            var puzzle = new DatabasePuzzle();            

            try
            {               
                Open();
                string sql = $"SELECT id, label, event, pgn, playCount, solved from puzzles WHERE solved=0 LIMIT 1 OFFSET {offset};";
                var xmlSerializer = new XmlSerializer(typeof(DatabaseGame));
                using (var cmd = new SQLiteCommand(sql, _connection))
                {                    
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            var pgnLoader = new PgnLoader();
                            var pgn = rdr.GetString(3);                            
                            puzzle.Id = rdr.GetInt32(0);
                            puzzle.Label = rdr.GetString(1);                                                           
                            puzzle.PgnGame = pgnLoader.GetGame(pgn);
                            puzzle.PlayCount = rdr.GetInt32(4);
                            puzzle.IsSolved = false;
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
            
            return puzzle;
        }

        public bool SavePuzzle(string label, string pgnEvent, string pgn, int playCount, bool commitTransaction = true)
        {
            var success = false;
            if (!_dbExists)
            {
                if (!CreateTables())
                {
                    return false;
                }
            }

            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
                var pragma = "pragma journal_mode = memory;";
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
                var sql = @"INSERT INTO puzzles (label, event, pgn, playCount, solved) VALUES (@label, @event, @pgn, @playCount, @solved); ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {

                    command2.Parameters.Add("@label", DbType.String).Value = label;
                    command2.Parameters.Add("@event", DbType.String).Value = pgnEvent;
                    command2.Parameters.Add("@pgn", DbType.String).Value = pgn;
                    command2.Parameters.Add("@playCount", DbType.Int32).Value = playCount;
                    command2.Parameters.Add("@solved", DbType.Int32).Value = 0;
                    command2.ExecuteNonQuery();
                }
                if (commitTransaction)
                {
                    _sqLiteTransaction?.Commit();
                    _sqLiteTransaction = null;
                }

                success = true;
            }
            catch (Exception ex)
            {
                _sqLiteTransaction?.Rollback();
                _sqLiteTransaction = null;
                _logging?.LogError(ex);
            }
            finally
            {
                if (commitTransaction)
                {
                    _connection?.Close();
                }
            }
            return success;
        }

        public void Close()
        {
            _connection?.Close();
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
                const string sql = "VACUUM; ";
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

        public int GetTotalPuzzlesCount()
        {
            return GetInternalTotalPuzzlesCount(null);         
        }

        public int GetTotalPuzzlesCount(bool solved)
        {
            return GetInternalTotalPuzzlesCount(solved);
           
        }

        public void ResetToUnsolved()
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
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            try
            {

                var sql = "UPDATE puzzles SET solved=0;";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
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

        public void UpdateSolved(int id, bool solved)
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
            if (_connection.State!=ConnectionState.Open)
            {
                _connection.Open();
            }

            try
            {
                
                var sql = @"UPDATE puzzles SET solved=@solved  WHERE id=@id; ";
                using (var command2 = new SQLiteCommand(sql, _connection))
                {
                    command2.Parameters.Add("@solved", DbType.Int32).Value = solved ? 1 : 0;                    
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

        private int GetInternalTotalPuzzlesCount(bool? solved)
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
                Open();

                var whereClause = solved == null ? string.Empty : solved.Value ? "WHERE solved = 1" : "WHERE solved = 0";
                var sql = $"SELECT COUNT(*) FROM puzzles {whereClause};";
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

        private int GetStorageVersion()
        {
            const string sql = "SELECT version FROM storageVersion;";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                var executeScalar = Convert.ToInt32(command.ExecuteScalar());
                return executeScalar;
            }
        }

        private void LoadDb()
        {
            LoadDb(FileName);
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

        private bool CreateTables()
        {
            if (_inError || !Open())
            {
                return false;
            }

            try
            {
                _storageVersion = -1;
                string sql;
                if (!TableExists("storageVersion"))
                {
                    sql = "CREATE TABLE storageVersion (version INTEGER NOT NULL);";
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

                
                if (!TableExists("puzzles"))
                {
                    sql = "CREATE TABLE puzzles " +
                          "(id INTEGER PRIMARY KEY," +
                          " label TEXT NOT NULL," +
                          " event TEXT NOT NULL," +
                          " pgn TEXT NOT NULL, " +
                          " playCount INTEGER NOT NULL, " +
                          " solved integer not null);";
                    using (var command = new SQLiteCommand(sql, _connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                _dbExists = true;
                _storageVersion = 1;
                return true;
            }
            catch (Exception ex)
            {
                _inError = true;
                _logging?.LogError(ex);
            }

            return false;
        }

        private void LoadDb(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logging?.LogError("Load with empty file name");
                _inError = true;
            }
            _dbExists = File.Exists(fileName);
            try
            {
                if (!_dbExists)
                {
                    SQLiteConnection.CreateFile(fileName);
                }
                _connection = new SQLiteConnection($"Data Source = {fileName}; Version = 3;");
                CreateTables();
                _inError = false;
            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                _inError = true;
            }
        }
        public void Dispose()
        {
            _connection?.Dispose();
        }

        void IDatabase.LoadDb(string fileName)
        {
            LoadDb(fileName);
        }

        public void CommitAndClose()
        {
            _sqLiteTransaction?.Commit();
            _sqLiteTransaction = null;
            _connection?.Close();
        }

        public string Backup()
        {
            string target;
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
                if (!File.Exists(backupFileName))
                {
                    return $"File{Environment.NewLine}{backupFileName}{Environment.NewLine} not found";
                }

                Close();
                _connection = null;
                File.Copy(backupFileName, FileName, true);
                LoadDb();
                return string.Empty;

            }
            catch (Exception ex)
            {
                _logging?.LogError(ex);
                LoadDb();
                return $"Error: {ex.Message}";
            }
        }
    }
}