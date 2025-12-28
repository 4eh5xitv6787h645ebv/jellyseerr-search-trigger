using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JellyseerrSearchTrigger.Api;

[ApiController]
[Route("JellyseerrSearchTrigger")]
[Authorize]
public class SearchTriggerController : ControllerBase
{
    private readonly ILogger<SearchTriggerController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public SearchTriggerController(
        ILogger<SearchTriggerController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gets the plugin configuration.
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetConfig()
    {
        var config = Plugin.Instance?.Configuration;
        return Ok(new
        {
            configured = !string.IsNullOrEmpty(config?.SonarrApiKey) || !string.IsNullOrEmpty(config?.RadarrApiKey),
            autoSearchEnabled = config?.AutoSearchEnabled ?? true,
            sonarrConfigured = !string.IsNullOrEmpty(config?.SonarrUrl) && !string.IsNullOrEmpty(config?.SonarrApiKey),
            radarrConfigured = !string.IsNullOrEmpty(config?.RadarrUrl) && !string.IsNullOrEmpty(config?.RadarrApiKey)
        });
    }

    /// <summary>
    /// Triggers a search in Sonarr or Radarr based on the issue report.
    /// Called after an issue is created in Jellyseerr.
    /// </summary>
    [HttpPost("trigger")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> TriggerSearch([FromBody] JsonElement requestBody)
    {
        var config = Plugin.Instance?.Configuration;

        if (config == null || !config.AutoSearchEnabled)
        {
            return Ok(new { triggered = false, reason = "Auto-search is disabled" });
        }

        try
        {
            // Extract parameters from request body
            int tmdbId = 0;
            int tvdbId = 0;
            string mediaType = "";
            int problemSeason = 0;
            int problemEpisode = 0;

            if (requestBody.TryGetProperty("tmdbId", out var tmdbProp))
                tmdbId = tmdbProp.GetInt32();
            if (requestBody.TryGetProperty("tvdbId", out var tvdbProp))
                tvdbId = tvdbProp.GetInt32();
            if (requestBody.TryGetProperty("mediaType", out var typeProp))
                mediaType = typeProp.GetString() ?? "";
            if (requestBody.TryGetProperty("problemSeason", out var seasonProp))
                problemSeason = seasonProp.GetInt32();
            if (requestBody.TryGetProperty("problemEpisode", out var episodeProp))
                problemEpisode = episodeProp.GetInt32();

            _logger.LogInformation("[Search Trigger] Processing: mediaType={MediaType}, tmdbId={TmdbId}, tvdbId={TvdbId}, season={Season}, episode={Episode}",
                mediaType, tmdbId, tvdbId, problemSeason, problemEpisode);

            bool searchTriggered = false;
            string result = "";

            if (mediaType == "tv" && tvdbId > 0 && !string.IsNullOrWhiteSpace(config.SonarrUrl) && !string.IsNullOrWhiteSpace(config.SonarrApiKey))
            {
                var sonarrService = new Services.SonarrService(_httpClientFactory, _logger);
                searchTriggered = await sonarrService.TriggerSearchAsync(
                    config.SonarrUrl,
                    config.SonarrApiKey,
                    tvdbId,
                    problemSeason,
                    problemEpisode
                );
                result = searchTriggered ? "Sonarr search triggered" : "Sonarr search failed or skipped";
                _logger.LogInformation("[Search Trigger] Sonarr result: {Result}", result);
            }
            else if (mediaType == "movie" && tmdbId > 0 && !string.IsNullOrWhiteSpace(config.RadarrUrl) && !string.IsNullOrWhiteSpace(config.RadarrApiKey))
            {
                var radarrService = new Services.RadarrService(_httpClientFactory, _logger);
                searchTriggered = await radarrService.TriggerSearchAsync(
                    config.RadarrUrl,
                    config.RadarrApiKey,
                    tmdbId
                );
                result = searchTriggered ? "Radarr search triggered" : "Radarr search failed";
                _logger.LogInformation("[Search Trigger] Radarr result: {Result}", result);
            }
            else
            {
                result = "Missing required parameters or Arr configuration";
                _logger.LogWarning("[Search Trigger] Skipped: {Reason}", result);
            }

            return Ok(new { triggered = searchTriggered, result = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Search Trigger] Error triggering search");
            return StatusCode(500, new { triggered = false, error = ex.Message });
        }
    }
}
