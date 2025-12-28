using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JellyseerrSearchTrigger.Services;

public class RadarrMovie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }
}

public class RadarrService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public RadarrService(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the URL has a valid HTTP scheme.
    /// </summary>
    private static string EnsureHttpScheme(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        url = url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "http://" + url;
        }
        return url;
    }

    /// <summary>
    /// Triggers a search in Radarr for a movie based on TMDB ID.
    /// </summary>
    public async Task<bool> TriggerSearchAsync(string radarrUrl, string apiKey, int tmdbId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(radarrUrl) || string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Radarr URL or API key not configured");
                return false;
            }

            // Ensure URL has http:// scheme
            radarrUrl = EnsureHttpScheme(radarrUrl);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            // Find movie by TMDB ID using query parameter (more efficient)
            var movieUrl = $"{radarrUrl.TrimEnd('/')}/api/v3/movie?tmdbId={tmdbId}";
            var movieResponse = await httpClient.GetAsync(movieUrl);

            if (!movieResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Radarr movie. Status: {StatusCode}", movieResponse.StatusCode);
                return false;
            }

            var movieContent = await movieResponse.Content.ReadAsStringAsync();
            var matchingMovies = JsonSerializer.Deserialize<List<RadarrMovie>>(movieContent) ?? new List<RadarrMovie>();
            var movie = matchingMovies.FirstOrDefault();

            if (movie == null)
            {
                _logger.LogWarning("Movie with TMDB ID {TmdbId} not found in Radarr", tmdbId);
                return false;
            }

            _logger.LogInformation("Found movie '{Title}' (ID: {Id}) for TMDB ID {TmdbId}", movie.Title, movie.Id, tmdbId);

            // Execute the search command
            var commandUrl = $"{radarrUrl.TrimEnd('/')}/api/v3/command";
            var commandBody = new { name = "MoviesSearch", movieIds = new[] { movie.Id } };
            var jsonContent = JsonSerializer.Serialize(commandBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var commandResponse = await httpClient.PostAsync(commandUrl, content);

            if (commandResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Radarr MoviesSearch triggered successfully for '{Title}'", movie.Title);
                return true;
            }
            else
            {
                var errorContent = await commandResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Radarr command failed. Status: {StatusCode}, Response: {Response}", commandResponse.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering Radarr search for TMDB ID {TmdbId}", tmdbId);
            return false;
        }
    }
}
