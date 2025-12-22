namespace EdgeTTS.DotNet;

using System.Net;
using System.Text.Json;
using Models;


public static class Voices
{
    /// <summary>
    /// List all available voices and their attributes.
    /// </summary>
    /// <param name="proxy">Optional proxy server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available voices.</returns>
    public static async Task<IReadOnlyList<Voice>> ListVoicesAsync(string? proxy = null, CancellationToken cancellationToken = default)
    {
        var handler = new HttpClientHandler();
        if (!string.IsNullOrEmpty(proxy))
        {
            handler.Proxy = new WebProxy(proxy);
            handler.UseProxy = true;
        }

        using var client = new HttpClient(handler);
        foreach (var header in Constants.VoiceHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
        client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", $"muid={Drm.GenerateMuid()};");

        var url = $"{Constants.VoiceListUrl}&Sec-MS-GEC={Drm.GenerateSecMsGec()}&Sec-MS-GEC-Version={Constants.SecMsGecVersion}";
        var response = await client.GetAsync(url, cancellationToken);

        // Corrected Logic: Check status code before throwing an exception.
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            // The clock skew is likely off. Adjust it based on the server's Date header.
            Drm.HandleClientResponseError(response.Headers);

            // Retry the request with the corrected clock skew.
            var retryUrl = $"{Constants.VoiceListUrl}&Sec-MS-GEC={Drm.GenerateSecMsGec()}&Sec-MS-GEC-Version={Constants.SecMsGecVersion}";
            response = await client.GetAsync(retryUrl, cancellationToken);
        }

        // Now, for any other error or if the retry failed, this will throw the appropriate exception.
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var voices = JsonSerializer.Deserialize<List<Voice>>(json);
        return voices ?? new List<Voice>();
    }
}