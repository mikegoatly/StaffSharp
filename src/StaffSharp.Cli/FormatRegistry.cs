namespace StaffSharp.Cli;

using StaffSharp;
using StaffSharp.Abc.Importing;
using StaffSharp.Json;
using StaffSharp.Midi;
using StaffSharp.MusicXml;

/// <summary>
/// Registry for all available import and export formats.
/// </summary>
internal static class FormatRegistry
{
    private static readonly Dictionary<string, IScoreImporter> _importers = BuildImporters();
    private static readonly Dictionary<string, IScoreExporter> _exporters = BuildExporters();

    private static Dictionary<string, IScoreImporter> BuildImporters()
    {
        var importers = new IScoreImporter[]
        {
            new AbcScoreImporter(),
            new MusicXmlScoreImporter(),
            new AudioScoreImporter(),
        };

        var dict = new Dictionary<string, IScoreImporter>(StringComparer.OrdinalIgnoreCase);
        foreach (var importer in importers)
        {
            foreach (var ext in importer.SupportedExtensions)
            {
                dict[ext] = importer;
            }
        }
        return dict;
    }

    private static Dictionary<string, IScoreExporter> BuildExporters()
    {
        var exporters = new IScoreExporter[]
        {
            new MidiScoreExporter(),
            new SvgScoreExporter(),
            new JsonScoreExporter()
        };

        var dict = new Dictionary<string, IScoreExporter>(StringComparer.OrdinalIgnoreCase);
        foreach (var exporter in exporters)
        {
            foreach (var ext in exporter.SupportedExtensions)
            {
                dict[ext] = exporter;
            }
        }

        return dict;
    }

    /// <summary>
    /// Gets all registered importers.
    /// </summary>
    public static IEnumerable<IScoreImporter> Importers => _importers.Values.Distinct();

    /// <summary>
    /// Gets all registered exporters.
    /// </summary>
    public static IEnumerable<IScoreExporter> Exporters => _exporters.Values.Distinct();

    /// <summary>
    /// Finds an importer for the specified file extension.
    /// </summary>
    /// <param name="extension">File extension (e.g., ".abc").</param>
    /// <returns>The importer, or null if not found.</returns>
    public static IScoreImporter? GetImporter(string extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        _importers.TryGetValue(extension, out var importer);
        return importer;
    }

    /// <summary>
    /// Finds an exporter for the specified file extension.
    /// </summary>
    /// <param name="extension">File extension (e.g., ".mid").</param>
    /// <returns>The exporter, or null if not found.</returns>
    public static IScoreExporter? GetExporter(string extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        _exporters.TryGetValue(extension, out var exporter);
        return exporter;
    }
}
