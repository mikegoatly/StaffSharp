using StaffSharp.Audio;
using StaffSharp.Audio.Diagnostics;
using StaffSharp.Notation;

namespace StaffSharp.Demo.Services
{
    internal sealed record ConversionResult(
        bool Success,
        NotationScore Score, 
        AudioBuffer? SourceAudio,
        InMemoryDiagnosticsCollector Diagnostics)
    {
        public static ConversionResult Successful(NotationScore score, AudioBuffer? sourceAudio, InMemoryDiagnosticsCollector diagnostics) => new (
            true, 
            score, 
            sourceAudio, 
            diagnostics);

        public static ConversionResult Failure(InMemoryDiagnosticsCollector diagnostics) => new (
            false, 
            new(ScoreMetadata.Empty, []), 
            null, 
            diagnostics);
    }
}