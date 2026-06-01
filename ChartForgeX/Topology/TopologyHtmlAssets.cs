using System;
using System.IO;

namespace ChartForgeX.Topology;

internal static class TopologyHtmlAssets
{
    private const string InteractionScriptResourceName = "ChartForgeX.Topology.Assets.topology-interaction.js";

    private static readonly Lazy<string> InteractionScriptResource = new Lazy<string>(() => ReadResource(InteractionScriptResourceName));

    internal static string InteractionScript => InteractionScriptResource.Value;

    private static string ReadResource(string resourceName)
    {
        var assembly = typeof(TopologyHtmlAssets).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException("Embedded topology HTML asset not found: " + resourceName);
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
