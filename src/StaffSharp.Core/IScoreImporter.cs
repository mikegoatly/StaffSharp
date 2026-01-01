namespace StaffSharp;

using StaffSharp.Notation;

/// <summary>
/// Represents progress information during score import operations.
/// </summary>
/// <param name="CurrentStep">The current step number (1-based).</param>
/// <param name="TotalSteps">The total number of steps in the import process.</param>
/// <param name="StepName">A descriptive name for the current step.</param>
public record ImportProgress(int CurrentStep, int TotalSteps, string StepName);

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
    /// Imports a notation score from the specified stream with progress reporting.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="progress">Optional progress reporter for tracking import steps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The imported notation score.</returns>
    Task<NotationScore> ImportAsync(Stream stream, IProgress<ImportProgress>? progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a notation score from the specified stream.
    /// Convenience wrapper that calls the progress version with null progress.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The imported notation score.</returns>
    Task<NotationScore> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return ImportAsync(stream, progress: null, cancellationToken);
    }
}
