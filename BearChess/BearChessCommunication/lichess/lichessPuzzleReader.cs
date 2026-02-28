using System;
using System.IO;
using System.IO.Compression;
using www.SoLaNoSoft.com.BearChessBase;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess
{

    public class LichessPuzzle
    {
        public string PuzzleId
        {
            get;
            set;
        }

        public string Fen
        {
            get;
            set;
        }

        public string Moves
        {
            get;
            set;
        }

        public int PlayCount
        {
            get;
            set;
        }

        public int Rating
        {
            get;
            set;
        }

        public int RatingDeviation
        {
            get;
            set;
        }

        public int Popularity
        {
            get;
            set;
        }

        public int NbPlays
        {
            get;
            set;
        }

        public string Themes
        {
            get;
            set;
        }

        public string GameUrl
        {
            get;
            set;
        }
    }

    public static class LichessPuzzleReader
    {
        public static LichessPuzzle GetPuzzle()
        {
            var puzzleFile = Configuration.Instance.GetConfigValue("lichessPuzzleFile", string.Empty);
            if (string.IsNullOrEmpty(puzzleFile) || !File.Exists(puzzleFile))
            {
                return null;
            }

            var fileSize = Configuration.Instance.GetLongValue("lichessPuzzleFileSize", 0);
            var fileTime = Configuration.Instance.GetLongValue("lichessPuzzleFileTime", 0);
            var puzzleCount = Configuration.Instance.GetIntValue("lichessPuzzleCount", 0);
            var fi2 = new FileInfo(puzzleFile);
            var reRead =  fi2.Length != fileSize || fi2.LastWriteTime.ToFileTime()!=fileTime;

            string line;

            FileStream fs = null;
            StreamReader sr = null;
            if (reRead)
            {
                puzzleCount = 0;
                fs = new FileStream(puzzleFile, FileMode.Open, FileAccess.Read);
                sr = new StreamReader(fs);

                while ((line = sr.ReadLine()) != null)
                {
                    puzzleCount++;
                }

                sr.Close();
                fs.Close();
                Configuration.Instance.SetIntValue("lichessPuzzleCount", puzzleCount);
                Configuration.Instance.SetLongValue("lichessPuzzleFileSize", fi2.Length);
                Configuration.Instance.SetLongValue("lichessPuzzleFileTime", fi2.LastWriteTime.ToFileTime());
            }

            var rnd = new Random();
            var selected = rnd.Next(0, puzzleCount - 1);
            fs = new FileStream(puzzleFile, FileMode.Open, FileAccess.Read);
            sr = new StreamReader(fs);
            var count = 0;
            while ((line = sr.ReadLine()) != null)
            {
                if (count == selected)
                {
                    sr.Close();
                    break;
                }

                count++;
            }



            /*
             PuzzleId,FEN,Moves,Rating,RatingDeviation,Popularity,NbPlays,Themes,GameUrl,OpeningTags
             00008,r6k/pp2r2p/4Rp1Q/3p4/8/1N1P2R1/PqP2bPP/7K b - - 0 24,f2g3 e6e7 b2b1 b3c1 b1c1 h6c1,1829,76,95,8958,crushing hangingPiece long middlegame,https://lichess.org/787zsVup/black#48,
             * */
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            var split = line.Split(',');
            var puzzle = new LichessPuzzle();
            if (split.Length < 3)
            {
                return null;
            }

            puzzle.PuzzleId = split[0];
            puzzle.Fen = split[1];
            puzzle.Moves = string.Empty;
            var moves = split[2].Split(' ');
            puzzle.Moves = string.Join(";", moves);
            if (split.Length > 7)
            {
                puzzle.GameUrl = split[8];
            }
            if (split.Length > 7)
            {
                puzzle.Themes = split[7];
            }
            puzzle.PlayCount = moves.Length;
        
            //puzzle.Rating = int.Parse(splitedLine[3]);
            //puzzle.RatingDeviation = int.Parse(splitedLine[4]);
            //puzzle.Popularity = int.Parse(splitedLine[5]);
            //puzzle.NbPlays = int.Parse(splitedLine[6]);
        
            return puzzle;
        }

        public static bool UnzipPuzzle(string fileName, string destination)
        {
            if (!File.Exists(fileName))
            {
                return false;
            }

            var fi = new FileInfo(fileName);
            if (string.IsNullOrEmpty(destination))
            {
                destination = fi.DirectoryName;
            }

            ZipFile.ExtractToDirectory(fileName, destination);
            return true;
        }
    }
}
