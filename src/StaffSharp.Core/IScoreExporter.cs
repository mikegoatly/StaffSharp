namespace StaffSharp;

using StaffSharp.Notation;

/// <summary>
/// Represents an exporter that can write a NotationScore to a specific music notation format.
/// </summary>
public interface IScoreExporter
{
    /// <summary>
    /// Gets the file extensions supported by this exporter (e.g., ".mid", ".midi", ".svg").
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Gets the format name for display purposes (e.g., "MIDI", "SVG").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Gets the available options for this exporter.
    /// </summary>
    IReadOnlyList<ExportOption> AvailableOptions { get; }

    /// <summary>
    /// Exports a notation score to the specified stream.
    /// </summary>
    /// <param name="score">The notation score to export.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Format-specific options (key-value pairs).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportAsync(
        NotationScore score,
        Stream stream,
        IReadOnlyDictionary<string, string>? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes a format-specific export option.
/// </summary>
/// <param name="Name">The option name (e.g., "tpqn").</param>
/// <param name="Description">Description including which format(s) it applies to.</param>
/// <param name="DefaultValue">The default value if not specified.</param>
public sealed record ExportOption(string Name, string Description, string? DefaultValue = null);
