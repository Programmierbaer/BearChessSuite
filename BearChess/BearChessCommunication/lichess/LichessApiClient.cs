using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json;
using System.Threading;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess
{
    public class LichessApiClient : ILichessApiClient, IDisposable
    {
        private readonly ILogging _logger;

        public const string BASE_URL = "https://lichess.org/";
        
        public string Token => _token;
        
        private readonly HttpClient _httpClient;
        private string _token;
        
        public LichessApiClient(ILogging logger)
        {
            _logger = logger;
            _token = Configuration.Instance.GetSecureConfigValue("lichessToken", string.Empty);
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        public async Task SetToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                token = Token;
            }
            var testResult = await TestToken(token);
            if (testResult)
            {
                _token = token;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
            else
            {
                _logger?.LogError("LichessApiClient: invalid token");
                throw new UnauthorizedAccessException("Invalid token");
            }
        }

        private async Task<bool> TestToken(string token)
        {
            var request = GetRequest("api/token/test");
            var response = await SendRequest(request,  HttpMethod.Post, new StringContent(token));
            var json = await response.Content.ReadAsStringAsync();
            var tokenInfo = JsonSerializer.Deserialize<Dictionary<string, TokenInfo>>(json);
            return (tokenInfo[token] is not null);
        }
        
        public HttpRequestMessage GetRequest(string endpoint, params Tuple<string, string>[] queryParameters)
        {
            var request = new HttpRequestMessage();
            request.RequestUri = BuildUri(endpoint, queryParameters).Uri;
            return request;
        }
        
        private UriBuilder BuildUri(string endpoint, params Tuple<string, string>[] queryParameters)
        {
            var builder = new UriBuilder(BASE_URL + endpoint)
            {
                Port = -1
            };

            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var param in queryParameters)
            {
                query[param.Item1] = param.Item2;
            }

            builder.Query = query.ToString();

            return builder;
        }
        
        public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, HttpMethod method, HttpContent content)
        {
            if (method == null)
            {
                method = HttpMethod.Get;
            }

            var client = _httpClient;

            request.Method = method;
            request.Content = content;
            int count = 0;
            HttpResponseMessage response = null;
            while (count<2)
            {
                response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    Thread.Sleep(1000 * 60);
                    count++;
                    continue;
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger?.LogError($"LichessApiClient: {response.StatusCode}: {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                    _logger?.LogError("LichessApiClient: Access denied");
                    throw new HttpRequestException("Access denied");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger?.LogError("LichessApiClient: Key is not authorized");
                    _logger?.LogError($"LichessApiClient: {response.StatusCode}: {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                    throw new UnauthorizedAccessException("Key is not authorized");
                }

                _logger?.LogError($"LichessApiClient: {response.StatusCode}: {response.ReasonPhrase}");
                throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}");
            }
            return response;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}