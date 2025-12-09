using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations.CTG;

namespace www.SoLaNoSoft.com.BearChessBase.Implementations
{
    public static class OpeningBookLoader
    {
        private static string _bookPath;
        private static string _binPath;
        private static Dictionary<string, BookInfo> _installedBooks;


        public static void Init(string bookPath, string binPath, FileLogger fileLogger)
        {
            _bookPath = bookPath;
            _binPath = binPath;
            _installedBooks = new Dictionary<string, BookInfo>();
            InstallInternalBook(fileLogger);
            InstallInternalHiddenBook(fileLogger);
            ReadInstalledBooks(fileLogger);
        }

        public static OpeningBook LoadBook(string bookName, bool checkFile)
        {
            var openingBook = new OpeningBook();
            if (openingBook.LoadBook(_installedBooks[bookName].FileName, checkFile))
            {
                return openingBook;
            }

            return null;
        }

        public static string[] GetInstalledBooks()
        {
            return _installedBooks.Keys.ToArray();
        }

        public static BookInfo[] GetInstalledBookInfos()
        {
            return _installedBooks.Values.ToArray();
        }

        private static void InstallInternalBook(FileLogger fileLogger)
        {
            try
            {

                var sourcePath = Path.Combine(_binPath, Constants.InternalBookGUIDPerfectCTG);
                fileLogger?.LogDebug($"Check for internal book path: {sourcePath}");
                if (!Directory.Exists(sourcePath))
                {
                    fileLogger?.LogDebug($"Path not found");
                    return;
                }

                var sourceFile = Path.Combine(_binPath, Constants.InternalBookGUIDPerfectCTG, $"{Constants.InternalBookGUIDPerfectCTG}.book");
                fileLogger?.LogDebug($"Read internal book: {sourceFile}");
                if (!File.Exists(sourceFile))
                {
                    fileLogger?.LogDebug($"File not found");
                    return;
                }

                var targetFile = Path.Combine(_bookPath, $"{Constants.InternalBookGUIDPerfectCTG}.book");
                fileLogger?.LogDebug($"Copy {sourceFile} {targetFile}");
                File.Copy(sourceFile, targetFile, true);
                var file = new FileInfo(targetFile);
                var serializer = new XmlSerializer(typeof(BookInfo));
                TextReader textReader = new StreamReader(file.FullName);
                var origConfig = (BookInfo)serializer.Deserialize(textReader);
                origConfig.FileName =
                    Path.Combine(_binPath, Constants.InternalBookGUIDPerfectCTG, Constants.InternalBookFileNamePerfectCTG);
                origConfig.Name = "Perfect 2023";
                origConfig.IsInternalBook = true;
                origConfig.IsHiddenInternalBook = false;
                textReader.Close();
                TextWriter textWriter = new StreamWriter(file.FullName, false);
                serializer.Serialize(textWriter, origConfig);
                textWriter.Close();


            }
            catch (Exception ex)
            {
                fileLogger?.LogError(ex);
            }
        }

        private static void InstallInternalHiddenBook(FileLogger fileLogger)
        {
            try
            {
                var sourcePath = Path.Combine(_binPath, Constants.InternalBookGUIDPerfectBIN);
                fileLogger?.LogDebug($"Check for internal book path: {sourcePath}"); ;
                if (!Directory.Exists(sourcePath))
                {
                    fileLogger?.LogDebug($"Path not found");
                    return;
                }

                var sourceFile = Path.Combine(_binPath, Constants.InternalBookGUIDPerfectBIN, $"{Constants.InternalBookGUIDPerfectBIN}.book");
                fileLogger?.LogDebug($"Read internal book: {sourceFile}");
                if (!File.Exists(sourceFile))
                {
                    fileLogger?.LogDebug($"File not found");
                    return;
                }

                var targetFile = Path.Combine(_bookPath, $"{Constants.InternalBookGUIDPerfectBIN}.book");
                fileLogger?.LogDebug($"Copy {sourceFile} {targetFile}");
                File.Copy(sourceFile, targetFile, true);
                var file = new FileInfo(targetFile);
                var serializer = new XmlSerializer(typeof(BookInfo));
                TextReader textReader = new StreamReader(file.FullName);
                var origConfig = (BookInfo)serializer.Deserialize(textReader);
                origConfig.FileName =
                    Path.Combine(_binPath, Constants.InternalBookGUIDPerfectBIN, Constants.InternalBookFileNamePerfectBIN);
                origConfig.Name = "Perfect 2023 Polyglot";
                origConfig.IsInternalBook = true;
                origConfig.IsHiddenInternalBook = true;
                textReader.Close();
                TextWriter textWriter = new StreamWriter(file.FullName, false);
                serializer.Serialize(textWriter, origConfig);
                textWriter.Close();

            }
            catch (Exception ex)
            {
                fileLogger?.LogError(ex);
            }
        }

        private static void ReadInstalledBooks(FileLogger fileLogger)
        {
            try
            {
                int invalidBooks = 0;
                fileLogger?.LogInfo("Read installed books...");
                _installedBooks.Clear();
                var fileNames = Directory.GetFiles(_bookPath, "*.book", SearchOption.TopDirectoryOnly);
                foreach (var fileName in fileNames)
                {
                    try
                    {
                        fileLogger?.LogInfo($"  File {fileName}");
                        var serializer = new XmlSerializer(typeof(BookInfo));
                        TextReader textReader = new StreamReader(fileName);
                        var savedBook = (BookInfo)serializer.Deserialize(textReader);
                        if (!File.Exists(savedBook.FileName))
                        {
                            fileLogger?.LogWarning($"  Book file {savedBook.FileName} not found");
                            invalidBooks++;
                            continue;
                        }

                        if (_installedBooks.ContainsKey(savedBook.Name))
                        {
                            fileLogger?.LogWarning($" Book file {savedBook.Name} already installed");
                            invalidBooks++;
                            continue;
                        }

                        fileLogger?.LogInfo($"   Add book {savedBook.FileName} as {savedBook.Name}");
                        _installedBooks.Add(savedBook.Name, savedBook);
                    }
                    catch (Exception ex)
                    {
                        fileLogger?.LogError(ex);
                    }
                }

                fileLogger?.LogInfo($" {_installedBooks.Count} books read");
                if (invalidBooks > 0)
                {
                    fileLogger?.LogWarning($" {invalidBooks} books could not read");
                }
            }
            catch (Exception ex)
            {
                fileLogger?.LogError("Read installed books", ex);
            }
        }
    }
}