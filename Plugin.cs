using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace JellyseerrSearchTrigger;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override string Name => "Jellyseerr Search Trigger";

    public override string Description => "Triggers Sonarr/Radarr searches when issues are reported via Jellyseerr";

    public override Guid Id => new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    public static Plugin? Instance { get; private set; }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = "JellyseerrSearchTrigger.Configuration.configPage.html"
            }
        };
    }
}
