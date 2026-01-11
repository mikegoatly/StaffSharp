
using StaffSharp.Audio;
using StaffSharp.Notation;

namespace StaffSharp.Demo.Services
{
    internal sealed record ConversionResult(
        bool Success,
        NotationScore Score, 
        AudioBuffer? SourceAudio,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> Diagnostics)
    {
        public static ConversionResult Successful(NotationScore score, AudioBuffer? sourceAudio, IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> diagnostics) => new (
            true, 
            score, 
            sourceAudio, 
            diagnostics);

        public static ConversionResult Failure(IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> diagnostics) => new (
            false, 
            new(ScoreMetadata.Empty, []), 
            null, 
            diagnostics);
    }
}