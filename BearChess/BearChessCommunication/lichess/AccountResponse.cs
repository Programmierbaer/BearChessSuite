using System.Text.Json.Serialization;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

public class AccountResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; }
}