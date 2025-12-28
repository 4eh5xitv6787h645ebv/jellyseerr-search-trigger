using MediaBrowser.Model.Plugins;

namespace JellyseerrSearchTrigger;

public class PluginConfiguration : BasePluginConfiguration
{
    public PluginConfiguration()
    {
        SonarrUrl = "http://sonarr:8989";
        SonarrApiKey = "";
        RadarrUrl = "http://radarr:7878";
        RadarrApiKey = "";
        AutoSearchEnabled = true;
        SearchOnEpisode = false;
        SearchOnSeason = true;
        SearchOnSeries = true;
        SearchOnMovie = true;
    }

    /// <summary>
    /// Gets or sets the Sonarr URL.
    /// </summary>
    public string SonarrUrl { get; set; }

    /// <summary>
    /// Gets or sets the Sonarr API key.
    /// </summary>
    public string SonarrApiKey { get; set; }

    /// <summary>
    /// Gets or sets the Radarr URL.
    /// </summary>
    public string RadarrUrl { get; set; }

    /// <summary>
    /// Gets or sets the Radarr API key.
    /// </summary>
    public string RadarrApiKey { get; set; }

    /// <summary>
    /// Gets or sets whether auto-search is enabled globally.
    /// </summary>
    public bool AutoSearchEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether to trigger search when a single episode issue is reported.
    /// </summary>
    public bool SearchOnEpisode { get; set; }

    /// <summary>
    /// Gets or sets whether to trigger search when a whole season issue is reported.
    /// </summary>
    public bool SearchOnSeason { get; set; }

    /// <summary>
    /// Gets or sets whether to trigger search when a whole series issue is reported.
    /// </summary>
    public bool SearchOnSeries { get; set; }

    /// <summary>
    /// Gets or sets whether to trigger search when a movie issue is reported.
    /// </summary>
    public bool SearchOnMovie { get; set; }
}
