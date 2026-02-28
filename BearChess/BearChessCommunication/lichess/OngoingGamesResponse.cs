using System.Text.Json.Serialization;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

public class OngoingGamesResponse
{
    [JsonPropertyName("nowPlaying")]
    public OngoingGame[] OngoingGames { get; set; }
}