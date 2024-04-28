using GameUtils;
using GameUtils.Extensions;
using System.Text.Json;

namespace TestClient
{
    internal static class RequestHelper
    {
        private static HttpRequester _httpRequester = new HttpRequester();
        private static string _urlBase = "http://127.0.0.1:30003";
        public static TRes Request<TReq, TRes>(TReq req)
        {
            var url = $"{_urlBase}/{typeof(TReq).Name}";
            var json = JsonSerializer.Serialize(req);
            var jsonText = _httpRequester.PostByJsonAsync(url, json, 300000).GetResult();
            return JsonSerializer.Deserialize<TRes>(jsonText, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            });
        }
    }
}
