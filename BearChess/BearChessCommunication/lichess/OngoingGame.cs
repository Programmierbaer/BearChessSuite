using System.Text.Json.Serialization;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

public class OngoingGame
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; }
    [JsonPropertyName("fen")]
    public string Fen { get; set; }
}