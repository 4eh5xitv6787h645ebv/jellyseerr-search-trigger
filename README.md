# Jellyseerr Search Trigger

A Jellyfin plugin that triggers Sonarr/Radarr searches when issues are reported via Jellyseerr.

## Features

- Automatically triggers searches in Sonarr/Radarr when issues are reported
- Smart search behavior:
  - **Single episode** reported → No search triggered (just reports the issue)
  - **Whole season** reported → SeasonSearch for that season only
  - **Whole show** reported → SeriesSearch for all episodes

## Installation

1. Add this repository to Jellyfin: `https://raw.githubusercontent.com/4eh5xitv6787h645ebv/jellyseerr-search-trigger/main/manifest.json`
2. Install "Jellyseerr Search Trigger" from the plugin catalog
3. Restart Jellyfin
4. Configure the plugin in Dashboard → Plugins → Jellyseerr Search Trigger

## Configuration

- **Sonarr URL**: Your Sonarr instance URL (e.g., `http://sonarr:8989`)
- **Sonarr API Key**: Your Sonarr API key
- **Radarr URL**: Your Radarr instance URL (e.g., `http://radarr:7878`)
- **Radarr API Key**: Your Radarr API key
- **Auto-Search Enabled**: Toggle to enable/disable automatic searches

## Usage

This plugin works alongside Jellyfin Enhanced. When an issue is reported:
1. Jellyfin Enhanced creates the issue in Jellyseerr
2. This plugin triggers the appropriate search in Sonarr/Radarr

## API Endpoint

`POST /JellyseerrSearchTrigger/trigger`

Request body:
```json
{
  "tmdbId": 12345,
  "tvdbId": 67890,
  "mediaType": "tv",
  "problemSeason": 1,
  "problemEpisode": 0
}
```

## License

MIT
