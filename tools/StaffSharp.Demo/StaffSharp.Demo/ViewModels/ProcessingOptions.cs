using CommunityToolkit.Mvvm.ComponentModel;

using StaffSharp.Audio.Analysis;
using StaffSharp.Audio.Diagnostics;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning;
using StaffSharp.MachineLearning.Options;

namespace StaffSharp.Demo.ViewModels;

/// <summary>
/// Configuration options for audio-to-score processing.
/// </summary>
public partial class ProcessingOptions : ObservableObject
{
    private static readonly MLTranscriptionOptions _defaultMLOptions = new();

    // SVG Rendering
    [ObservableProperty]
    public partial SvgExportOptions ExportOptions { get; set; } = new SvgExportOptions();

    // Note Detection
    [ObservableProperty]
    public partial bool UseMachineLearning { get; set; } = true;

    [ObservableProperty]
    public partial string ModelPath { get; set; }

    [ObservableProperty]
    public partial float OnsetThreshold { get; set; } = _defaultMLOptions.OnsetThreshold;

    [ObservableProperty]
    public partial float FrameThreshold { get; set; } = _defaultMLOptions.FrameThreshold;

    [ObservableProperty]
    public partial float OffsetThreshold { get; set; } = _defaultMLOptions.OffsetThreshold;

    [ObservableProperty]
    public partial float MinNoteLengthSeconds { get; set; } = _defaultMLOptions.MinNoteLengthSeconds;

    /// <summary>
    /// Resets all options to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        ExportOptions = new SvgExportOptions();
        UseMachineLearning = true;
        ModelPath = string.Empty;
        OnsetThreshold = _defaultMLOptions.OnsetThreshold;
        FrameThreshold = _defaultMLOptions.FrameThreshold;
        OffsetThreshold = _defaultMLOptions.OffsetThreshold;
        MinNoteLengthSeconds = _defaultMLOptions.MinNoteLengthSeconds;
    }

    public AudioPipelineOptions CreateAudioPipelineOptions(IDiagnosticsCollector diagnosticsCollector)
    {
        INoteDetector noteDetector = UseMachineLearning
            ? MLNoteDetector.Create(CreateMLTranscriptionOptions())
            : AlgorithmicNoteDetector.Create(); // TODO pass options as AlgorithmicNoteDetectorOptions when available

        return new AudioPipelineOptions
        {
            NoteDetector = noteDetector,
            DiagnosticsCollector = diagnosticsCollector
        };
    }

    private MLTranscriptionOptions CreateMLTranscriptionOptions()
    {
        return new()
        {
            ModelPath = string.IsNullOrWhiteSpace(ModelPath) ? null : ModelPath,
            OnsetThreshold = OnsetThreshold,
            FrameThreshold = FrameThreshold,
            OffsetThreshold = OffsetThreshold,
            MinNoteLengthSeconds = MinNoteLengthSeconds,
        };
    }
}
