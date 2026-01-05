using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using StaffSharp.Audio;
using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.Audio.Analysis.Quantization;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.IO;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.Demo.ViewModels;
using StaffSharp.Importers.Abc;
using StaffSharp.Notation;

namespace StaffSharp.Demo.Services;

/// <summary>
/// Service for converting between audio, ABC, and notation formats using StaffSharp's audio pipeline.
/// </summary>
public class ConversionService : IConversionService
{
    /// <summary>
    /// Converts an audio file to a notation score.
    /// </summary>
    public async Task<ConversionResult> ConvertAudioAsync(
        string filePath,
        ProcessingOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO implement once the pipeline is structured better
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Audio conversion failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts an ABC notation file to a notation score.
    /// </summary>
    public async Task<ConversionResult> ConvertAbcAsync(
        string abcContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var importer = new AbcScoreImporter();
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(abcContent));
            var score = await importer.ImportAsync(stream, cancellationToken: cancellationToken);

            return new ConversionResult
            {
                Score = score,
                AudioSamples = null,
                DetectedTempo = score.Metadata.Tempo,
                Diagnostics = new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"ABC conversion failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Result of a conversion operation.
/// </summary>
public class ConversionResult
{
    public required NotationScore Score { get; init; }
    public ReadOnlyMemory<float>? AudioSamples { get; init; }
    public int DetectedTempo { get; init; }
    public required IDictionary<string, object> Diagnostics { get; init; }
}
