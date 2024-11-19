#nullable enable
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace UICatalog.Scenarios;

/// <summary>
///     A helper class for accessing the ucdapi.org API.
/// </summary>
public class UcdApiClient
{
    public const string BaseUrl = "https://ucdapi.org/unicode/latest/";
    private static readonly HttpClient _httpClient = new ();

    public async Task<string> GetChars (string chars)
    {
        HttpResponseMessage response = await _httpClient.GetAsync ($"{BaseUrl}chars/{Uri.EscapeDataString (chars)}");
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ();
    }

    public async Task<string> GetCharsName (string chars)
    {
        HttpResponseMessage response =
            await _httpClient.GetAsync ($"{BaseUrl}chars/{Uri.EscapeDataString (chars)}/name");
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ();
    }

    public async Task<string> GetCodepointDec (int dec)
    {
        HttpResponseMessage response = await _httpClient.GetAsync ($"{BaseUrl}codepoint/dec/{dec}");
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ();
    }

    public async Task<string> GetCodepointHex (string hex)
    {
        HttpResponseMessage response = await _httpClient.GetAsync ($"{BaseUrl}codepoint/hex/{hex}");
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ();
    }
}
