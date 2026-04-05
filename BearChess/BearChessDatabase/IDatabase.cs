using System;

namespace www.SoLaNoSoft.com.BearChessDatabase;

public interface IDatabase : IDisposable
{
    bool InError { get; }
    string FileName { get; }
    bool LoadDb(string fileName);
    bool LoadDb();
    bool Open();
    void CommitAndClose();
    void Close();
    void Compress();
    string Backup();
    string Restore(string backupFileName);
    void Drop();
    string ErrorMessage { get; }
}