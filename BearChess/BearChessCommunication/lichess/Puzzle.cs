using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

/// <summary>
/// Represents a chess puzzle obtained from Lichess, including its metadata and solution.
/// </summary>
public class Puzzle
{
    /// <summary>
    /// Puzzle ID
    /// </summary>
    [JsonPropertyName("id")]
    public string id { get; set; }

    /// <summary>
    /// The game in which the puzzle was extracted from
    /// </summary>
    [JsonIgnore]
    public PuzzleGame Game { get; set; }

    /// <summary>
    /// The ply in which the puzzle starts
    /// </summary>
    [JsonPropertyName("initialPly")]
    public int InitialPly { get; set; }

    /// <summary>
    /// How often that puzzle was already played
    /// </summary>
    [JsonPropertyName("Plays")]
    public int Plays { get; set; }

    /// <summary>
    /// The Rating of the puzzle
    /// </summary>
    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    /// <summary>
    /// The moves of the puzzle
    /// </summary>
    [JsonPropertyName("solution")]
    public List<string> Solution { get; set; } = new List<string>();

    /// <summary>
    /// The topics that the puzzle covers
    /// </summary>
    [JsonPropertyName("themes")]
    public List<string> Themes { get; set; } = new List<string>();

    public override string ToString()
    {
        return $"{id}: {Game}  Solution: {string.Join(",",Solution)}";
    }
}