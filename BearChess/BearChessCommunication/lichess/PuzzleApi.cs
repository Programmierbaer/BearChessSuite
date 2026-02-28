using System.Text.Json;
using System.Threading.Tasks;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

public class PuzzleApi
{
    private readonly ILichessApiClient _lichessApiClient;
    private readonly ILogging _logger;

    public PuzzleApi(ILichessApiClient lichessApiClient, ILogging logger)
    {
        _lichessApiClient = lichessApiClient;
        _logger = logger;
    }

    public async Task<Puzzle> GetRandomPuzzle()
    {
        _logger?.LogDebug("PuzzleApi: Getting random puzzle");
        var request = _lichessApiClient.GetRequest("api/puzzle/next");
        var response = await _lichessApiClient.SendRequest(request, null, null);
        var content = await response.Content.ReadAsStringAsync();
        _logger?.LogDebug($"PuzzleApi: Content: {content}");
        var puzzleResponse = JsonSerializer.Deserialize<PuzzleResponse>(content);
        puzzleResponse.puzzle.Game = puzzleResponse.game;
        _logger?.LogDebug($"PuzzleApi: {puzzleResponse.puzzle}");
        return puzzleResponse.puzzle;
    }
    
    public async Task<Puzzle> GetPuzzleOfTheDay()
    {
        _logger?.LogDebug("PuzzleApi: Getting puzzle of the day");
        var request = _lichessApiClient.GetRequest("api/puzzle/daily");
        var response = await _lichessApiClient.SendRequest(request, null, null);
        var content = await response.Content.ReadAsStringAsync();
        _logger?.LogDebug($"PuzzleApi: Content: {content}");
        var puzzleResponse = JsonSerializer.Deserialize<PuzzleResponse>(content);
        puzzleResponse.puzzle.Game = puzzleResponse.game;
        _logger?.LogDebug($"PuzzleApi: {puzzleResponse.puzzle}");
        return puzzleResponse.puzzle;
    }
}