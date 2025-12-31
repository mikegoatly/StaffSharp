namespace StaffSharp.Performance;

/// <summary>
/// Metadata about a performance recording or symbolic source.
/// </summary>
/// <param name="Title">The title of the piece.</param>
/// <param name="Composer">The composer or artist.</param>
/// <param name="Copyright">Copyright information.</param>
/// <param name="SourceFile">Original file path or URL of the audio/MIDI source.</param>
/// <param name="RecordingDate">Date the recording was made or file was created.</param>
public sealed record PerformanceMetadata(
    string? Title = null,
    string? Composer = null,
    string? Copyright = null,
    string? SourceFile = null,
    DateTime? RecordingDate = null);
