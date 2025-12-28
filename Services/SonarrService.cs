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

public class SonarrSeries
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("tvdbId")]
    public int TvdbId { get; set; }
}

public class SonarrEpisode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

public class SonarrService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public SonarrService(IHttpClientFactory httpClientFactory, ILogger logger)
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
    /// Triggers a search in Sonarr for a TV series based on TVDB ID.
    /// - Single episode (season > 0 AND episode > 0) = EpisodeSearch (if enabled)
    /// - Whole season (season > 0, episode = 0) = SeasonSearch
    /// - Whole show (season = 0, episode = 0) = SeriesSearch
    /// </summary>
    public async Task<bool> TriggerSearchAsync(string sonarrUrl, string apiKey, int tvdbId, int seasonNumber = 0, int episodeNumber = 0, bool searchOnEpisode = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sonarrUrl) || string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Sonarr URL or API key not configured");
                return false;
            }

            // Ensure URL has http:// scheme
            sonarrUrl = EnsureHttpScheme(sonarrUrl);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            // Find series by TVDB ID using query parameter (more efficient)
            var seriesUrl = $"{sonarrUrl.TrimEnd('/')}/api/v3/series?tvdbId={tvdbId}";
            var seriesResponse = await httpClient.GetAsync(seriesUrl);

            if (!seriesResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Sonarr series. Status: {StatusCode}", seriesResponse.StatusCode);
                return false;
            }

            var seriesContent = await seriesResponse.Content.ReadAsStringAsync();
            var matchingSeries = JsonSerializer.Deserialize<List<SonarrSeries>>(seriesContent) ?? new List<SonarrSeries>();
            var series = matchingSeries.FirstOrDefault();

            if (series == null)
            {
                _logger.LogWarning("Series with TVDB ID {TvdbId} not found in Sonarr", tvdbId);
                return false;
            }

            _logger.LogInformation("Found series '{Title}' (ID: {Id}) for TVDB ID {TvdbId}", series.Title, series.Id, tvdbId);

            object commandBody;
            string commandName;

            if (seasonNumber > 0 && episodeNumber > 0)
            {
                // Episode search - need to find the episode ID first
                var episodesUrl = $"{sonarrUrl.TrimEnd('/')}/api/v3/episode?seriesId={series.Id}&seasonNumber={seasonNumber}";
                var episodesResponse = await httpClient.GetAsync(episodesUrl);

                if (!episodesResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch episodes. Status: {StatusCode}", episodesResponse.StatusCode);
                    return false;
                }

                var episodesContent = await episodesResponse.Content.ReadAsStringAsync();
                var episodes = JsonSerializer.Deserialize<List<SonarrEpisode>>(episodesContent) ?? new List<SonarrEpisode>();
                var episode = episodes.FirstOrDefault(e => e.EpisodeNumber == episodeNumber);

                if (episode == null)
                {
                    _logger.LogWarning("Episode S{Season:D2}E{Episode:D2} not found in Sonarr", seasonNumber, episodeNumber);
                    return false;
                }

                commandName = "EpisodeSearch";
                commandBody = new { name = commandName, episodeIds = new[] { episode.Id } };
                _logger.LogInformation("Triggering episode search for S{Season:D2}E{Episode:D2} '{EpisodeTitle}'", seasonNumber, episodeNumber, episode.Title);
            }
            else if (seasonNumber > 0)
            {
                // Season search
                commandName = "SeasonSearch";
                commandBody = new { name = commandName, seriesId = series.Id, seasonNumber = seasonNumber };
                _logger.LogInformation("Triggering season search for S{Season:D2}", seasonNumber);
            }
            else
            {
                // Series search (whole show)
                commandName = "SeriesSearch";
                commandBody = new { name = commandName, seriesId = series.Id };
                _logger.LogInformation("Triggering series search for '{Title}'", series.Title);
            }

            // Execute the command
            var commandUrl = $"{sonarrUrl.TrimEnd('/')}/api/v3/command";
            var jsonContent = JsonSerializer.Serialize(commandBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var commandResponse = await httpClient.PostAsync(commandUrl, content);

            if (commandResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Sonarr {CommandName} triggered successfully for '{Title}'", commandName, series.Title);
                return true;
            }
            else
            {
                var errorContent = await commandResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Sonarr command failed. Status: {StatusCode}, Response: {Response}", commandResponse.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering Sonarr search for TVDB ID {TvdbId}", tvdbId);
            return false;
        }
    }
}
