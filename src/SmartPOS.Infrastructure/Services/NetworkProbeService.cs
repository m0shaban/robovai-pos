using System.Net.Http;

namespace SmartPOS.Infrastructure.Services;

public class NetworkProbeService
{
    private readonly HttpClient _httpClient;

    public NetworkProbeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<bool> IsEndpointReachableAsync(string endpointUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpointUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
