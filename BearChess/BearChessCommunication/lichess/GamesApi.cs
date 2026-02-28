using System;
using System.Text.Json;
using System.Threading.Tasks;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

public class GamesApi
{
    private readonly ILichessApiClient _lichessApiClient;
    private readonly ILogging _logger;

    public GamesApi(ILichessApiClient lichessApiClient, ILogging logger)
    {
        _lichessApiClient = lichessApiClient;
        _logger = logger;        
    }    
  
    public async Task<OngoingGamesResponse> GetOngoingGames()
    {
        _logger?.LogDebug("GamesApi: Getting my ongoing games");
        var request = _lichessApiClient.GetRequest("api/account/playing", Tuple.Create("nb", 9.ToString()));;
        var response = await _lichessApiClient.SendRequest(request, null, null);
        var content = await response.Content.ReadAsStringAsync();
        _logger?.LogDebug($"GamesApi: Content: {content}");
        var gamesResponse = JsonSerializer.Deserialize<OngoingGamesResponse>(content);
        return gamesResponse;
    }
}