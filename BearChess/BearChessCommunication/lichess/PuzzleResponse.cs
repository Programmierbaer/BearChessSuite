using System.Text.Json.Serialization;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

public class PuzzleResponse
{
    [JsonPropertyName("puzzle")]
    public Puzzle puzzle { get; set; }
        
    [JsonPropertyName("game")]
    public PuzzleGame game { get; set; }
}