using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.ChessCom
{
    public static class ChessComReader
    {


        private static string profileUrl = "https://api.chess.com/pub/player/";
        private static string currentGamesUrl = "https://api.chess.com/pub/player/{username}/games";
        private static string archivedGamesListUrl = "https://api.chess.com/pub/player/{username}/games/archives";

        private static string archivedGamesMonthListUrl =
            "https://api.chess.com/pub/player/{username}/games/{YYYY}/{MM}";

        private static string puzzleUrl = "https://api.chess.com/pub/puzzle";
        private static string puzzleRandomUrl = "https://api.chess.com/pub/puzzle/random";

        public class ProfileResponse
        {
            [JsonPropertyName("username")]
            public string Username
            {
                get;
                set;
            }

            [JsonPropertyName("avatar")]
            public string Avatar
            {
                get;
                set;
            }

            [JsonPropertyName("name")]
            public string Name
            {
                get;
                set;
            }

            [JsonPropertyName("title")]
            public string Title
            {
                get;
                set;
            }

            [JsonPropertyName("location")]
            public string Location
            {
                get;
                set;
            }

            [JsonPropertyName("status")]
            public string Status
            {
                get;
                set;
            }

        }

        public class PuzzleResponse
        {
            [JsonPropertyName("title")]
            public string Title
            {
                get;
                set;
            }

            [JsonPropertyName("comments")]
            public string Comments
            {
                get;
                set;
            }

            [JsonPropertyName("fen")]
            public string Fen
            {
                get;
                set;
            }

            [JsonPropertyName("url")]
            public string Url
            {
                get;
                set;
            }

            [JsonPropertyName("pgn")]
            public string Pgn
            {
                get;
                set;
            }

            [JsonPropertyName("image")]
            public string Image
            {
                get;
                set;
            }

            [JsonPropertyName("publish_time")]
            public long PublishTime
            {
                get;
                set;
            }
        }


        public class Accuracies
        {
            [JsonPropertyName("white")]
            public double White
            {
                get;
                set;
            }

            [JsonPropertyName("black")]
            public double Black
            {
                get;
                set;
            }
        }

        public class Player
        {
            [JsonPropertyName("rating")]
            public long Rating
            {
                get;
                set;
            }

            [JsonPropertyName("result")]
            public string Result
            {
                get;
                set;
            }

            [JsonPropertyName("username")]
            public string Username
            {
                get;
                set;
            }
        }

        public class ArchivedGame
        {
            [JsonPropertyName("url")]
            public string Url
            {
                get;
                set;
            }

            [JsonPropertyName("pgn")]
            public string Pgn
            {
                get;
                set;
            }

            [JsonPropertyName("time_control")]
            public string TimeControl
            {
                get;
                set;
            }

            [JsonPropertyName("end_time")]
            public long EndTime
            {
                get;
                set;
            }

            [JsonPropertyName("rated")]
            public bool Rated
            {
                get;
                set;
            }

            [JsonPropertyName("accuracies")]
            public Accuracies Accuracies
            {
                get;
                set;
            }

            [JsonPropertyName("fen")]
            public string Fen
            {
                get;
                set;
            }


            [JsonPropertyName("initial_setup")]
            public string InitialSetup
            {
                get;
                set;
            }

            [JsonPropertyName("time_class")]
            public string TimeClass
            {
                get;
                set;
            }

            [JsonPropertyName("rules")]
            public string Rules
            {
                get;
                set;
            }

            [JsonPropertyName("white")]
            public Player White
            {
                get;
                set;
            }

            [JsonPropertyName("black")]
            public Player Black
            {
                get;
                set;
            }

            [JsonPropertyName("uuid")]
            public string Uuid
            {
                get;
                set;
            }
        }

        public class ArchivedGamesListResponse
        {
            [JsonPropertyName("games")]
            public ArchivedGame[] Archives
            {
                get;
                set;
            }
        }

        public static PuzzleResponse GetPuzzle(bool random)
        {
            var webClient = new WebClient();
            webClient.Headers.Add("user-agent",
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            var data = random ? webClient.OpenRead($"{puzzleRandomUrl}") : webClient.OpenRead($"{puzzleUrl}");
            var responseData = JsonSerializer.Deserialize<PuzzleResponse>(data);
            return responseData;
        }

        public static ProfileResponse GetProfile(string userName)
        {
            var webClient = new WebClient();
            webClient.Headers.Add("user-agent",
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            var data = webClient.OpenRead($"{profileUrl}{userName}");
            var responseData = JsonSerializer.Deserialize<ProfileResponse>(data);
            return responseData;
        }

        public static ArchivedGame[] GetArchivedGamesMonth(string userName, int year, int month)
        {
            var webClient = new WebClient();
            webClient.Headers.Add("user-agent",
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            var data = webClient.OpenRead(
                $"{archivedGamesMonthListUrl.Replace("{username}", userName).Replace("{YYYY}", year.ToString()).Replace("{MM}", month.ToString("D2"))}");
            var responseData = JsonSerializer.Deserialize<ArchivedGamesListResponse>(data);
            return responseData.Archives;
        }
    }
}
