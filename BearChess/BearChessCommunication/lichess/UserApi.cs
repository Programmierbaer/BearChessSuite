using System.Text.Json;
using System.Threading.Tasks;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

public class UserApi
{
    private readonly ILichessApiClient _lichessApiClient;
    private readonly ILogging _logger;

    public UserApi(ILichessApiClient lichessApiClient, ILogging logger)
    {
        _lichessApiClient = lichessApiClient;
        _logger = logger;        
    }
    
    public async Task<AccountResponse> GetMyProfile()
    {
        _logger?.LogDebug("UserApi: Getting my profile");
        var request = _lichessApiClient.GetRequest("api/account");
        var response = await _lichessApiClient.SendRequest(request, null, null);
        var content = await response.Content.ReadAsStringAsync();
        _logger?.LogDebug($"UserApi: Content: {content}");
        var accountResponse = JsonSerializer.Deserialize<AccountResponse>(content);
        _logger?.LogDebug($"UserApi: {accountResponse.Id} {accountResponse.Username}");
        return accountResponse;
    }
}