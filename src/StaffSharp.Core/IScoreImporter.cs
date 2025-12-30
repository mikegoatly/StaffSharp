namespace StaffSharp;

using StaffSharp.Notation;

/// <summary>
/// Represents an importer that can read a music notation format and produce a NotationScore.
/// </summary>
public interface IScoreImporter
{
    /// <summary>
    /// Gets the file extensions supported by this importer (e.g., ".abc", ".musicxml").
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Gets the format name for display purposes (e.g., "ABC Notation", "MusicXML").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Imports a notation score from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The imported notation score.</returns>
    Task<NotationScore> ImportAsync(Stream stream, CancellationToken cancellationToken = default);
}
