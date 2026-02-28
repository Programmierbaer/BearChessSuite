using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;

public interface ILichessApiClient
{
    HttpRequestMessage GetRequest(string endpoint, params Tuple<string, string>[] queryParameters);
    Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, HttpMethod method, HttpContent content);
}