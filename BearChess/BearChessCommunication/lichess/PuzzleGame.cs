using System.Text.Json.Serialization;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

public class PuzzleGame
{
    [JsonPropertyName("clock")]
    public string Clock { get; set; }
    [JsonPropertyName("id")]
    public string Id { get; set; }
    public Variant Perf { get; set; }
    [JsonPropertyName("pgn")]
    public string PGN { get; set; }
    [JsonPropertyName("rated")]
    public bool Rated { get; set; }

    public override string ToString()
    {
        return $"Game: {PGN}";
    }
}