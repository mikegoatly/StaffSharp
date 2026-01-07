using StaffSharp.Abc.Importing;
using StaffSharp.Audio.IO;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Demo.ViewModels;
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
            // Load audio file
            using var fileStream = File.OpenRead(filePath);

            // Create pipeline options from UI options
            var pipelineOptions = CreatePipelineOptions(options);

            // Run the pipeline
            var score = await AudioPipeline.FromWavAsync(fileStream, pipelineOptions, cancellationToken);

            // Load the audio samples for waveform display
            ReadOnlyMemory<float>? audioSamples = null;
            try
            {
                using var audioFile = File.OpenRead(filePath);
                var audioBuffer = await WavReader.ReadAsync(audioFile, cancellationToken);
                audioSamples = audioBuffer.Samples;
            }
            catch
            {
                // If we can't load samples for display, that's OK
            }

            return new ConversionResult
            {
                Score = score,
                AudioSamples = audioSamples,
                DetectedTempo = score.Metadata.Tempo,
                Diagnostics = new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Audio conversion failed: {ex.Message}", ex);
        }
    }

    private static AudioPipelineOptions CreatePipelineOptions(ProcessingOptions options)
    {
        var pipelineOptions = AudioPipelineOptions.Default;

        // Apply user-configured options
        // TODO: Map more options as needed when AudioPipelineOptions is expanded
        // For now, the default options should work well

        return pipelineOptions;
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
