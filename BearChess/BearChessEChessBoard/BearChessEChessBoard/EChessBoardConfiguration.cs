﻿using System.IO;
using System.Xml.Serialization;

namespace www.SoLaNoSoft.com.BearChess.EChessBoard
{
    public class EChessBoardConfiguration
    {
        public string PortName { get; set; }
        public string WebSocketAddr { get; set; }
        public string Baud { get; set; }
        public bool DimLeds { get; set; }
        public int DimLevel { get; set; }
        public bool FlashInSync { get; set; }
        public bool NoFlash { get; set; }
        public bool UseBluetooth { get; set; }
        public bool UseClock { get; set; }
        public bool ClockShowOnlyMoves { get; set; }
        public bool ClockSwitchSide { get; set; }
        public bool ClockUpperCase { get; set; }
        public bool ClockBeep { get; set; }
        public int BeepDuration { get; set; }
        public bool LongMoveFormat { get; set; }
        public int ScanTime { get; set; }
        public int Debounce { get; set; }
        public bool UseChesstimation { get; set; }
        public bool UseElfacun { get; set; }
        public bool ShowMoveLine { get; set; }
        public bool ShowPossibleMoves { get; set; }
        public bool ShowPossibleMovesEval { get; set; }
        public bool ShowOwnMoves { get; set; }
        public bool ShowHintMoves { get; set; }
        public string FileName { get; set; }
        public bool SayLiftUpDownFigure { get; set; }
        public bool SendLEDCommands { get; set; }


        [XmlArray]
        public ExtendedEChessBoardConfiguration[] ExtendedConfig { get; set; }

        // ReSharper disable once ConvertConstructorToMemberInitializers
        public EChessBoardConfiguration()
        {
            DimLevel = -1;
            UseClock = true;
            ClockShowOnlyMoves = false;
            ClockSwitchSide = false;
            ClockUpperCase = false;
            UseBluetooth = false;
            LongMoveFormat = true;
            ScanTime = 30; // Default for ChessLink
            Debounce = 0; // Default for ChessLink
            Baud = "1200";
            UseChesstimation = false;
            UseElfacun = false;
            ShowMoveLine = false;
            ExtendedConfig = new[]
                             {
                                 new ExtendedEChessBoardConfiguration()
                                 {
                                     Name = "BearChess",
                                     IsCurrent = true
                                 }
                             };
            FileName = string.Empty;
            ShowPossibleMoves = false;
            ShowPossibleMovesEval = false;
            ShowOwnMoves = false;
            ShowHintMoves = true;
            ClockBeep = false;
            BeepDuration = 1;
            WebSocketAddr = string.Empty;
            SayLiftUpDownFigure = false;
            SendLEDCommands = false;
        }

        public static EChessBoardConfiguration Load(string fileName)
        {
            var configuration = new EChessBoardConfiguration();
            try
            {
                if (File.Exists(fileName))
                {
                    var serializer = new XmlSerializer(typeof(EChessBoardConfiguration));
                    TextReader textReader = new StreamReader(fileName);
                    var savedConfig = (EChessBoardConfiguration)serializer.Deserialize(textReader);
                    textReader.Close();
                    configuration.PortName = savedConfig.PortName;
                    configuration.Baud = savedConfig.Baud;
                    configuration.DimLeds = savedConfig.DimLeds;
                    configuration.DimLevel = savedConfig.DimLevel;
                    configuration.FlashInSync = savedConfig.FlashInSync;
                    configuration.NoFlash = savedConfig.NoFlash;
                    configuration.UseBluetooth = savedConfig.UseBluetooth;
                    configuration.ClockShowOnlyMoves = savedConfig.ClockShowOnlyMoves;
                    configuration.UseClock = savedConfig.UseClock;
                    configuration.ClockBeep = savedConfig.ClockBeep;
                    configuration.BeepDuration = savedConfig.BeepDuration;
                    configuration.ClockSwitchSide = savedConfig.ClockSwitchSide;
                    configuration.ClockUpperCase = savedConfig.ClockUpperCase;
                    configuration.LongMoveFormat = savedConfig.LongMoveFormat;
                    configuration.ScanTime = savedConfig.ScanTime;
                    configuration.Debounce = savedConfig.Debounce;
                    configuration.UseChesstimation = savedConfig.UseChesstimation;
                    configuration.UseElfacun = savedConfig.UseElfacun;
                    configuration.ShowMoveLine = savedConfig.ShowMoveLine;

                    if (configuration.DimLevel < 0)
                    {
                        configuration.DimLevel = configuration.DimLeds ? 0 : 14;
                    }

                    configuration.ExtendedConfig = savedConfig.ExtendedConfig;
                    configuration.FileName = fileName;
                    configuration.ShowPossibleMoves = savedConfig.ShowPossibleMoves;
                    configuration.ShowPossibleMovesEval = savedConfig.ShowPossibleMovesEval;
                    configuration.ShowOwnMoves = savedConfig.ShowOwnMoves;
                    configuration.ShowHintMoves = savedConfig.ShowHintMoves;
                    configuration.WebSocketAddr = savedConfig.WebSocketAddr;
                    configuration.SayLiftUpDownFigure = savedConfig.SayLiftUpDownFigure;
                    configuration.SendLEDCommands = savedConfig.SendLEDCommands;
                }
                else
                {
                    configuration.PortName = "<auto>";
                }
            }
            catch
            {
                configuration.PortName = "<auto>";
            }

            return configuration;
        }

        public static void Save(EChessBoardConfiguration configuration, string fileName)
        {
            try
            {
                var fileInfo = new FileInfo(fileName);
                if (!Directory.Exists(fileInfo.DirectoryName))
                {
                    Directory.CreateDirectory(fileInfo.DirectoryName);
                    Directory.CreateDirectory(Path.Combine(fileInfo.DirectoryName, "log"));
                }

                configuration.FileName = fileName;
                var serializer = new XmlSerializer(typeof(EChessBoardConfiguration));
                TextWriter textWriter = new StreamWriter(fileName, false);
                serializer.Serialize(textWriter, configuration);
                textWriter.Close();
            }
            catch 
            {
                //   _fileLogger?.LogError("Error on save configuration", ex);
            }
        }

        public override string ToString()
        {
            return
                $"Port:{PortName} Web:{WebSocketAddr} Baud:{Baud} DimLeds:{DimLeds} DimLevel:{DimLevel} FlashSync:{FlashInSync} " +
                $"NoFlash:{NoFlash} UseBT:{UseBluetooth} UseClock:{UseClock} ClockOnlyMoves:{ClockShowOnlyMoves} ClockSwitchSide:{ClockSwitchSide} " +
                $"ClockUpperCase:{ClockUpperCase} ClockBeep:{ClockBeep} BeepDuration:{BeepDuration} LongMove:{LongMoveFormat} " +
                $"ScanTime:{ScanTime} Debounce:{Debounce} Chesstimation:{UseChesstimation} Elfacun:{UseElfacun} " +
                $"ShowMoveLine:{ShowMoveLine} ShowPossibleMoves:{ShowPossibleMoves} ShowPossibleMovesEval:{ShowPossibleMovesEval} " +
                $"ShowOwnMoves:{ShowOwnMoves} ShowHintMoves:{ShowHintMoves} SayLiftUpDownFigure:{SayLiftUpDownFigure}";
        }
    }

}
