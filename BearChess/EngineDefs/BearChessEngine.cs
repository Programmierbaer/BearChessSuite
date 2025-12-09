using System;
using System.IO;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using System.Xml.Serialization;
using www.SoLaNoSoft.com.BearChessBase;

namespace www.SoLaNoSoft.com.BearChess.Engine
{
    public static class BearChessEngine
    {

        private static void InstallInternalEngine(string binPath, string uciPath, string engineFileName, string logoFileName,
                                                  string bookFileName, string engineGuid, string newName, string newOriginName, bool isInternalBearChessEngine = false)
        {
            try
            {
                var isBuddy = false;
                var isBearChess = false;
                var sourcePath = Path.Combine(binPath, engineGuid);
                if (!Directory.Exists(sourcePath))
                {
                    return;
                }
                var targetPath = Path.Combine(uciPath, engineGuid);
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }
                else
                {
                    var oldFiles = Directory.GetFiles(targetPath);
                    foreach (var file in oldFiles)
                    {
                        try
                        {
                            if (File.Exists(file))
                            {
                                if (file.EndsWith(".uci", StringComparison.OrdinalIgnoreCase))
                                {
                                    var serializer = new XmlSerializer(typeof(UciInfo));

                                    TextReader textReader = new StreamReader(file);
                                    var savedConfig = (UciInfo)serializer.Deserialize(textReader);
                                    textReader.Close();
                                    isBearChess = savedConfig.IsProbing;
                                    isBuddy = savedConfig.IsBuddy;
                                }
                                File.Delete(file);
                            }
                        }
                        catch
                        {
                            //
                        }
                    }
                }
                var dir = new DirectoryInfo(sourcePath);
                foreach (var file in dir.GetFiles())
                {
                    var targetFilePath = Path.Combine(targetPath, file.Name);
                    if (file.Extension.Equals(".uci"))
                    {
                        file.CopyTo(targetFilePath, true);
                        var serializer = new XmlSerializer(typeof(UciInfo));                      

                        TextReader textReader = new StreamReader(targetFilePath);
                        var savedConfig = (UciInfo)serializer.Deserialize(textReader);
                        textReader.Close();
                        
                        if (!string.IsNullOrEmpty(newName))
                        {
                            savedConfig.Name = newName;
                        }
                        if (!string.IsNullOrEmpty(newOriginName))
                        {
                            savedConfig.OriginName = newOriginName;
                        }                        
                        savedConfig.IsInternalChessEngine = !isInternalBearChessEngine;
                        savedConfig.IsInternalBearChessEngine = isInternalBearChessEngine;
                        savedConfig.FileName =  Path.Combine(binPath, engineGuid, engineFileName);
                        if (!string.IsNullOrEmpty(logoFileName))
                        {
                            savedConfig.LogoFileName = Path.Combine(binPath, engineGuid, logoFileName);
                        }
                        if (!string.IsNullOrEmpty(bookFileName))
                        {
                            savedConfig.SetOpeningBook(Path.Combine(binPath, engineGuid, bookFileName));
                        }

                        savedConfig.IsBuddy = isBuddy;
                        savedConfig.IsProbing = isBearChess;
                        TextWriter textWriter = new StreamWriter(targetFilePath, false);
                        serializer.Serialize(textWriter, savedConfig);
                        textWriter.Close();
                    }
                }
            }
            catch
            {
                //
            }
        }

        private static void InstallInternalBearChessEngine(string binPath, string uciPath)
        {
            InstallInternalEngine(binPath: binPath,
                uciPath: uciPath,
                engineFileName: Configuration.Instance.RunOn64Bit ? Constants.InternalChessEngineStockfish64FileName : Constants.InternalChessEngineStockfish32FileName,
                logoFileName: string.Empty, 
                bookFileName: string.Empty, 
                engineGuid: Constants.InternalBearChessEngineGUID, 
                newName: "Stockfish 11 Internal BearChess", 
                newOriginName: "Stockfish 11", 
                isInternalBearChessEngine: true);
        }

        private static void InstallInternalChessEngineSpike(string binPath, string uciPath)
        {
            InstallInternalEngine(binPath: binPath,
                uciPath: uciPath, 
                engineFileName: Constants.InternalChessEngineSpikeFileName, 
                logoFileName: Constants.InternalChessEngineSpikeLogoFileName, 
                bookFileName: string.Empty, 
                engineGuid: Constants.InternalChessEngineSpikeGUID, 
                newName: string.Empty, 
                newOriginName: string.Empty);

        }

        private static void InstallInternalChessEngineWasp(string binPath, string uciPath)
        {
            InstallInternalEngine(binPath: binPath, 
                uciPath: uciPath, 
                engineFileName: Constants.InternalChessEngineWaspFileName, 
                logoFileName: Constants.InternalChessEngineWaspLogoFileName, 
                bookFileName: Constants.InternalBookFileNameWaspBIN, 
                engineGuid: Constants.InternalChessEngineWaspGUID, 
                newName: "Wasp 7.14 BearChess", 
                newOriginName: "Wasp 7.14");

        }

        private static void InstallInternalChessEngineStockfish(string binPath, string uciPath)
        {
            InstallInternalEngine(binPath: binPath, 
                uciPath: uciPath, 
                engineFileName: Configuration.Instance.RunOn64Bit ? Constants.InternalChessEngineStockfish64FileName : Constants.InternalChessEngineStockfish32FileName, 
                logoFileName:Constants.InternalChessEngineStockfishLogoFileName, 
                bookFileName: string.Empty, 
                engineGuid: Constants.InternalChessEngineStockfishGUID,
                newName: "Stockfish 11 BearChess",
                newOriginName: "Stockfish 11");

        }

        private static void InstallInternalChessEngineFruit(string binPath, string uciPath)
        {
            InstallInternalEngine(binPath: binPath, 
                uciPath: uciPath, 
                engineFileName: Constants.InternalChessEngineFruitFileName, 
                logoFileName: Constants.InternalChessEngineFruitLogoFileName, 
                bookFileName: Constants.InternalBookFileNamePerfectBIN, 
                engineGuid: Constants.InternalChessEngineFruitGUID, 
                newName: "Fruit 2.1 BearChess", 
                newOriginName: "Fruit 2.1");
        }

        private static void InstallInternalChessEngineCT800(string binPath, string uciPath)
        {
            InstallInternalEngine(binPath: binPath, 
                uciPath: uciPath, 
                engineFileName:  Configuration.Instance.RunOn64Bit ? Constants.InternalChessEngineCT80064FileName : Constants.InternalChessEngineCT80032FileName, 
                logoFileName: Configuration.Instance.RunOn64Bit ? Constants.InternalChessEngineCT80064LogoFileName : Constants.InternalChessEngineCT80032LogoFileName,
                bookFileName: string.Empty, 
                engineGuid: Constants.InternalChessEngineCT800GUID, 
                newName: string.Empty, newOriginName: string.Empty);

        }

        public static void InstallBearChessEngines(string binPath, string uciPath)
        {
            InstallInternalBearChessEngine(binPath, uciPath);
            InstallInternalChessEngineSpike(binPath, uciPath);
            InstallInternalChessEngineWasp(binPath, uciPath);
            InstallInternalChessEngineStockfish(binPath, uciPath);
            InstallInternalChessEngineFruit(binPath, uciPath);
            InstallInternalChessEngineCT800(binPath, uciPath);
        }
    }
}
